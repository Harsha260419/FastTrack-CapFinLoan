# 1. Project Overview

CapFinLoan is a full-stack loan processing platform built as a distributed microservices system. It supports the complete lending journey from user onboarding to final approval/rejection and notification.

The system is designed for:
- Applicant self-service (signup/login, create and submit loan applications)
- Document collection and verification
- Admin-side review and decision workflows
- Status tracking and timeline visibility
- Event-driven notifications (OTP and decision emails)

## Key Features

- Applicant authentication with OTP-assisted signup and JWT login
- Multi-step loan application form with draft/save/update/submit flows
- Document upload and replacement with file type and size validation
- Document verification by admin with status sync back to application lifecycle
- Admin queue, dashboard, review, status history, and decision capture
- Role-based authorization (APPLICANT and ADMIN)
- API Gateway routing and role claim enforcement
- RabbitMQ eventing for loose coupling between services

## Tech Stack

Backend:
- .NET 10 Web API per service
- Entity Framework Core + SQL Server
- MassTransit + RabbitMQ for messaging
- Ocelot API Gateway
- Swagger/OpenAPI per service + aggregated gateway docs
- JWT Bearer authentication/authorization

Frontend:
- React 18 + Vite
- React Router DOM
- Zustand (auth/session state)
- React Hook Form (complex forms)
- Axios for API calls
- Tailwind CSS for UI styling

Infrastructure:
- Docker Compose orchestration
- SQL Server 2022 container
- RabbitMQ with management plugin

---

# 2. Architecture

## Architecture Style

This is a microservices architecture (not monolith).

Evidence in codebase:
- Separate independently deployable services in `CapFinLoan.Backend` (`AuthService`, `ApplicationService`, `DocumentService`, `AdminService`, `NotificationService`, `ApiGateway`)
- Service-owned EF DbContexts and schemas (`auth`, `core`, `docs`, `admin`)
- Inter-service communication via both HTTP clients and RabbitMQ events/commands

## Services and Responsibilities

### 1) API Gateway
- Project: `CapFinLoan.Backend/ApiGateway/CapFinLoan.Gateway.API`
- Core file: `Program.cs`, `ocelot.json`
- Responsibilities:
  - Single entrypoint for frontend (`http://localhost:8002`)
  - JWT validation (`GatewayJwt` scheme)
  - Route forwarding to backend services
  - Claim-based route restriction (admin route requires `Role=ADMIN`)

### 2) Auth Service
- Projects:
  - API: `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API`
  - Application: `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.Application`
  - Domain: `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.Domain`
  - Persistence: `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.Persistence`
- Core classes:
  - Controller: `AuthController`
  - Service: `AuthService`
  - Token generator: `JwtTokenService`
- Responsibilities:
  - Signup OTP generation/validation
  - Signup and login
  - JWT creation with role and user claims
  - Publishing `OtpRequestedEvent` for email delivery

### 3) Application Service
- Projects under `CapFinLoan.Backend/ApplicationService`
- Core classes:
  - Controller: `ApplicationsController`, `InternalApplicationsController`
  - Service: `LoanApplicationService`
  - Consumer: `UpdateApplicationStatusCommandConsumer`
- Responsibilities:
  - Loan application CRUD (for applicant)
  - Submission and status lifecycle management
  - Status timeline construction
  - Internal admin-facing application querying/status updates
  - Publishing `ApplicationSubmittedEvent`

### 4) Document Service
- Projects under `CapFinLoan.Backend/DocumentService`
- Core classes:
  - Controller: `DocumentsController`, `InternalAdminDocumentsController`
  - Service: `DocumentService`
  - Consumer: `ApplicationSubmittedEventConsumer`
  - Storage: `LocalFileStorageService`
- Responsibilities:
  - Document upload/replace/fetch
  - Admin verification of documents
  - Required-document completeness and verification checks
  - Syncing application status to `DocsPending` or `DocsVerified`
  - Publishing `DocumentsVerifiedEvent` and `DocumentsRequestedEvent`

### 5) Admin Service
- Projects under `CapFinLoan.Backend/AdminService`
- Core classes:
  - Controller: `AdminController`
  - Service: `AdminService`
  - Consumer: `DocumentsVerifiedEventConsumer`
  - Clients: `ApplicationClient`, `DocumentClient`
- Responsibilities:
  - Admin queue/details/dashboard/history APIs
  - Verify documents (delegates to Document Service)
  - Decision capture (`UNDER_REVIEW`, `APPROVED`, `REJECTED`)
  - Status transition + status history persistence
  - Publishing `ApplicationUnderReviewEvent` and `DecisionMadeEvent`

### 6) Notification Service
- Project: `CapFinLoan.Backend/NotificationService/CapFinLoan.Notification.API`
- Core classes:
  - Consumers: `OtpRequestedEventConsumer`, `DecisionMadeEventConsumer`
  - Email sender: `SmtpEmailSender`
- Responsibilities:
  - Send OTP email from `OtpRequestedEvent`
  - Send decision email from `DecisionMadeEvent`

### 7) Shared Messaging Contracts
- Project: `CapFinLoan.Backend/Shared/CapFinLoan.Messaging.Contracts`
- Contains event/command contracts shared by producers and consumers.

## Communication Patterns

### Synchronous REST (HTTP)
- Frontend -> Gateway -> Service
- Admin Service -> Application Service (internal APIs)
- Admin Service -> Document Service (internal APIs)
- Document Service -> Application Service (validation + status sync)

### Asynchronous Messaging (RabbitMQ via MassTransit)
- Auth -> Notification: `OtpRequestedEvent`
- Application -> Document: `ApplicationSubmittedEvent`
- Document -> Application: `UpdateApplicationStatusCommand` (request/response)
- Document -> Admin: `DocumentsVerifiedEvent`
- Admin -> Notification: `DecisionMadeEvent`
- Admin -> (optional event consumers/workflow): `ApplicationUnderReviewEvent`

## High-Level System Flow

1. User signs up and logs in.
2. User creates loan application (Draft) and submits it.
3. Application Service publishes `ApplicationSubmittedEvent`.
4. Document Service receives event and initiates document workflow.
5. Applicant uploads required documents.
6. Document Service sets application status to `DocsPending` or `DocsVerified`.
7. Admin verifies documents, reviews application, marks `UNDER_REVIEW`, then `APPROVED`/`REJECTED`.
8. Admin Service stores decision and status history.
9. Notification Service emails the decision.

---

# 3. Folder Structure

## Backend Structure

```text
CapFinLoan.Backend/
  AuthService/
    CapFinLoan.Auth.API/
    CapFinLoan.Auth.Application/
    CapFinLoan.Auth.Domain/
    CapFinLoan.Auth.Infrastructure/
    CapFinLoan.Auth.Persistence/

  ApplicationService/
    CapFinLoan.Application.API/
    CapFinLoan.Application.Application/
    CapFinLoan.Application.Domain/
    CapFinLoan.Application.Infrastructure/
    CapFinLoan.Application.Persistence/

  DocumentService/
    CapFinLoan.Document.API/
    CapFinLoan.Document.Application/
    CapFinLoan.Document.Domain/
    CapFinLoan.Document.Infrastructure/
    CapFinLoan.Document.Persistence/

  AdminService/
    CapFinLoan.Admin.API/
    CapFinLoan.Admin.Application/
    CapFinLoan.Admin.Domain/
    CapFinLoan.Admin.Infrastructure/
    CapFinLoan.Admin.Persistence/

  NotificationService/
    CapFinLoan.Notification.API/

  ApiGateway/
    CapFinLoan.Gateway.API/

  Shared/
    CapFinLoan.Messaging.Contracts/
```

Explanation:
- `*.API`: controllers, middleware pipeline, startup/DI
- `*.Application`: business use cases, DTOs, interfaces, validations
- `*.Domain`: entities/enums/value concepts
- `*.Infrastructure`: external integration (JWT, HTTP clients, storage, options)
- `*.Persistence`: EF DbContext, repositories, migrations
- `Shared/CapFinLoan.Messaging.Contracts`: event and command message contracts

## Frontend Structure

```text
CapFinLoan.Frontend/
  src/
    api/
      axiosInstance.js
      authService.js
      adminService.js
    assets/
    components/
      LoadingSpinner.jsx
      Modal.jsx
      Navbar.jsx
      PageCard.jsx
      PageTitle.jsx
      Sidebar.jsx
      StatusBadge.jsx
    layouts/
      MainLayout.jsx
      ApplicantLayout.jsx
      AdminLayout.jsx
    pages/
      public/
      auth/
      applicant/
      admin/
    store/
      authStore.js
    utils/
      roleRoutes.jsx
    App.jsx
    main.jsx
```

Explanation:
- `api/`: API client setup and service wrappers
- `layouts/`: route layout shells (public, applicant, admin)
- `pages/`: feature pages by role/domain
- `store/`: global auth/session state (Zustand)
- `utils/roleRoutes.jsx`: route guards for auth and admin authorization

---

# 4. Backend Deep Dive (.NET)

## 4.1 Layers Explanation

### Domain Layer
Contains pure business entities and enums:
- `LoanApplication`, `ApplicationStatus`
- `Document`, `DocumentType`, `DocumentStatus`
- `User`, `SignupOtp`
- `Decision`, `ApplicationStatusHistory`

This layer has no external infra concerns.

### Application Layer
Contains business rules and orchestration:
- `AuthService` validates OTP, password strength, role normalization
- `LoanApplicationService` enforces lifecycle and transition rules
- `DocumentService` enforces required doc set + syncs app status
- `AdminService` handles decision logic and history recording

### Infrastructure/Persistence Layer
- Persistence:
  - EF `DbContext` classes
  - repositories for data operations
  - migrations
- Infrastructure:
  - JWT token service
  - HTTP clients between services
  - local file storage implementation
  - configuration options and external integration code

### API Layer
- Controllers define HTTP contracts and route permissions.
- `Program.cs` files configure:
  - DI registration
  - AuthN/AuthZ
  - EF connection and DbContext
  - MassTransit/RabbitMQ
  - Swagger and middleware

## 4.2 Key Concepts Used

### Dependency Injection
All services and repositories are registered in each service's `Program.cs`.
Examples:
- `AddScoped<IAuthService, AuthService>()`
- `AddScoped<ILoanApplicationService, LoanApplicationService>()`
- `AddHttpClient<IApplicationClient, ApplicationClient>()`

### Entity Framework Core
- Service-owned DbContext and schema:
  - Auth: `AuthDbContext` (schema `auth`)
  - Application: `ApplicationDbContext` (schema `core`)
  - Document: `DocumentsDbContext` (schema `docs`)
  - Admin: `AdminDbContext` (schema `admin`)
- Strong column constraints, indexes, precision, and uniqueness are defined in model builders.

### DTOs
Each service uses DTOs for request/response contracts.
Examples:
- `CreateApplicationRequestDto`, `SubmitApplicationRequestDto`
- `UploadDocumentDto`, `VerifyDocumentDto`
- `CreateDecisionRequestDto`

### Repository Pattern
Implemented and used through interfaces + concrete repositories, e.g.:
- `IUserRepository`, `UserRepository`
- `ILoanApplicationRepository`, `LoanApplicationRepository`
- `IDocumentRepository`, `DocumentRepository`
- `IDecisionRepository`, `DecisionRepository`

### Middleware and Auth
- JWT bearer authentication in each API service.
- Authorization attributes with role checks:
  - `[Authorize(Roles = "APPLICANT")]`
  - `[Authorize(Roles = "ADMIN")]`
- Global exception handling middleware is configured in each service.

## 4.3 Authentication Flow

1. Applicant requests OTP via `POST /api/auth/signup/send-otp`.
2. `AuthService` validates email, checks cooldown/duplicates, hashes OTP, stores in `SignupOtps`, publishes `OtpRequestedEvent`.
3. Notification Service consumes event and sends OTP email.
4. Applicant signs up via `POST /api/auth/signup` with OTP.
5. `AuthService` verifies OTP hash, expiry, and used status; creates `User` with normalized role.
6. Applicant logs in via `POST /api/auth/login`.
7. `JwtTokenService` generates token with claims:
   - `sub`, `email`
   - role claim + custom `Role`
   - custom `UserId` and `Email`
8. Frontend stores token in session storage and sends `Authorization: Bearer ...` automatically.

Role-based access:
- API/controller role attributes enforce APPLICANT/ADMIN.
- Gateway additionally enforces admin claim for `/gateway/admin/*` routes.

## 4.4 Loan Application Flow

Implementation centers on `LoanApplicationService`.

Step-by-step:
1. Create Draft
   - Endpoint: `POST /api/applications`
   - Creates `LoanApplication` with status `Draft`.
2. Update Draft
   - Endpoint: `PUT /api/applications/{id}`
   - Allowed only when status is `Draft` and owner matches user.
3. Submit Application
   - Endpoint: `POST /api/applications/{id}/submit`
   - Requires declaration accepted and complete data.
   - Status moves `Draft -> Submitted`.
   - `SubmittedAt` is set.
   - Publishes `ApplicationSubmittedEvent`.
4. Fetch and Track
   - `GET /api/applications/my`
   - `GET /api/applications/{id}`
   - `GET /api/applications/{id}/status` (builds timeline from admin history + fallback)
5. Delete Draft
   - `DELETE /api/applications/{id}` only when status is Draft.

Status transitions enforced by code:
- Submitted -> DocsPending or DocsVerified
- DocsPending <-> DocsVerified
- DocsVerified -> UnderReview
- UnderReview -> Approved/Rejected
- Approved/Rejected -> Closed

## 4.5 Document Management Flow

Implementation centers on `DocumentService`.

Required document types:
- `ID_PROOF`
- `ADDRESS_PROOF`
- `BANK_STATEMENT`
- `INCOME_PROOF`

Flow:
1. Upload/replace document (`/api/documents/upload` or `/api/documents/{id}`)
2. Validate ownership via Application Service (`ValidateApplicationAccessAsync`)
3. Validate file extension and size (`LocalFileStorageService`)
4. Persist metadata in `docs.Documents`
5. Recompute application document state:
   - Missing docs or any rejected -> `DocsPending`
   - All required docs verified -> `DocsVerified`
6. Sync status to Application Service (HTTP or messaging mode)
7. If `DocsVerified`, publish `DocumentsVerifiedEvent`
8. Admin verifies docs using `/api/admin/documents/{id}/verify` (through Admin Service -> Document Service)

## 4.6 Admin Flow

Implementation centers on `AdminService`.

1. Queue/Details
   - `GET /api/admin/applications`
   - `GET /api/admin/applications/{id}`
2. Document verification
   - `PUT /api/admin/documents/{id}/verify`
   - Records status history with before/after states.
3. Decision
   - `POST /api/admin/applications/{id}/decision`
   - Allowed decisions: `UNDER_REVIEW`, `APPROVED`, `REJECTED`
   - Updates application status via Application Service internal status endpoint
   - Persists/updates `Decision` row (sanction amount, interest rate, remarks, decided by)
   - Records `ApplicationStatusHistory`
   - Publishes:
     - `ApplicationUnderReviewEvent` when marked under review
     - `DecisionMadeEvent` for approved/rejected
4. Dashboard and History
   - `GET /api/admin/dashboard`
   - `GET /api/admin/applications/{id}/history`

EMI/decision calculations:
- No EMI formula computation in backend currently.
- Admin captures `SanctionAmount` and `InterestRate` explicitly in decision request.

## 4.7 Database Design

Service-owned data model:

Auth (`auth` schema):
- `Users`
  - Identity, role, credentials, active flag
- `SignupOtps`
  - OTP hash, expiry, used flag

Application (`core` schema):
- `LoanApplications`
  - Applicant profile + employment + loan request + status fields

Document (`docs` schema):
- `Documents`
  - ApplicationId, UserId, file metadata, document type, verification status
  - Unique index on `(ApplicationId, DocumentType)`

Admin (`admin` schema):
- `Decisions`
  - One per application (unique index on `ApplicationId`)
- `ApplicationStatusHistory`
  - Transition audit trail

Relationships:
- Cross-service relationships are logical by IDs (not SQL foreign keys across DBs).
- Example links:
  - `LoanApplications.UserId` -> `Users.Id`
  - `Documents.ApplicationId` -> `LoanApplications.ApplicationId`
  - `Decisions.ApplicationId` -> `LoanApplications.ApplicationId`

---

# 5. Frontend Deep Dive (React)

## Folder Structure

- Routing shell: `App.jsx`, `main.jsx`
- Layouts: `MainLayout`, `ApplicantLayout`, `AdminLayout`
- Role guards: `RequireAuth`, `RequireAdmin` in `utils/roleRoutes.jsx`
- Auth store: Zustand `store/authStore.js`
- Applicant pages:
  - `DashboardPage`, `MyApplicationsPage`
  - `ApplicantApplyPage` (multi-step form)
  - `ApplicationDetailPage` (timeline + document upload)
  - Placeholder pages: `ApplicantDocumentsPage`, `ApplicantStatusPage`
- Admin pages:
  - `AdminDashboardPage`, `AdminQueuePage`, `AdminReviewPage`, `AdminReportsPage`

## Routing

Defined in `App.jsx`:
- Public:
  - `/`
  - `/login`
  - `/signup`
- Applicant (auth required):
  - `/applicant/dashboard`
  - `/applicant/applications`
  - `/applicant/apply`
  - `/applicant/application/:id`
  - `/applicant/documents`
  - `/applicant/status` and `/applicant/status/:id`
- Admin (auth + role required):
  - `/admin/dashboard`
  - `/admin/queue`
  - `/admin/review/:id`
  - `/admin/reports`

## State Management

- Zustand store (`authStore.js`) manages:
  - token
  - role
  - email
  - userId
- State is mirrored in `sessionStorage`.
- Logout clears session storage.

## API Integration

- Axios base URL: `http://localhost:8002` (gateway)
- Request interceptor injects bearer token from session storage.
- Response interceptor redirects to `/login` on 401.
- Most pages call gateway routes such as `/gateway/auth/*`, `/gateway/applications/*`, `/gateway/admin/*`, `/gateway/documents/*`.

Note:
- `src/api/authService.js` and `src/api/adminService.js` currently use `/auth/*` and `/admin/*` routes directly and appear out of sync with actual page usage (`/gateway/*`).

## Key Page Behaviors

- `LoginPage`
  - Calls `/gateway/auth/login`
  - Normalizes role to uppercase and routes to applicant/admin area.
- `SignupPage`
  - OTP send -> `/gateway/auth/signup/send-otp`
  - Signup -> `/gateway/auth/signup` with `role: APPLICANT`
- `ApplicantApplyPage`
  - 4-step form with validation per step
  - Save draft (create/update)
  - Submit with declaration acceptance
- `ApplicationDetailPage`
  - Loads application summary + status timeline + documents
  - Handles document upload/replace and file preview
- `AdminReviewPage`
  - Loads application, history, docs
  - Verifies/rejects documents
  - Submits under-review/approve/reject decision

---

# 6. End-to-End Flow

User -> Login -> Apply Loan -> Upload Docs -> Admin Review -> Final Decision

## Detailed Walkthrough

1. User signup/login
- Signup requires OTP verification.
- Login returns JWT with role and user claims.

2. Applicant creates application
- Draft created in Application Service.
- Applicant can edit draft multiple times.

3. Applicant submits application
- Status becomes `Submitted`.
- `ApplicationSubmittedEvent` is published.

4. Document workflow starts
- Document Service consumes submit event and emits `DocumentsRequestedEvent`.
- Applicant uploads required documents.
- Document statuses move between `Pending`, `Verified`, `Rejected`.
- Application status synced to `DocsPending`/`DocsVerified`.

5. Admin review
- Admin queue shows submitted pipeline.
- Admin verifies each document.
- Status history is recorded.

6. Final decision
- Admin marks `UNDER_REVIEW`, then `APPROVED` or `REJECTED`.
- Decision details are persisted in admin DB.
- `DecisionMadeEvent` triggers notification email.

7. Applicant visibility
- Applicant sees current status and timeline through status endpoint and detail page.

---

# 7. API Endpoints Summary

## Auth Service

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/auth/signup/send-otp` | Generate and send signup OTP |
| POST | `/api/auth/signup` | Create account using OTP |
| POST | `/api/auth/login` | Authenticate and return JWT |

## Application Service

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/applications` | Create draft loan application |
| PUT | `/api/applications/{id}` | Update draft application |
| POST | `/api/applications/{id}/submit` | Submit application |
| DELETE | `/api/applications/{id}` | Delete draft application |
| GET | `/api/applications/my` | List current user applications |
| GET | `/api/applications/{id}` | Get application details |
| GET | `/api/applications/{id}/status` | Get current status + timeline |

Internal endpoints:

| Method | Route | Purpose |
|---|---|---|
| GET | `/internal/applications` | Admin/internal application queue |
| GET | `/internal/applications/{id}` | Admin/internal application detail |
| GET | `/internal/applications/{id}/status` | Admin/internal current status |
| PATCH | `/internal/applications/{id}/status` | Internal status transition |

## Document Service

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/documents/upload` | Upload document |
| PUT | `/api/documents/{id}` | Replace document |
| GET | `/api/documents/application/{applicationId}` | List docs by application |
| GET | `/api/documents/{id}/file` | Download/open stored file |

Internal admin endpoints:

| Method | Route | Purpose |
|---|---|---|
| GET | `/internal/admin/documents/{id}` | Fetch document metadata |
| PUT | `/internal/admin/documents/{id}/verify` | Verify/reject document |

## Admin Service

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/applications` | Admin queue list |
| GET | `/api/admin/applications/{id}` | Application detail for admin |
| POST | `/api/admin/applications/{id}/decision` | Submit review decision |
| GET | `/api/admin/dashboard` | Dashboard KPIs |
| GET | `/api/admin/applications/{id}/status` | Current status lookup |
| GET | `/api/admin/applications/{id}/history` | Status history timeline |
| PUT | `/api/admin/documents/{id}/verify` | Verify/reject document |

Additional internal admin endpoint:

| Method | Route | Purpose |
|---|---|---|
| GET | `/internal/admin/applications/{id}/history` | Internal history access |

## Gateway Upstream Routes (frontend-facing)

- `/gateway/auth/*` -> Auth Service `/api/auth/*`
- `/gateway/applications/*` -> Application Service `/api/applications/*`
- `/gateway/documents/*` -> Document Service `/api/documents/*`
- `/gateway/admin/*` -> Admin Service `/api/admin/*` (requires `Role=ADMIN` claim)

---

# 8. Key Design Decisions

## 1) Service-per-domain split
Why:
- Isolates concerns (identity, core application, docs, admin workflow, notifications).

Trade-off:
- More moving parts and operational complexity than monolith.

## 2) Hybrid communication (HTTP + messaging)
Why:
- HTTP for request/response workflows requiring immediate feedback.
- RabbitMQ for decoupled events and eventual consistency.

Trade-off:
- Requires idempotency/error handling and observability for async flows.

## 3) Service-owned databases/schemas
Why:
- Preserves bounded context ownership and independent schema evolution.

Trade-off:
- No cross-service SQL joins; consistency handled at application/event level.

## 4) API Gateway enforcement
Why:
- Centralized entrypoint and route-level claim restrictions.

Trade-off:
- Gateway configuration must remain synchronized with service endpoints.

## 5) Status normalization strategy
Why:
- Different services and frontend use different status text forms.
- Constants and normalizers reduce mismatch risk.

Trade-off:
- Additional mapping logic is required in multiple places.

---

# 9. How to Run the Project

## Option A: Docker (recommended for full environment)

1. Configure `.env` in repository root with required values:

```env
SA_USER=sa
SA_PASSWORD=YourStrong@Password123
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
EMAIL_SENDER=...
EMAIL_APP_PASSWORD=...
```

2. Start all services:

```bash
docker-compose up --build -d
```

3. Run EF migrations (first run):

```bash
docker exec capfinloan-authservice dotnet ef database update
docker exec capfinloan-applicationservice dotnet ef database update
docker exec capfinloan-documentservice dotnet ef database update
docker exec capfinloan-adminservice dotnet ef database update
```

4. Access endpoints:
- Frontend: `http://localhost:3000`
- Gateway: `http://localhost:8002`
- RabbitMQ UI: `http://localhost:15672`
- SQL Server: `localhost:1433`

## Option B: Local .NET run (non-docker)

1. Start services using helper script:

```powershell
./start-services.ps1
```

2. Script starts:
- Auth (5025)
- Application (5256)
- Document (5262)
- Admin (5088)
- Notification (5095)
- Gateway (8002)

3. Start frontend:

```bash
cd CapFinLoan.Frontend
npm install
npm run dev
```

## Database Migration/Data Split Script

Use:

```powershell
./scripts/migrate-service-databases.ps1
```

This script can:
- Ensure target service DBs exist
- Apply EF migrations per service
- Optionally copy data from a source consolidated DB into split service DBs

---

# 10. Possible Improvements

## Scaling Ideas

- Add centralized service discovery/config management for dynamic environments.
- Add horizontal scaling with per-service autoscaling (especially Application/Admin APIs).
- Move document files from local storage to cloud object storage (S3/Azure Blob) for durable scaling.
- Introduce distributed cache for heavy dashboard/queue queries.

## Security Improvements

- Add refresh token flow and token revocation strategy.
- Add stricter gateway rate limiting and request throttling.
- Encrypt sensitive PII fields at rest.
- Add anti-virus scanning for uploaded documents.
- Add explicit validation/error response contracts to reduce ambiguous client handling.

## Performance Optimizations

- Add database read models/index tuning for queue/dashboard filters.
- Batch or optimize status-history retrieval for timeline-heavy views.
- Add resilience policies (retry/circuit breaker) around inter-service HTTP.
- Introduce outbox/inbox patterns for reliable event publishing and idempotent consumption.

## Product/Codebase Improvements

- Replace placeholder applicant pages (`ApplicantDocumentsPage`, `ApplicantStatusPage`) with functional pages or route redirection to detail/status views.
- Consolidate frontend API wrappers so all requests consistently use `/gateway/*` paths.
- Add integration tests for complete workflow:
  - submit -> docs upload -> verify -> decision -> notification
- Add observability stack (structured logs + tracing + correlation ID dashboards).

---

## Interview Preparation Notes (Quick Recap)

If asked to explain this project in interviews, emphasize:
- It is a microservices lending platform using clean layered architecture per service.
- It combines synchronous and asynchronous integration patterns.
- It implements end-to-end role-based workflows for APPLICANT and ADMIN.
- It tracks lifecycle transitions with explicit status state management and audit history.
- It is containerized and supports service-level database ownership, which mirrors real enterprise design.