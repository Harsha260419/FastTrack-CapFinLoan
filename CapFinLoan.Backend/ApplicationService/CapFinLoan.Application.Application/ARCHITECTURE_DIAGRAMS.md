# Application Service Layer Architecture

## 🏗️ Layered Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Layer                                 │
│        (Controllers / HTTP Endpoints)                           │
│                                                                 │
│  ApplicationsController                                         │
│  ├─ POST   /api/applications              → CreateAsync        │
│  ├─ PUT    /api/applications/{id}         → UpdateAsync        │
│  ├─ POST   /api/applications/{id}/submit  → SubmitAsync        │
│  ├─ GET    /api/applications/my           → GetMyAsync         │
│  └─ GET    /api/applications/{id}/status  → GetStatusAsync     │
│                         ↓                                        │
└─────────────────────────────────────────────────────────────────┘
                          │
                    (Dependency)
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│                   Application Layer                              │
│   (Business Logic / Services) ← YOU ARE HERE                   │
│                                                                 │
│  ILoanApplicationService (Interface)                            │
│  └─ LoanApplicationService (Implementation)                     │
│     ├─ CreateApplicationAsync()          [~30 lines]           │
│     ├─ UpdateApplicationAsync()          [~35 lines]           │
│     ├─ SubmitApplicationAsync()          [~40 lines]           │
│     ├─ GetMyApplicationsAsync()          [~20 lines]           │
│     ├─ GetApplicationStatusAsync()       [~25 lines]           │
│     └─ Private Helper Methods            [~80 lines]           │
│                                                                 │
│  Supporting Files:                                              │
│  ├─ DTOs/ (Data Transfer Objects)                              │
│  │  ├─ CreateApplicationRequestDto                             │
│  │  ├─ UpdateApplicationRequestDto                             │
│  │  ├─ SubmitApplicationRequestDto                             │
│  │  ├─ ApplicationResponseDto                                  │
│  │  └─ ApplicationStatusDto                                    │
│  ├─ Enums/                                                      │
│  │  └─ ApplicationStatus (8 statuses)                          │
│  └─ Interfaces/                                                 │
│     ├─ ILoanApplicationService                                 │
│     └─ ILoanApplicationRepository                              │
│                         ↓                                        │
└─────────────────────────────────────────────────────────────────┘
                          │
                    (Dependency)
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│                   Domain Layer                                   │
│           (Entities / Business Rules)                           │
│                                                                 │
│  LoanApplication (Entity)                                       │
│  ├─ ApplicationId (Guid)                                        │
│  ├─ UserId (Guid)                                               │
│  ├─ FullName (string)                                           │
│  ├─ Email (string)                                              │
│  ├─ PhoneNumber (string)                                        │
│  ├─ LoanAmount (decimal)                                        │
│  ├─ LoanPurpose (string)                                        │
│  ├─ TenureMonths (int)                                          │
│  ├─ Status (ApplicationStatus)                                  │
│  ├─ CreatedAt (DateTime)                                        │
│  ├─ UpdatedAt (DateTime)                                        │
│  ├─ SubmittedAt (DateTime?)                                     │
│  └─ AdminRemarks (string?)                                      │
│                         ↓                                        │
└─────────────────────────────────────────────────────────────────┘
                          │
                    (Dependency)
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│               Persistence / Infrastructure Layer                 │
│           (Data Access / EF Core DbContext)                     │
│                                                                 │
│  ApplicationDbContext (DbContext)                               │
│  ├─ DbSet<LoanApplication>                                      │
│  └─ OnModelCreating()                                           │
│                         ↓                                        │
│  LoanApplicationRepository (Repository Implementation)          │
│  ├─ AddAsync()           [INSERT]                              │
│  ├─ UpdateAsync()        [UPDATE]                              │
│  ├─ GetByIdAsync()       [SELECT single]                       │
│  ├─ GetByUserIdAsync()   [SELECT many]                         │
│  ├─ GetByUserIdPaginatedAsync() [SELECT paginated]             │
│  ├─ ExistsByIdAndUserIdAsync()                                 │
│  ├─ GetByStatusAsync()                                          │
│  └─ SaveChangesAsync()                                          │
│                         ↓                                        │
└─────────────────────────────────────────────────────────────────┘
                          │
                    (SQL Generated)
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│                   Database Layer                                 │
│              (SQL Server)                                        │
│                                                                 │
│  Schema: core                                                    │
│  Table: LoanApplications                                         │
│  Columns: ApplicationId, UserId, FullName, Email, ...          │
│           Status (int), CreatedAt, UpdatedAt, SubmittedAt, ... │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Data Flow Example: Create Application

```
┌─ User (Applicant)
│  └─ Makes HTTP POST /api/applications with JSON body
│     │
│     └─→ ApplicationsController.CreateApplication()
│        │
│        ├─ Extract UserId from JWT token (claims)
│        ├─ Pass userId + request to service
│        │
│        └─→ ILoanApplicationService.CreateApplicationAsync(userId, request)
│           │
│           ├─ ValidateCreateApplicationRequest(request)
│           │  ├─ Check FullName not empty
│           │  ├─ Check Email valid
│           │  ├─ Check PhoneNumber ≥ 10 chars
│           │  ├─ Check LoanAmount > 0 and < 10M
│           │  └─ Check TenureMonths between 1-360
│           │
│           ├─ Create LoanApplication entity
│           │  └─ Set Status = Draft
│           │     Set CreatedAt, UpdatedAt = now
│           │     Set UserId from JWT
│           │
│           ├─ _repository.AddAsync(application)
│           │  └─ Queue INSERT (no DB hit yet)
│           │
│           ├─ _repository.SaveChangesAsync()
│           │  └─ Execute INSERT to SQL Server
│           │
│           ├─ MapToApplicationResponseDto(application)
│           │  └─ Convert entity to DTO (no sensitive data)
│           │
│           └─ Return ApplicationResponseDto
│              ├─ Success = true
│              ├─ ApplicationId (generated GUID)
│              ├─ Status = "Draft"
│              └─ Message = "Application created successfully..."
│
└─→ ApplicationsController catches response
   └─ Returns 200 OK + JSON response to client
```

---

## 📊 Status Transition State Machine

```
                    ┌──────────────┐
                    │   Draft      │
                    │ (Applicant   │
                    │  can edit)   │
                    └──────┬───────┘
                           │
                  Applicant │ Clicks Submit
                  (with     │ (declaration)
                 accepted) │
                           ↓
                    ┌──────────────┐
                    │ Submitted    │
                    └──────┬───────┘
                           │
           Admin marks docs│ Doc Upload
              as pending   │ Initiated
                           ↓
                    ┌──────────────┐
          (waiting) │ DocsPending  │
            ........│              │
           .        └──────┬───────┘
          .                │
         .        Admin    │
        .      approves    │ All docs
       .        upload     │ verified
                           ↓
                    ┌──────────────┐
        (Doc phase  │DocsVerified  │
         complete)  └──────┬───────┘
                           │
            Admin initiates│ Review
              review       │ Started
                           ↓
                    ┌──────────────┐
                    │UnderReview   │
                    └──┬──────────┬─┘
                       │          │
          Admin         │          │ Admin
        (approves)      │          │ (rejects)
                   ↓    │          │    ↓
            ┌────────┐  │          │  ┌────────┐
            │Approved│  │          │  │Rejected│
            └──────┬─┘  │          │  └──┬─────┘
                   │    │          │     │
                   │    │ Decision │     │
                   │    │ Finalized│     │
                   └────┴─→ ↓ ←────┘
                        ┌──────────┐
                        │  Closed  │
                        └──────────┘
```

---

## 🔐 Authorization & Role Flow

```
JWT Token from Auth Service
       ↓
   Contains Claims:
   ├─ NameIdentifier = UserId (Guid)
   ├─ Email = user@example.com
   ├─ Role = "Applicant" | "Admin"
   └─ other claims...
       ↓
ApplicationsController
├─ [Authorize] attribute checks token exists
└─ Extract UserId: User.FindFirst(ClaimTypes.NameIdentifier)
       ↓
Pass UserId to Service Methods
       ↓
Service Performs Authorization Checks:

CreateApplicationAsync(userId, request)
└─ No check needed (creating new app)

UpdateApplicationAsync(userId, request)
├─ Fetch app by ID
└─ Verify: app.UserId == userId ✓ else throw UnauthorizedAccessException

SubmitApplicationAsync(userId, request)
├─ Fetch app by ID
└─ Verify: app.UserId == userId ✓ else throw UnauthorizedAccessException

GetMyApplicationsAsync(userId, ...)
└─ Query only apps where UserId == userId

GetApplicationStatusAsync(userId, appId)
├─ Fetch app by ID
└─ Verify: app.UserId == userId ✓ else throw UnauthorizedAccessException
```

---

## 🧪 Testability Architecture

```
Service Layer (Testable via Mocking)
     ↑
     │ depends on
     │
     └─ ILoanApplicationRepository (Interface/Contract)
        
For Testing:
├─ Create Mock<ILoanApplicationRepository>
├─ Setup mock behaviors:
│  ├─ mockRepo.Setup(r => r.GetByIdAsync(...)).ReturnsAsync(mockApp)
│  ├─ mockRepo.Setup(r => r.AddAsync(...)).ReturnsAsync
│  └─ mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync
├─ Inject mock into service constructor
├─ Call service methods
└─ Assert and Verify mock calls

Example Unit Test:
┌──────────────────────────────────────────────┐
│ [Test]                                       │
│ CreateApplicationAsync_ValidData_Success()   │
│                                              │
│ 1. Arrange: Create mock repo + service      │
│ 2. Act: Call CreateApplicationAsync()       │
│ 3. Assert: Check response Success = true    │
│ 4. Verify: AddAsync + SaveChangesAsync      │
│           called exactly once                │
└──────────────────────────────────────────────┘
```

---

## 📋 File Dependency Map

```
Controllers/
├─ ApplicationsController
   └─ Depends on: ILoanApplicationService

Services/
├─ LoanApplicationService
   └─ Depends on:
      ├─ ILoanApplicationRepository
      ├─ Enums/ApplicationStatus
      └─ DTOs/ApplicationResponseDto, etc.

Interfaces/
├─ ILoanApplicationService
│  └─ Depends on: DTOs/ApplicationResponseDto, etc.
└─ ILoanApplicationRepository
   └─ Depends on: Domain/Entities/LoanApplication

DTOs/
├─ CreateApplicationRequestDto (no dependencies)
├─ UpdateApplicationRequestDto (no dependencies)
├─ SubmitApplicationRequestDto (no dependencies)
├─ ApplicationResponseDto (no dependencies)
└─ ApplicationStatusDto (no dependencies)

Enums/
└─ ApplicationStatus (no dependencies)

Domain/
└─ Entities/LoanApplication
   └─ Depends on: Enums/ApplicationStatus

Persistence/
├─ ApplicationDbContext
│  └─ Depends on: Domain/Entities/LoanApplication
└─ LoanApplicationRepository (implements ILoanApplicationRepository)
   └─ Depends on:
      ├─ ApplicationDbContext
      ├─ Interfaces/ILoanApplicationRepository
      └─ Domain/Entities/LoanApplication
```

---

## 🎯 Usage Pattern

```
/* In Program.cs (Dependency Injection Setup) */
builder.Services.AddScoped<ILoanApplicationService, LoanApplicationService>();
builder.Services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();

/* In Controller */
public class ApplicationsController : ControllerBase
{
    private readonly ILoanApplicationService _service;
    
    public ApplicationsController(ILoanApplicationService service)
    {
        _service = service;  // Injected by DI container
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicationRequestDto request)
    {
        var userId = GetUserIdFromJwt();  // Extract from Claims
        var result = await _service.CreateApplicationAsync(userId, request);
        return Ok(result);
    }
    
    [HttpGet("my")]
    public async Task<IActionResult> GetMy([FromQuery] int pageNumber = 1)
    {
        var userId = GetUserIdFromJwt();
        var (apps, total) = await _service.GetMyApplicationsAsync(userId, pageNumber);
        return Ok(new { applications = apps, totalCount = total });
    }
}

/* In Unit Test */
[Test]
public async Task CreateApplication_ValidData_Success()
{
    // Arrange
    var mockRepo = new Mock<ILoanApplicationRepository>();
    var service = new LoanApplicationService(mockRepo.Object);
    var userId = Guid.NewGuid();
    var request = new CreateApplicationRequestDto { /* ... */ };
    
    // Act
    var result = await service.CreateApplicationAsync(userId, request);
    
    // Assert
    Assert.IsTrue(result.Success);
    mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
}
```

---

## 📦 NuGet Dependencies (What to Install)

For the **CapFinLoan.Application.Application** project:

```xml
<ItemGroup>
    <!-- Entity Framework Core for database operations -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    
    <!-- Optional: For logging within service -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
</ItemGroup>

<!-- Project References -->
<ItemGroup>
    <ProjectReference Include="CapFinLoan.Application.Domain.csproj" />
    <!-- Domain layer entities (LoanApplication) -->
</ItemGroup>
```

For the **CapFinLoan.Application.Persistence** project:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
</ItemGroup>

<ItemGroup>
    <ProjectReference Include="CapFinLoan.Application.Domain.csproj" />
    <ProjectReference Include="CapFinLoan.Application.Application.csproj" />
</ItemGroup>
```

---

## 🎬 Quick Start: 3 Steps to Integration

### Step 1: Add to Program.cs
```csharp
builder.Services.AddScoped<ILoanApplicationService, LoanApplicationService>();
builder.Services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Step 2: Create Repository Implementation
- File: `CapFinLoan.Application.Persistence/Repositories/LoanApplicationRepository.cs`
- Implement: `ILoanApplicationRepository`

### Step 3: Create Controller
- File: `CapFinLoan.Application.API/Controllers/ApplicationsController.cs`
- Inject: `ILoanApplicationService`
- Endpoints: 5 HTTP endpoints (POST, PUT, GET, etc.)

---

**Architecture Status:** ✅ Complete and Ready for Integration  
**Next:** Domain Layer, Persistence Layer, API Controllers  
**Interview Ready:** Yes - Production-quality code with SOLID principles
