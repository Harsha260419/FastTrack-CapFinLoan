# Backend RabbitMQ Context History

Date updated: 2026-04-10

## Purpose
This document captures the backend messaging migration and related service changes completed in this workspace so future prompts can continue with full context.

## High-level architecture decisions
- RabbitMQ is the internal async transport for backend microservice communication.
- SQL Server remains local LocalDB for service databases (not Docker).
- RabbitMQ runs in Docker.
- API Gateway remains HTTP routing only (no RabbitMQ integration).
- Internal HTTP endpoints remain in place where already used, but primary paths for migrated flows are RabbitMQ-based.

## Major flow migrations completed

### 1) Document status sync (DocumentService -> ApplicationService)
- Implemented with MassTransit request/response contracts.
- Status sync modes exist in DocumentService:
  - HttpOnly
  - DualWrite
  - RabbitMqPrimary
- Current target mode is RabbitMqPrimary.
- RabbitMqPrimary currently has no HTTP fallback (publish/await and fail naturally if broker is unavailable).

### 2) Decision notification flow (AdminService -> NotificationService)
- AdminService publishes DecisionMadeEvent after successful decision persistence.
- NotificationService consumes DecisionMadeEvent and sends decision email through SMTP.
- Implemented for APPROVED and REJECTED decisions.

### 3) OTP delivery flow (AuthService -> NotificationService)
- AuthService send-otp endpoint still generates OTP and stores hashed OTP in auth.SignupOtps exactly as before.
- Direct SMTP send in AuthService was removed.
- AuthService now publishes OtpRequestedEvent.
- NotificationService consumes OtpRequestedEvent and sends OTP email through SMTP.
- Signup OTP validation flow remains unchanged.

## Shared messaging contracts added
- DecisionMadeEvent
- OtpRequestedEvent
- Existing status-sync contracts remain in shared messaging contracts.

## Services and RabbitMQ status (current)
- AuthService: RabbitMQ publisher for OTP event.
- ApplicationService: RabbitMQ consumer for status update command with retry and fault handling.
- DocumentService: RabbitMQ request client for status sync, RabbitMqPrimary target mode.
- AdminService: RabbitMQ publisher for decision events.
- NotificationService: RabbitMQ consumers for decision and OTP events; sends email.
- ApiGateway: no RabbitMQ.

## Retry and fault behavior
- ApplicationService status consumer retry configured (3 attempts, 5-second interval) with fault handling path.
- NotificationService decision and OTP consumers retry configured (3 attempts, 5-second interval) with fault consumers logging DLQ/fault movement.
- DocumentService publisher-side retries were disabled for status sync path in RabbitMQ transport configuration.

## Startup orchestration changes
- start-services.ps1 includes RabbitMQ preflight/start/readiness checks before service startup.
- NotificationService startup entry added on port 5095.

## Configuration state highlights
- DocumentService appsettings currently set to RabbitMqPrimary and RabbitMQ enabled.
- ApplicationService appsettings has RabbitMQ enabled.
- AdminService appsettings has RabbitMQ enabled.
- AuthService appsettings has RabbitMQ block and SMTP block removed from AuthService runtime path.
- NotificationService appsettings contains RabbitMQ and Email SMTP settings.

## Security and git hygiene
- NotificationService appsettings is ignored in git to protect local SMTP credentials.
- File untracking for NotificationService appsettings was verified (already not tracked in index).
- Recommendation: rotate any exposed app passwords and keep secrets in user secrets or environment variables.

## New project introduced
- NotificationService API project created at:
  - CapFinLoan.Backend/NotificationService/CapFinLoan.Notification.API
- Characteristics:
  - Port 5095
  - No database
  - Consumer-only hosted service style app

## Build validation completed
These targets were built successfully after relevant changes:
- Shared messaging contracts
- DocumentService API
- ApplicationService API
- AdminService API
- NotificationService API
- AuthService API

## Known warnings observed
- MimeKit package advisory warning (moderate severity) in NotificationService dependency graph.
- Non-blocking package pruning warning in Admin.Infrastructure.

## Important constraints followed
- No RabbitMQ added to ApiGateway.
- No RabbitMQ added to AuthService login/signup validation flow beyond OTP publish.
- No HTTP fallback for RabbitMqPrimary in document status sync path.
- No feature-flag additions for the new OTP decision flows.

## Suggested next steps for future prompts
- End-to-end manual validation across all migrated flows with RabbitMQ management UI checks.
- Optional package upgrade to address MimeKit advisory.
- Move all SMTP credentials to secure secret storage if not already done.
- Add operational runbook entries only when requested.
