# CapFinLoan Docker Setup

This repository can be started fully in Docker, including backend services, API gateway, frontend, RabbitMQ, and SQL Server.

## Prerequisites

- Docker Desktop with Docker Compose enabled
- At least 6 GB RAM available to Docker

## 1. Configure environment variables

The root .env file is used by docker-compose.yml.

Default template:

```env
SA_USER=sa
SA_PASSWORD=YourStrong@Password123
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
EMAIL_SENDER=
EMAIL_APP_PASSWORD=
```

Update the values before first run, especially SA_PASSWORD and email credentials.

## 2. Build and start all containers

```bash
docker-compose up --build -d
```

## 3. First-run EF Core migrations (manual)

SQL Server now runs in the sqlserver container, so existing local host databases are not reused. Fresh databases are created in the container.

Run these commands after containers are up:

```bash
docker exec capfinloan-authservice dotnet ef database update
docker exec capfinloan-applicationservice dotnet ef database update
docker exec capfinloan-documentservice dotnet ef database update
docker exec capfinloan-adminservice dotnet ef database update
```

## 4. Access points

- Frontend: http://localhost:3000
- API Gateway: http://localhost:8002
- RabbitMQ Management: http://localhost:15672
- SQL Server: localhost:1433

## Notes

- Service configuration overrides are injected through Docker environment variables using ASP.NET Core double-underscore notation.
- No appsettings.json files are modified for Docker runtime values.
- Migrations are intentionally not auto-run in Docker startup.
- Backend projects target net10.0, so Dockerfiles use .NET 10 SDK/ASP.NET runtime images to build successfully.
