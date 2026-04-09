param(
    [string]$SqlServer = "(localdb)\MSSQLLocalDB",
    [string]$SourceDatabase = "CapFinLoanDb",
    [string]$AuthDatabase = "CapFinLoanAuthDb",
    [string]$ApplicationDatabase = "CapFinLoanApplicationDb",
    [string]$AdminDatabase = "CapFinLoanAdminDb",
    [string]$DocumentDatabase = "CapFinLoanDocumentDb",
    [switch]$SkipEfMigrations,
    [switch]$SkipDataCopy,
    [switch]$FailIfSourceMissing
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Write-Step {
    param([string]$Message)
    Write-Host "[MIGRATION] $Message" -ForegroundColor Cyan
}

function New-TrustedConnectionString {
    param(
        [string]$Server,
        [string]$Database
    )

    return "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"
}

function Invoke-SqlNonQuery {
    param(
        [string]$ConnectionString,
        [string]$Sql
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 180
        $command.CommandText = $Sql
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlQuery {
    param(
        [string]$ConnectionString,
        [string]$Sql
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 180
        $command.CommandText = $Sql

        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        $dataTable = New-Object System.Data.DataTable
        [void]$adapter.Fill($dataTable)
        return $dataTable
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlScalar {
    param(
        [string]$ConnectionString,
        [string]$Sql
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 180
        $command.CommandText = $Sql
        return $command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

function Ensure-Database {
    param(
        [string]$Server,
        [string]$DatabaseName
    )

    $masterConnection = New-TrustedConnectionString -Server $Server -Database "master"
    $sql = @"
IF DB_ID(N'$DatabaseName') IS NULL
BEGIN
    CREATE DATABASE [$DatabaseName];
END
"@
    Invoke-SqlNonQuery -ConnectionString $masterConnection -Sql $sql
}

function Ensure-DotnetEfAvailable {
    Push-Location $repoRoot
    try {
        & dotnet ef --version | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet ef command is not available. Install it with: dotnet tool install --global dotnet-ef"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-EfUpdate {
    param(
        [string]$ServiceName,
        [string]$ProjectPath,
        [string]$StartupProjectPath,
        [string]$EnvironmentVariableName,
        [string]$ConnectionString
    )

    Push-Location $repoRoot
    if ([string]::IsNullOrWhiteSpace($EnvironmentVariableName)) {
        throw "Environment variable name is required for EF migration execution."
    }

    $previousValue = [Environment]::GetEnvironmentVariable($EnvironmentVariableName, "Process")

    try {
        [Environment]::SetEnvironmentVariable($EnvironmentVariableName, $ConnectionString, "Process")

        Write-Step "Applying EF migrations for $ServiceName"
        & dotnet ef database update --project $ProjectPath --startup-project $StartupProjectPath --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "EF migration failed for $ServiceName"
        }
    }
    finally {
        [Environment]::SetEnvironmentVariable($EnvironmentVariableName, $previousValue, "Process")
        Pop-Location
    }
}

function Copy-Data {
    param(
        [string]$Server,
        [string]$SourceDb,
        [string]$AuthDb,
        [string]$AppDb,
        [string]$AdminDb,
        [string]$DocDb,
        [bool]$FailOnMissing
    )

    $masterConnection = New-TrustedConnectionString -Server $Server -Database "master"

    $sourceDbExists = [int](Invoke-SqlScalar -ConnectionString $masterConnection -Sql "SELECT CASE WHEN DB_ID(N'$SourceDb') IS NULL THEN 0 ELSE 1 END;")
    $authDbExists = [int](Invoke-SqlScalar -ConnectionString $masterConnection -Sql "SELECT CASE WHEN DB_ID(N'$AuthDb') IS NULL THEN 0 ELSE 1 END;")
    $appDbExists = [int](Invoke-SqlScalar -ConnectionString $masterConnection -Sql "SELECT CASE WHEN DB_ID(N'$AppDb') IS NULL THEN 0 ELSE 1 END;")
    $adminDbExists = [int](Invoke-SqlScalar -ConnectionString $masterConnection -Sql "SELECT CASE WHEN DB_ID(N'$AdminDb') IS NULL THEN 0 ELSE 1 END;")
    $docDbExists = [int](Invoke-SqlScalar -ConnectionString $masterConnection -Sql "SELECT CASE WHEN DB_ID(N'$DocDb') IS NULL THEN 0 ELSE 1 END;")

    if ($sourceDbExists -eq 0) {
        $message = "Source database '$SourceDb' was not found."
        if ($FailOnMissing) {
            throw $message
        }

        Write-Host "[WARN] $message Skipping data copy." -ForegroundColor Yellow
        return
    }

    if ($authDbExists -eq 0 -or $appDbExists -eq 0 -or $adminDbExists -eq 0 -or $docDbExists -eq 0) {
        throw "One or more target databases do not exist. Run database creation/migration first."
    }

    $copySql = @"
SET NOCOUNT ON;

IF EXISTS (
    SELECT 1 FROM [$SourceDb].sys.tables t
    INNER JOIN [$SourceDb].sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'auth' AND t.name = 'Users'
)
BEGIN
    INSERT INTO [$AuthDb].[auth].[Users] ([Id],[Name],[Email],[PhoneNumber],[PasswordHash],[Role],[IsActive],[CreatedAt])
    SELECT s.[Id],s.[Name],s.[Email],s.[PhoneNumber],s.[PasswordHash],s.[Role],s.[IsActive],s.[CreatedAt]
    FROM [$SourceDb].[auth].[Users] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [$AuthDb].[auth].[Users] t WHERE t.[Id] = s.[Id]
    );
END

IF EXISTS (
    SELECT 1 FROM [$SourceDb].sys.tables t
    INNER JOIN [$SourceDb].sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'auth' AND t.name = 'SignupOtps'
)
BEGIN
    INSERT INTO [$AuthDb].[auth].[SignupOtps] ([Id],[Email],[OtpHash],[ExpiresAtUtc],[IsUsed],[CreatedAtUtc])
    SELECT s.[Id],s.[Email],s.[OtpHash],s.[ExpiresAtUtc],s.[IsUsed],s.[CreatedAtUtc]
    FROM [$SourceDb].[auth].[SignupOtps] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [$AuthDb].[auth].[SignupOtps] t WHERE t.[Id] = s.[Id]
    );
END

IF EXISTS (
    SELECT 1 FROM [$SourceDb].sys.tables t
    INNER JOIN [$SourceDb].sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'core' AND t.name = 'LoanApplications'
)
BEGIN
    INSERT INTO [$AppDb].[core].[LoanApplications] (
        [ApplicationId],[UserId],[FirstName],[LastName],[DateOfBirth],[Gender],[AddressLine1],[AddressLine2],[City],[State],[PostalCode],
        [EmployerName],[EmploymentType],[MonthlyIncome],[AnnualIncome],[FullName],[Email],[PhoneNumber],[LoanAmount],[LoanPurpose],
        [TenureMonths],[Status],[CreatedAt],[UpdatedAt],[SubmittedAt],[AdminRemarks]
    )
    SELECT
        s.[ApplicationId],s.[UserId],s.[FirstName],s.[LastName],s.[DateOfBirth],s.[Gender],s.[AddressLine1],s.[AddressLine2],s.[City],s.[State],s.[PostalCode],
        s.[EmployerName],s.[EmploymentType],s.[MonthlyIncome],s.[AnnualIncome],s.[FullName],s.[Email],s.[PhoneNumber],s.[LoanAmount],s.[LoanPurpose],
        s.[TenureMonths],s.[Status],s.[CreatedAt],s.[UpdatedAt],s.[SubmittedAt],s.[AdminRemarks]
    FROM [$SourceDb].[core].[LoanApplications] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [$AppDb].[core].[LoanApplications] t WHERE t.[ApplicationId] = s.[ApplicationId]
    );
END

IF EXISTS (
    SELECT 1 FROM [$SourceDb].sys.tables t
    INNER JOIN [$SourceDb].sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'admin' AND t.name = 'Decisions'
)
BEGIN
    INSERT INTO [$AdminDb].[admin].[Decisions] (
        [DecisionId],[ApplicationId],[AdminUserId],[DecisionStatus],[Remarks],[SanctionAmount],[InterestRate],[DecidedBy],[DecidedAt]
    )
    SELECT
        s.[DecisionId],s.[ApplicationId],s.[AdminUserId],s.[DecisionStatus],s.[Remarks],s.[SanctionAmount],s.[InterestRate],s.[DecidedBy],s.[DecidedAt]
    FROM [$SourceDb].[admin].[Decisions] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [$AdminDb].[admin].[Decisions] t WHERE t.[DecisionId] = s.[DecisionId]
    );
END

IF EXISTS (
    SELECT 1 FROM [$SourceDb].sys.tables t
    INNER JOIN [$SourceDb].sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'admin' AND t.name = 'ApplicationStatusHistory'
)
BEGIN
    INSERT INTO [$AdminDb].[admin].[ApplicationStatusHistory] (
        [HistoryId],[ApplicationId],[AdminUserId],[FromStatus],[ToStatus],[ChangedBy],[Remarks],[ChangedAt]
    )
    SELECT
        s.[HistoryId],s.[ApplicationId],s.[AdminUserId],s.[FromStatus],s.[ToStatus],s.[ChangedBy],s.[Remarks],s.[ChangedAt]
    FROM [$SourceDb].[admin].[ApplicationStatusHistory] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [$AdminDb].[admin].[ApplicationStatusHistory] t WHERE t.[HistoryId] = s.[HistoryId]
    );
END

IF EXISTS (
    SELECT 1 FROM [$SourceDb].sys.tables t
    INNER JOIN [$SourceDb].sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'docs' AND t.name = 'Documents'
)
BEGIN
    INSERT INTO [$DocDb].[docs].[Documents] (
        [Id],[ApplicationId],[UserId],[FileName],[FilePath],[DocumentType],[Status],[Remarks],[UploadedAt]
    )
    SELECT
        s.[Id],s.[ApplicationId],s.[UserId],s.[FileName],s.[FilePath],s.[DocumentType],s.[Status],s.[Remarks],s.[UploadedAt]
    FROM [$SourceDb].[docs].[Documents] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [$DocDb].[docs].[Documents] t WHERE t.[Id] = s.[Id]
    );
END
"@

    Invoke-SqlNonQuery -ConnectionString $masterConnection -Sql $copySql
}

function Show-CopySummary {
    param(
        [string]$Server,
        [string]$SourceDb,
        [string]$AuthDb,
        [string]$AppDb,
        [string]$AdminDb,
        [string]$DocDb
    )

    $masterConnection = New-TrustedConnectionString -Server $Server -Database "master"

    $summarySql = @"
SELECT 'auth.Users' AS Entity,
       (SELECT COUNT(*) FROM [$SourceDb].[auth].[Users]) AS SourceCount,
       (SELECT COUNT(*) FROM [$AuthDb].[auth].[Users]) AS TargetCount
UNION ALL
SELECT 'auth.SignupOtps',
       (SELECT COUNT(*) FROM [$SourceDb].[auth].[SignupOtps]),
       (SELECT COUNT(*) FROM [$AuthDb].[auth].[SignupOtps])
UNION ALL
SELECT 'core.LoanApplications',
       (SELECT COUNT(*) FROM [$SourceDb].[core].[LoanApplications]),
       (SELECT COUNT(*) FROM [$AppDb].[core].[LoanApplications])
UNION ALL
SELECT 'admin.Decisions',
       (SELECT COUNT(*) FROM [$SourceDb].[admin].[Decisions]),
       (SELECT COUNT(*) FROM [$AdminDb].[admin].[Decisions])
UNION ALL
SELECT 'admin.ApplicationStatusHistory',
       (SELECT COUNT(*) FROM [$SourceDb].[admin].[ApplicationStatusHistory]),
       (SELECT COUNT(*) FROM [$AdminDb].[admin].[ApplicationStatusHistory])
UNION ALL
SELECT 'docs.Documents',
       (SELECT COUNT(*) FROM [$SourceDb].[docs].[Documents]),
       (SELECT COUNT(*) FROM [$DocDb].[docs].[Documents]);
"@

    $rows = Invoke-SqlQuery -ConnectionString $masterConnection -Sql $summarySql
    $rows | Format-Table -AutoSize
}

Write-Step "Using SQL Server: $SqlServer"
Write-Step "Source database: $SourceDatabase"
Write-Step "Target databases: $AuthDatabase, $ApplicationDatabase, $AdminDatabase, $DocumentDatabase"

Write-Step "Ensuring target databases exist"
Ensure-Database -Server $SqlServer -DatabaseName $AuthDatabase
Ensure-Database -Server $SqlServer -DatabaseName $ApplicationDatabase
Ensure-Database -Server $SqlServer -DatabaseName $AdminDatabase
Ensure-Database -Server $SqlServer -DatabaseName $DocumentDatabase

if (-not $SkipEfMigrations) {
    Write-Step "Checking dotnet ef availability"
    Ensure-DotnetEfAvailable

    Invoke-EfUpdate `
        -ServiceName "AuthService" `
        -ProjectPath "CapFinLoan.Backend/AuthService/CapFinLoan.Auth.Persistence/CapFinLoan.Auth.Persistence.csproj" `
        -StartupProjectPath "CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/CapFinLoan.Auth.API.csproj" `
        -EnvironmentVariableName "ConnectionStrings__AuthServiceConnection" `
        -ConnectionString (New-TrustedConnectionString -Server $SqlServer -Database $AuthDatabase)

    Invoke-EfUpdate `
        -ServiceName "ApplicationService" `
        -ProjectPath "CapFinLoan.Backend/ApplicationService/CapFinLoan.Application.Persistence/CapFinLoan.Application.Persistence.csproj" `
        -StartupProjectPath "CapFinLoan.Backend/ApplicationService/CapFinLoan.Application.API/CapFinLoan.Application.API.csproj" `
        -EnvironmentVariableName "ConnectionStrings__ApplicationServiceConnection" `
        -ConnectionString (New-TrustedConnectionString -Server $SqlServer -Database $ApplicationDatabase)

    Invoke-EfUpdate `
        -ServiceName "AdminService" `
        -ProjectPath "CapFinLoan.Backend/AdminService/CapFinLoan.Admin.Persistence/CapFinLoan.Admin.Persistence.csproj" `
        -StartupProjectPath "CapFinLoan.Backend/AdminService/CapFinLoan.Admin.API/CapFinLoan.Admin.API.csproj" `
        -EnvironmentVariableName "ConnectionStrings__AdminServiceConnection" `
        -ConnectionString (New-TrustedConnectionString -Server $SqlServer -Database $AdminDatabase)

    Invoke-EfUpdate `
        -ServiceName "DocumentService" `
        -ProjectPath "CapFinLoan.Backend/DocumentService/CapFinLoan.Document.Persistence/CapFinLoan.Document.Persistence.csproj" `
        -StartupProjectPath "CapFinLoan.Backend/DocumentService/CapFinLoan.Document.API/CapFinLoan.Document.API.csproj" `
        -EnvironmentVariableName "ConnectionStrings__DocumentServiceConnection" `
        -ConnectionString (New-TrustedConnectionString -Server $SqlServer -Database $DocumentDatabase)
}
else {
    Write-Step "Skipping EF migrations as requested"
}

if (-not $SkipDataCopy) {
    Write-Step "Copying data from shared database into service-owned databases"
    Copy-Data `
        -Server $SqlServer `
        -SourceDb $SourceDatabase `
        -AuthDb $AuthDatabase `
        -AppDb $ApplicationDatabase `
        -AdminDb $AdminDatabase `
        -DocDb $DocumentDatabase `
        -FailOnMissing $FailIfSourceMissing.IsPresent

    Write-Step "Row count summary (source vs target)"
    Show-CopySummary `
        -Server $SqlServer `
        -SourceDb $SourceDatabase `
        -AuthDb $AuthDatabase `
        -AppDb $ApplicationDatabase `
        -AdminDb $AdminDatabase `
        -DocDb $DocumentDatabase
}
else {
    Write-Step "Skipping data copy as requested"
}

Write-Step "Migration workflow completed"
