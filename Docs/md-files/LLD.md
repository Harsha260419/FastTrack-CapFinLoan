# Low-Level Design (LLD) - CapFinLoan

## Purpose
Provide implementation-level details for key components so a complete LLD can be generated.

## Scope
- In scope:
  - Backend service responsibilities, APIs, and message contracts
  - Data model and status transitions
  - Frontend routes and flows
- Out of scope:
  - Production infra hardening and DR
  - Third-party credit or payment integrations

## Component Breakdown
### API Gateway (Ocelot)
- Responsibilities:
  - Single entry point for UI
  - JWT validation (GatewayJwt scheme)
  - Claim-based authorization for admin routes
- Interfaces:
  - Routes to Auth, Application, Document, Admin
- Dependencies:
  - Ocelot configuration

### Auth Service
- Responsibilities:
  - OTP generation and validation
  - Signup and login
  - JWT creation with role and user claims
- Interfaces:
  - POST /api/auth/signup/send-otp
  - POST /api/auth/signup
  - POST /api/auth/login
- Dependencies:
  - SQL Server (auth schema)
  - RabbitMQ publisher for OtpRequestedEvent

### Application Service
- Responsibilities:
  - Loan application CRUD
  - Status lifecycle and timeline
  - Publish ApplicationSubmittedEvent
  - Consume UpdateApplicationStatusCommand
- Interfaces:
  - POST /api/applications
  - PUT /api/applications/{id}
  - POST /api/applications/{id}/submit
  - GET /api/applications/my
  - GET /api/applications/{id}
  - GET /api/applications/{id}/status
  - DELETE /api/applications/{id}
  - Internal admin APIs for queue and details
- Dependencies:
  - SQL Server (core schema)
  - RabbitMQ consumer (status update command)

### Document Service
- Responsibilities:
  - Document upload/replace/fetch
  - Verify documents
  - Sync application status to DocsPending/DocsVerified
  - Publish DocumentsVerifiedEvent and DocumentsRequestedEvent
- Interfaces:
  - POST /api/documents/upload
  - PUT /api/documents/{id}
  - GET /api/documents/application/{applicationId}
  - GET /api/documents/{id}/file
  - PUT /api/admin/documents/{id}/verify
- Dependencies:
  - SQL Server (docs schema)
  - Local file storage
  - RabbitMQ request client (status sync)

### Admin Service
- Responsibilities:
  - Admin queue, dashboard, and history
  - Verify documents (delegates to Document Service)
  - Record decision and status history
  - Publish DecisionMadeEvent and ApplicationUnderReviewEvent
- Interfaces:
  - GET /api/admin/applications
  - GET /api/admin/applications/{id}
  - GET /api/admin/applications/{id}/history
  - GET /api/admin/dashboard
  - POST /api/admin/applications/{id}/decision
- Dependencies:
  - SQL Server (admin schema)
  - HTTP clients to Application and Document services
  - RabbitMQ publisher (DecisionMadeEvent)

### Notification Service
- Responsibilities:
  - Consume OtpRequestedEvent and DecisionMadeEvent
  - Send emails via SMTP
- Interfaces:
  - No public HTTP endpoints (consumer-only)
- Dependencies:
  - RabbitMQ consumers
  - SMTP configuration

### Frontend
- Responsibilities:
  - Applicant and admin UX
  - Route guards by auth and role
  - API integration via gateway
- Key routes:
  - /login, /signup
  - /applicant/dashboard, /applicant/apply, /applicant/application/:id
  - /admin/dashboard, /admin/queue, /admin/review/:id
- Dependencies:
  - React Router, Zustand, Axios, Tailwind CSS

## API Design
- Gateway base URL: http://localhost:8002
- Example gateway routes:
  - POST /gateway/auth/login
  - POST /gateway/auth/signup/send-otp
  - POST /gateway/applications/{id}/submit
  - GET /gateway/admin/applications
  - PUT /gateway/admin/documents/{documentId}/verify
  - POST /gateway/admin/applications/{id}/decision
- Error handling:
  - Standard 4xx for validation/authorization
  - 5xx for unhandled exceptions

## Data Model
- Auth
  - Users: Id, Name, Email, PhoneNumber, PasswordHash, Role, IsActive, CreatedAt
  - SignupOtps: Id, Email, OtpHash, ExpiresAtUtc, IsUsed, CreatedAtUtc
- Application
  - LoanApplications: ApplicationId, UserId, personal details, employment, income, loan details, Status, timestamps
- Document
  - Documents: Id, ApplicationId, UserId, FileName, FilePath, DocumentType, Status, Remarks, UploadedAt
- Admin
  - Decisions: DecisionId, ApplicationId, AdminUserId, DecisionStatus, Remarks, SanctionAmount, InterestRate, DecidedBy, DecidedAt
  - StatusHistory: HistoryId, ApplicationId, AdminUserId, FromStatus, ToStatus, ChangedBy, Remarks, ChangedAt

## Detailed Flows
### Signup with OTP
1. Applicant requests OTP via send-otp.
2. Auth Service stores hashed OTP and publishes OtpRequestedEvent.
3. Notification Service sends OTP email.
4. Applicant submits signup with OTP.
5. Auth Service validates OTP, creates user, returns JWT.

### Loan Application Lifecycle
1. Create draft (status Draft).
2. Update draft (only in Draft status).
3. Submit application (Draft -> Submitted) and publish ApplicationSubmittedEvent.
4. Document Service receives event and sets DocsPending or DocsVerified.
5. Admin sets UnderReview then Approved or Rejected.
6. Admin Service stores decision and status history.
7. Notification Service emails decision.

### Document Upload and Verification
1. Applicant uploads required documents.
2. Document Service validates file type and size.
3. Document Service recomputes overall doc state.
4. Status sync with Application Service via UpdateApplicationStatusCommand.
5. Admin verifies documents; on all verified, DocumentsVerifiedEvent published.

## Business Rules
- Only Draft applications can be edited or deleted.
- Submit requires complete data and declaration.
- Document types required: ID_PROOF, ADDRESS_PROOF, BANK_STATEMENT.
- Decisions allowed only when docs are verified or under review.

## Validation and Error Handling
- Server-side validation for required fields and enums.
- Authorization enforced by role claims.
- Document upload checks file type and size.

## Security Details
- JWT claims include Role and UserId.
- Role-based authorization at controller level.
- Gateway claim enforcement for admin routes.

## Configuration
- Docker Compose with env variables:
  - SA_USER, SA_PASSWORD
  - RABBITMQ_USERNAME, RABBITMQ_PASSWORD
  - EMAIL_SENDER, EMAIL_APP_PASSWORD
- Service appsettings override via env variables.

## Caching Strategy
- No explicit server-side caching in current design.

## Performance Considerations
- Use async I/O for file upload and DB access.
- RabbitMQ decouples latency-sensitive workflows.

## Testing Strategy
- Unit tests for service and domain logic.
- Integration tests for EF Core and API endpoints.
- Manual E2E flow validation via Docker Compose.

## Deployment Notes
- Run EF Core migrations manually in Docker:
  - docker exec capfinloan-authservice dotnet ef database update
  - docker exec capfinloan-applicationservice dotnet ef database update
  - docker exec capfinloan-documentservice dotnet ef database update
  - docker exec capfinloan-adminservice dotnet ef database update
- NotificationService appsettings not tracked in git to protect SMTP credentials.

## Open Questions
- External file storage and CDN strategy
- Centralized audit log and metrics
- CI pipeline coverage for all services
