# High-Level Design (HLD) - CapFinLoan

## Purpose
Provide a system-level view of the CapFinLoan platform for generating a complete HLD.

## Scope
- In scope:
  - Full loan lifecycle for applicants and admins
  - Microservices backend, API gateway, frontend UI
  - Messaging via RabbitMQ and SQL Server persistence
  - Document upload and verification workflow
  - Email notifications for OTP and decisions
- Out of scope:
  - Credit bureau integrations
  - Payment disbursal and repayment management
  - Production-grade HA infrastructure details

## Requirements Summary
### Functional
- Applicant signup with OTP, login, and JWT session
- Create, update, submit loan applications
- Upload and replace required documents
- Admin review queue and application details
- Admin document verification and decision capture
- Status tracking with history and timeline
- Email notifications for OTP and decisions

### Non-Functional
- Service isolation with independent databases
- Secure authentication and role-based access control
- Event-driven integration between services
- Reasonable performance for standard CRUD and review flows
- Operable via Docker Compose for local/dev

## System Overview
- Primary users:
  - Applicants (self-service)
  - Admins (review and decisions)
- Key workflows:
  - OTP signup and login
  - Loan application create/update/submit
  - Document upload and verification
  - Admin review and approve/reject
  - Notification emails
- Assumptions:
  - Gateway is the only public entry point for APIs
  - Services own their data and schema
  - RabbitMQ is available for async flows

## Architecture
### Logical Architecture
- Frontend: React SPA for applicant and admin experiences
- API Gateway: Ocelot for routing, JWT validation, claim enforcement
- Backend microservices:
  - Auth Service
  - Application Service
  - Document Service
  - Admin Service
  - Notification Service
- Shared contracts: messaging event and command DTOs
- Data stores: SQL Server per service, local file storage for documents

### Physical/Deployment Architecture
- Docker Compose orchestrates:
  - Frontend
  - API Gateway
  - Backend services
  - RabbitMQ (with management UI)
  - SQL Server
- Service databases are isolated by schema and DbContext
- Ports (default):
  - Frontend: 3000
  - Gateway: 8002
  - RabbitMQ mgmt: 15672
  - SQL Server: 1433

## Components and Responsibilities
- Frontend
  - Applicant and admin UIs, client-side routing
  - Auth/session management and API calls
- API Gateway
  - Single entrypoint for UI
  - JWT validation and route protection
- Auth Service
  - OTP generation/validation, signup, login
  - JWT creation with role and user claims
- Application Service
  - Loan application lifecycle and status timeline
  - Publish ApplicationSubmittedEvent
- Document Service
  - Document upload/replace and verification
  - Sync application status to DocsPending/DocsVerified
  - Publish DocumentsVerifiedEvent and DocumentsRequestedEvent
- Admin Service
  - Review queue, history, decision capture
  - Publish DecisionMadeEvent and ApplicationUnderReviewEvent
- Notification Service
  - Email OTP and decision messages

## Data Design
- SQL Server per service
  - Auth: users and signup OTPs
  - Application: loan applications
  - Document: document records and metadata
  - Admin: decisions and status history
- Local file storage for uploaded documents
- Cross-service links are logical via IDs (no FK across DBs)

## Integration Points
- Internal services
  - Admin -> Application (HTTP internal APIs)
  - Admin -> Document (HTTP internal APIs)
  - Document -> Application (status sync, RabbitMQ request/response)
- External systems
  - SMTP email server
- Messaging/queues (RabbitMQ)
  - OtpRequestedEvent (Auth -> Notification)
  - ApplicationSubmittedEvent (Application -> Document)
  - UpdateApplicationStatusCommand (Document -> Application)
  - DocumentsVerifiedEvent (Document -> Admin)
  - DecisionMadeEvent (Admin -> Notification)

## Security
- JWT Bearer authentication on gateway and services
- Role-based authorization (APPLICANT, ADMIN)
- Claims enforced in gateway for admin routes
- Email credentials stored in env/appsettings (not tracked in git)

## Scalability and Performance
- Stateless services allow horizontal scaling
- RabbitMQ decouples time-sensitive workflows
- Document files stored on disk with metadata in DB

## Availability and Resilience
- Retry for message consumers (3 attempts, 5s interval)
- Service isolation limits blast radius
- RabbitMQ required for async flows

## Observability
- Per-service logging to console/standard output
- RabbitMQ management UI for event monitoring
- Swagger/OpenAPI per service and aggregated via gateway

## Risks and Mitigations
- RabbitMQ unavailability impacts OTP and decision emails
- Local file storage limits horizontal scaling
- Document status sync requires broker availability (RabbitMqPrimary)

## Open Questions
- Production file storage (S3/Azure Blob) target
- Centralized logging and tracing approach
- SLA/SLO targets for API latency and availability
