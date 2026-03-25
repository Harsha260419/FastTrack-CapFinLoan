# Application Service Layer - Documentation

## Overview

This document explains the **Application/Core Service Layer** implementation for the CapFinLoan project. The service handles the complete loan application lifecycle using Clean Architecture, SOLID principles, and industry-standard patterns.

---

## Folder Structure

```
CapFinLoan.Application.Application/
├── DTOs/
│   ├── CreateApplicationRequestDto.cs          # Request: Create new draft application
│   ├── UpdateApplicationRequestDto.cs          # Request: Update draft application
│   ├── SubmitApplicationRequestDto.cs          # Request: Submit application for review
│   ├── ApplicationResponseDto.cs               # Response: Application details
│   └── ApplicationStatusDto.cs                 # Response: Status timeline
├── Enums/
│   └── ApplicationStatus.cs                    # Status lifecycle: Draft → Submitted → ... → Closed
├── Interfaces/
│   ├── ILoanApplicationService.cs              # Service contract (business logic)
│   └── ILoanApplicationRepository.cs           # Repository contract (data access)
├── Services/
│   └── LoanApplicationService.cs               # Service implementation
└── Class1.cs                                   # (Remove this template file)
```

---

## File Descriptions

### 1. **Enums/**

#### `ApplicationStatus.cs`
- **Purpose:** Defines the loan application status lifecycle
- **Statuses:** 
  ```
  Draft (1) 
    ↓ (Applicant submits)
  Submitted (2) 
    ↓ (Admin marks documents as pending)
  DocsPending (3) 
    ↓ (Documents uploaded and verified)
  DocsVerified (4) 
    ↓ (Admin initiates review)
  UnderReview (5) 
    ↓ (Admin makes decision)
  Approved/Rejected (6/7) 
    ↓ (Closed after decision)
  Closed (8)
  ```
- **Usage:** Prevents invalid status transitions
- **Key Feature:** Enum values numbered for database storage

---

### 2. **DTOs/** (Data Transfer Objects)

DTOs are **lightweight objects** used for API request/response communication. They separate the API contract from internal domain entities.

#### `CreateApplicationRequestDto.cs`
- **When Used:** POST /applications endpoint
- **Fields:**
  - `FullName` - Applicant's name
  - `Email` - Applicant's email
  - `PhoneNumber` - Contact number
  - `LoanAmount` - Amount requested
  - `LoanPurpose` - Purpose (Home/Personal/Business)
  - `TenureMonths` - Repayment period
- **Example:**
  ```csharp
  {
    "fullName": "John Doe",
    "email": "john@example.com",
    "phoneNumber": "9876543210",
    "loanAmount": 500000,
    "loanPurpose": "Home Loan",
    "tenureMonths": 120
  }
  ```

#### `UpdateApplicationRequestDto.cs`
- **When Used:** PUT /applications/{id} endpoint
- **Includes:** All fields from CreateApplicationRequestDto + ApplicationId
- **Restriction:** Only Draft applications can be updated
- **Use Case:** Applicant makes changes to draft before submission

#### `SubmitApplicationRequestDto.cs`
- **When Used:** POST /applications/{id}/submit endpoint
- **Fields:**
  - `ApplicationId` - Which application to submit
  - `DeclarationAccepted` - Boolean consent flag
  - `Comments` (optional) - Applicant's additional notes
- **Business Rule:** DeclarationAccepted must be true, application must be in Draft status

#### `ApplicationResponseDto.cs`
- **When Used:** Returned by all application endpoints
- **Fields:** Include all application details + metadata (CreatedAt, UpdatedAt, etc.)
- **Additional Fields:**
  - `Status` - Current status as string
  - `Success` - Operation success flag
  - `Message` - Human-readable message

#### `ApplicationStatusDto.cs`
- **When Used:** GET /applications/{id}/status endpoint
- **Purpose:** Show application journey through different statuses
- **Contains:** 
  - `CurrentStatus` - Present status
  - `Timeline` - List of StatusTimelineEntry objects (one per status transition)
  - Each timeline entry has: Status, TransitionDate, Remarks, NextAction

---

### 3. **Interfaces/**

Interfaces define **contracts** that implementations must follow. Enables **Dependency Injection** and **Testability** (mock implementations).

#### `ILoanApplicationService.cs`
- **Purpose:** Defines all business logic methods the service must implement
- **Methods & Signatures:**

| Method | Purpose | Parameters | Returns |
|--------|---------|-----------|---------|
| `CreateApplicationAsync` | Create draft app | userId, CreateApplicationRequestDto | ApplicationResponseDto |
| `UpdateApplicationAsync` | Update draft app | userId, UpdateApplicationRequestDto | ApplicationResponseDto |
| `SubmitApplicationAsync` | Submit for review | userId, SubmitApplicationRequestDto | ApplicationResponseDto |
| `GetMyApplicationsAsync` | Get user's apps (paginated) | userId, pageNumber, pageSize | (List<ApplicationResponseDto>, TotalCount) |
| `GetApplicationStatusAsync` | Get status timeline | userId, applicationId | ApplicationStatusDto |

**Key Design Decisions:**
- All methods are `async Task` (async/await pattern)
- `userId` parameter ensures authorization (from JWT token)
- DTOs used for all inputs/outputs (not domain entities)
- Methods throw specific exceptions for error scenarios

#### `ILoanApplicationRepository.cs`
- **Purpose:** Defines data access layer contract
- **Methods:** CRUD operations (AddAsync, UpdateAsync, GetByIdAsync, GetByUserIdAsync, etc.)
- **Abstraction:** Service doesn't know how data is persisted (SQL Server, NoSQL, etc.)
- **Benefit:** Easy to mock for unit testing

---

### 4. **Services/**

#### `LoanApplicationService.cs` - Implementation
- **File Size:** ~400 lines
- **Implements:** `ILoanApplicationService`
- **Dependencies:** `ILoanApplicationRepository` (injected)

**Key Features:**

##### Constructor (Dependency Injection)
```csharp
public LoanApplicationService(ILoanApplicationRepository repository)
{
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
}
```
- Service receives repository via constructor (Dependency Injection)
- Null check ensures repository is provided
- Enables easy unit testing (inject mock repository)

##### Method: `CreateApplicationAsync`
```csharp
public async Task<ApplicationResponseDto> CreateApplicationAsync(Guid userId, CreateApplicationRequestDto request)
```
**Flow:**
1. Validate input (required fields, ranges, email format)
2. Create LoanApplication entity with:
   - New GUID ApplicationId
   - UserId from JWT
   - Status = Draft
   - Timestamps (CreatedAt, UpdatedAt = now)
3. Call repository.AddAsync() (queues INSERT)
4. Call repository.SaveChangesAsync() (commits to DB)
5. Map entity to DTO and return with success message

**Validations:**
- FullName not empty
- Email contains "@"
- PhoneNumber length ≥ 10
- LoanAmount between 0 and 10,000,000
- TenureMonths between 1 and 360 months

##### Method: `UpdateApplicationAsync`
```csharp
public async Task<ApplicationResponseDto> UpdateApplicationAsync(Guid userId, UpdateApplicationRequestDto request)
```
**Key Differences from Create:**
1. Fetch existing application by ID
2. **Authorization Check:** Verify current user owns the application
   ```csharp
   if (application.UserId != userId)
       throw new UnauthorizedAccessException(...);
   ```
3. **Status Check:** Only allow updates if status is Draft
   ```csharp
   if (application.Status != ApplicationStatus.Draft)
       throw new InvalidOperationException(...);
   ```
4. Update fields and save

**Business Rule:**
- Cannot update submitted, under-review, or approved/rejected applications
- Ensures data integrity during processing

##### Method: `SubmitApplicationAsync`
```csharp
public async Task<ApplicationResponseDto> SubmitApplicationAsync(Guid userId, SubmitApplicationRequestDto request)
```
**Critical Flow:**
1. Validate declaration is accepted
2. Fetch application
3. Verify user ownership & Draft status
4. **Call ValidateApplicationForSubmission** - ensures all required fields filled
5. **Status Transition:** Draft → Submitted
6. Set SubmittedAt timestamp
7. Save to DB

**Business Rules:**
- Declaration must be accepted (legal requirement)
- All fields must be filled before submission
- Transition only allowed from Draft
- Sets SubmittedAt timestamp for audit trail

##### Method: `GetMyApplicationsAsync`
```csharp
public async Task<(List<ApplicationResponseDto>, int)> GetMyApplicationsAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
```
**Features:**
- **Pagination:** Support for large datasets
- **Validation:** pageNumber ≥ 1, pageSize between 1-100
- **Returns Tuple:** (applications list, total count)
- **Use Case:** Dashboard shows 10 applications per page

**Example Usage:**
```csharp
var (apps, total) = await service.GetMyApplicationsAsync(userId, pageNumber: 1, pageSize: 10);
Console.WriteLine($"Showing {apps.Count} of {total} applications");
```

##### Method: `GetApplicationStatusAsync`
```csharp
public async Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid userId, Guid applicationId)
```
**Purpose:** Show applicant the journey of their application
**Timeline Example:**
```
Status              Date                Remarks                             NextAction
Draft               2024-01-15 10:00    Application created in draft        Save and submit your application
Submitted           2024-01-16 14:30    Application submitted               Upload required documents for KYC
DocsVerified        2024-01-20 09:15    Documents have been verified        Wait for admin review
UnderReview         2024-01-21 11:00    Your application is under review    Decision within 3-5 business days
Approved            2024-01-25 16:45    Congratulations! Approved           Download loan approval letter
```

**Implementation Note:** 
- Timeline currently built from application entity timestamps
- Production: Should query separate `StatusHistory` audit table for complete history

---

## Private Helper Methods

The service includes private helper methods for reusability:

| Method | Purpose |
|--------|---------|
| `ValidateCreateApplicationRequest` | Validates all required fields for create |
| `ValidateUpdateApplicationRequest` | Validates all required fields for update |
| `ValidateApplicationForSubmission` | Ensures all fields before submission |
| `MapToApplicationResponseDto` | Converts domain entity to DTO |
| `BuildStatusTimeline` | Constructs timeline from application data |
| `GetStatusRemarks` | Returns human-readable remarks per status |
| `GetNextActionForStatus` | Returns next action text per status |

---

## SOLID Principles Applied

### 1. **Single Responsibility Principle (SRP)**
- `LoanApplicationService`: Only handles application business logic
- `ILoanApplicationRepository`: Only handles data access
- DTOs: Only represent data transfer contracts

### 2. **Open/Closed Principle (OCP)**
- Service is `closed` for modification (no direct DB calls)
- Service is `open` for extension via interface implementation

### 3. **Liskov Substitution Principle (LSP)**
- Repository implementations can be swapped without breaking service
- Mock repositories for testing adhere to same interface

### 4. **Interface Segregation Principle (ISP)**
- `ILoanApplicationService` only exposes methods used together
- Clients don't depend on methods they don't use

### 5. **Dependency Inversion Principle (DIP)**
- Service depends on `ILoanApplicationRepository` *abstraction*, not concrete class
- Enables dependency injection and unit testing

---

## Error Handling

The service throws **specific exceptions** for different scenarios:

```csharp
// Input validation
throw new ArgumentException("Loan amount must be greater than zero.");

// Authorization
throw new UnauthorizedAccessException("You do not have permission to update this application.");

// State violation
throw new InvalidOperationException($"Cannot submit application in {status} status.");

// Not found
throw new ArgumentException($"Application with ID {id} not found.");
```

**Controller should:**
1. Catch these exceptions
2. Map to appropriate HTTP status codes
   - `ArgumentException` → 400 Bad Request
   - `UnauthorizedAccessException` → 403 Forbidden
   - `InvalidOperationException` → 409 Conflict
   - `ArgumentNullException` → 400 Bad Request

---

## Async/Await Implementation

All methods are fully async:
```csharp
public async Task<ApplicationResponseDto> CreateApplicationAsync(...)
{
    // Don't block on repository calls
    await _repository.AddAsync(application);
    await _repository.SaveChangesAsync();
    
    // Return without .Result or .Wait()
    return dto;
}
```

**Benefits:**
- Non-blocking I/O (database queries don't lock threads)
- Scalable (single thread handles multiple requests)
- Better resource utilization

---

## Integration with Other Layers

### Domain Layer
- Service uses `Domain.Entities.LoanApplication` entity
- Dependency: `CapFinLoan.Application.Domain`

### Infrastructure/Persistence Layer
- Service uses `ILoanApplicationRepository`
- Repository implementation connects to EF Core DbContext
- Dependency: `CapFinLoan.Application.Infrastructure` (to be created)

### API Layer
- Controllers call service methods
- Controllers pass `UserId` from JWT claims
- Controllers map exceptions to HTTP responses

### Example Controller Integration:
```csharp
[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly ILoanApplicationService _service;
    
    public ApplicationsController(ILoanApplicationService service)
    {
        _service = service;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicationRequestDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            var result = await _service.CreateApplicationAsync(userId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

---

## Testing Strategy

### Unit Tests for Service
```csharp
[Test]
public async Task CreateApplicationAsync_WithValidData_ReturnsSuccessResponse()
{
    // Arrange
    var mockRepository = new Mock<ILoanApplicationRepository>();
    var service = new LoanApplicationService(mockRepository.Object);
    var userId = Guid.NewGuid();
    var request = new CreateApplicationRequestDto 
    { 
        FullName = "John Doe",
        Email = "john@example.com",
        // ... other fields
    };
    
    // Act
    var result = await service.CreateApplicationAsync(userId, request);
    
    // Assert
    Assert.IsTrue(result.Success);
    Assert.AreEqual("Draft", result.Status);
    mockRepository.Verify(r => r.AddAsync(It.IsAny<LoanApplication>()), Times.Once);
    mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
}

[Test]
public async Task UpdateApplicationAsync_WithInvalidStatus_ThrowsException()
{
    // Arrange
    var mockApp = new LoanApplication { Status = ApplicationStatus.Submitted };
    var mockRepository = new Mock<ILoanApplicationRepository>();
    mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync(mockApp);
    
    var service = new LoanApplicationService(mockRepository.Object);
    var request = new UpdateApplicationRequestDto { ApplicationId = Guid.NewGuid() };
    
    // Act & Assert
    Assert.ThrowsAsync<InvalidOperationException>(() => 
        service.UpdateApplicationAsync(Guid.NewGuid(), request));
}
```

---

## Configuration (Program.cs)

Add this to `Program.cs` to register the service:

```csharp
// Add services to the container
builder.Services.AddScoped<ILoanApplicationService, LoanApplicationService>();
builder.Services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>(); 
// ^ LoanApplicationRepository to be created in Infrastructure layer
```

---

## Next Steps

1. **Domain Layer** - Create `LoanApplication` entity class in `CapFinLoan.Application.Domain`
2. **Persistence Layer** - Create:
   - `ApplicationDbContext` (EF Core DbContext)
   - `LoanApplicationRepository` implementation
3. **API Layer** - Create `ApplicationsController` with endpoints
4. **Unit Tests** - Create test project with service tests

---

## Summary Table

| Component | Purpose | Key Features |
|-----------|---------|--------------|
| **DTOs** | API contracts | Lightweight, validation-friendly, no business logic |
| **Enums** | Status lifecycle | Type-safe status values, enforces valid transitions |
| **Interfaces** | Contracts | Enable DI, testability, separation of concerns |
| **Service** | Business logic | Validations, status transitions, authorization checks |
| **Repository** | Data access | Abstract DB operations from business logic |

---

**Created:** 2024  
**Architecture:** Clean Architecture + Microservices  
**Patterns:** SOLID, Repository Pattern, Dependency Injection, Async/Await
