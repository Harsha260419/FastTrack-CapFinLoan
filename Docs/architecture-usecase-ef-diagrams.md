# CapFinLoan Diagrams

This document provides three Mermaid diagrams based on the current codebase and Docker setup.

## 1) Architecture Diagram

```mermaid
flowchart LR
    A[Applicant User]
    B[Admin User]

    subgraph FE[Frontend]
      UI[React Frontend]
    end

    subgraph GW[API Gateway]
      OCELOT[Ocelot Gateway\nJWT validation + route claims]
    end

    subgraph MS[Backend Microservices]
      AUTH[Auth Service]
      APP[Application Service]
      DOC[Document Service]
      ADM[Admin Service]
      NOTIF[Notification Service]
    end

    subgraph INFRA[Infrastructure]
      MQ[RabbitMQ]
      SQL[(SQL Server)]
      FS[(Document Upload Storage)]
    end

    A --> UI
    B --> UI
    UI --> OCELOT

    OCELOT --> AUTH
    OCELOT --> APP
    OCELOT --> DOC
    OCELOT --> ADM

    AUTH --> SQL
    APP --> SQL
    DOC --> SQL
    ADM --> SQL
    DOC --> FS

    APP -. publishes ApplicationSubmittedEvent .-> MQ
    MQ -. delivers ApplicationSubmittedEvent .-> DOC
    DOC -. initial status sync DocsPending .-> APP
    DOC -. publishes DocumentsRequestedEvent .-> MQ
    DOC -. publishes DocumentsVerifiedEvent (all docs verified) .-> MQ
    MQ -. delivers DocumentsVerifiedEvent .-> ADM
    MQ -. delivers OtpRequestedEvent / DecisionMadeEvent .-> NOTIF

    ADM -->|internal HTTP| APP
    ADM -->|internal HTTP| DOC
    APP -->|internal HTTP callback| ADM
    DOC -->|internal HTTP fallback| APP
```

## 2) Use Case Diagram

```mermaid
flowchart LR
    AP[Applicant]
    AD[Admin]

    subgraph CAP[CapFinLoan System]
      UC1((Send Signup OTP))
      UC2((Sign Up))
      UC3((Login))
      UC4((Create Loan Application))
      UC5((Update/Delete Draft Application))
      UC6((Submit Application))
      UC7((View My Applications))
      UC8((Track Application Status))
      UC9((Upload/Replace Document))
      UC10((View Documents / Download File))
      UC11((View Application Queue))
      UC12((View Application Details))
      UC13((Verify Document))
      UC14((Record Decision Approve/Reject))
      UC15((View Dashboard & Status History))
      UC16((Auto Sync Status to DocsPending on Submit))
      UC17((Publish Documents Requested Event))
      UC18((Recompute App Status from Document States))
    end

    AP --- UC1
    AP --- UC2
    AP --- UC3
    AP --- UC4
    AP --- UC5
    AP --- UC6
    AP --- UC7
    AP --- UC8
    AP --- UC9
    AP --- UC10

    AD --- UC3
    AD --- UC10
    AD --- UC11
    AD --- UC12
    AD --- UC13
    AD --- UC14
    AD --- UC15

    UC6 -. includes .-> UC16
    UC6 -. includes .-> UC17
    UC9 -. includes .-> UC18
    UC13 -. includes .-> UC18
    UC18 -. updates .-> UC8
    UC14 -. persists decision and status transition .-> UC15
```

## 3) Database EF Diagram (ER)

```mermaid
erDiagram
    AUTH_USERS {
        uniqueidentifier Id PK
        nvarchar Name
        nvarchar Email UK
        nvarchar PhoneNumber
        nvarchar PasswordHash
        nvarchar Role
        bit IsActive
        datetime CreatedAt
    }

    AUTH_SIGNUP_OTPS {
        uniqueidentifier Id PK
        nvarchar Email
        nvarchar OtpHash
        datetime ExpiresAtUtc
        bit IsUsed
        datetime CreatedAtUtc
    }

    CORE_LOAN_APPLICATIONS {
        uniqueidentifier ApplicationId PK
        uniqueidentifier UserId
        nvarchar FirstName
        nvarchar LastName
        datetime DateOfBirth
        nvarchar Gender
        nvarchar AddressLine1
        nvarchar AddressLine2
        nvarchar City
        nvarchar State
        nvarchar PostalCode
        nvarchar EmployerName
        nvarchar EmploymentType
        decimal MonthlyIncome
        decimal AnnualIncome
        nvarchar FullName
        nvarchar Email
        nvarchar PhoneNumber
        decimal LoanAmount
        nvarchar LoanPurpose
        int TenureMonths
        int Status
        datetime CreatedAt
        datetime UpdatedAt
        datetime SubmittedAt
        nvarchar AdminRemarks
    }

    DOCS_DOCUMENTS {
        uniqueidentifier Id PK
        uniqueidentifier ApplicationId
        uniqueidentifier UserId
        nvarchar FileName
        nvarchar FilePath
        int DocumentType
        int Status
        nvarchar Remarks
        datetime UploadedAt
    }

    ADMIN_DECISIONS {
        uniqueidentifier DecisionId PK
        uniqueidentifier ApplicationId UK
        uniqueidentifier AdminUserId
        nvarchar DecisionStatus
        nvarchar Remarks
        decimal SanctionAmount
        decimal InterestRate
        nvarchar DecidedBy
        datetime DecidedAt
    }

    ADMIN_APPLICATION_STATUS_HISTORY {
        uniqueidentifier HistoryId PK
        uniqueidentifier ApplicationId
        uniqueidentifier AdminUserId
        nvarchar FromStatus
        nvarchar ToStatus
        nvarchar ChangedBy
        nvarchar Remarks
        datetime ChangedAt
    }

    AUTH_USERS ||--o{ CORE_LOAN_APPLICATIONS : "logical via UserId"
    CORE_LOAN_APPLICATIONS ||--o{ DOCS_DOCUMENTS : "logical via ApplicationId + status sync"
    CORE_LOAN_APPLICATIONS ||--o| ADMIN_DECISIONS : "logical via ApplicationId"
    CORE_LOAN_APPLICATIONS ||--o{ ADMIN_APPLICATION_STATUS_HISTORY : "logical via ApplicationId"
    AUTH_USERS ||--o{ ADMIN_DECISIONS : "logical via AdminUserId"
    AUTH_USERS ||--o{ ADMIN_APPLICATION_STATUS_HISTORY : "logical via AdminUserId"
    AUTH_USERS ||--o{ AUTH_SIGNUP_OTPS : "logical via Email"
```

## Notes

- The relationships above are mostly logical cross-service links by ID, not enforced SQL foreign keys across databases.
- Data is split per service (`auth`, `core`, `docs`, `admin` schemas) with service-owned EF Core contexts.
- API access is mediated through the Ocelot gateway, with role-based authorization for admin routes.