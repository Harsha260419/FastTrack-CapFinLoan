# Database Split Migration Runbook

This runbook migrates from shared `CapFinLoanDb` into service-owned databases.

## Script

- `scripts/migrate-service-databases.ps1`

## What It Does

1. Creates target databases if missing:
   - `CapFinLoanAuthDb`
   - `CapFinLoanApplicationDb`
   - `CapFinLoanAdminDb`
   - `CapFinLoanDocumentDb`
2. Applies EF migrations for each service against its target database.
3. Copies data from shared schemas into service-owned databases:
   - `auth.Users`, `auth.SignupOtps` -> `CapFinLoanAuthDb`
   - `core.LoanApplications` -> `CapFinLoanApplicationDb`
   - `admin.Decisions`, `admin.ApplicationStatusHistory` -> `CapFinLoanAdminDb`
   - `docs.Documents` -> `CapFinLoanDocumentDb`
4. Prints row-count summary (source vs target).

Data copy is idempotent (insert-if-missing on primary keys).

## Usage

From repository root:

```powershell
.\scripts\migrate-service-databases.ps1
```

Custom names/server:

```powershell
.\scripts\migrate-service-databases.ps1 `
  -SqlServer "(localdb)\MSSQLLocalDB" `
  -SourceDatabase "CapFinLoanDb" `
  -AuthDatabase "CapFinLoanAuthDb" `
  -ApplicationDatabase "CapFinLoanApplicationDb" `
  -AdminDatabase "CapFinLoanAdminDb" `
  -DocumentDatabase "CapFinLoanDocumentDb"
```

Run only EF migrations (skip copy):

```powershell
.\scripts\migrate-service-databases.ps1 -SkipDataCopy
```

Run only data copy (skip EF migrations):

```powershell
.\scripts\migrate-service-databases.ps1 -SkipEfMigrations
```

Fail fast if source DB is missing:

```powershell
.\scripts\migrate-service-databases.ps1 -FailIfSourceMissing
```

## Notes

- Requires `dotnet ef` in PATH if EF migration step is enabled.
- Service startup already supports service-specific connection keys with fallback to `DefaultConnection`.
- Use environment variables in deployment for the final cutover:
  - `ConnectionStrings__AuthServiceConnection`
  - `ConnectionStrings__ApplicationServiceConnection`
  - `ConnectionStrings__AdminServiceConnection`
  - `ConnectionStrings__DocumentServiceConnection`
