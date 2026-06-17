# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository.

## Service overview

**HackatonFiap.Users** — Identity & Access microservice for the "Conexão Solidária" hackathon (FIAP PosTech). Handles user registration, authentication (JWT + refresh tokens), and user management. Self-contained: publishes **no cross-service events** in the MVP; auth/users are an isolated bounded context.

- **.NET 8** / ASP.NET Core Web API
- **Clean Architecture** — four source projects + one test project
- **CQRS** — manual command/query handlers returning `Result<T>` (no MediatR)
- **EF Core 8** (SQL Server), migrations applied automatically on startup
- **JWT** (access + refresh tokens), **BCrypt** for password hashing
- **Serilog** structured logging, **OpenTelemetry** (traces + metrics), **Prometheus** at `/metrics`
- Roles (pt-BR values): `Doador`, `GestorONG`
- Person types: `Individual` (PF/CPF), `Company` (PJ/CNPJ)
- **57 unit tests** — xUnit + NSubstitute + FluentAssertions

## Build & Run Commands

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# Build solution
dotnet build HackatonFiap.Users.sln -v minimal

# Run API (requires env vars below)
dotnet run --project src/HackatonFiap.Users.API

# Run all tests (57 tests)
dotnet test --nologo

# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~CreateUserCommandHandlerTests"

# Docker (SQL Server + API together)
docker compose up --build
```

## EF Core Migrations (local tool)

```bash
# Restore the local tool first (one-time per machine)
dotnet tool restore

# Add a migration
dotnet ef migrations add <MigrationName> \
  --project src/HackatonFiap.Users.Infrastructure \
  --startup-project src/HackatonFiap.Users.API

# Apply pending migrations manually
dotnet ef database update \
  --project src/HackatonFiap.Users.Infrastructure \
  --startup-project src/HackatonFiap.Users.API
```

The EF tooling is a **local** tool declared in `.config/dotnet-tools.json`; always run `dotnet tool restore` before `dotnet ef`.

## Running locally

Start SQL Server via Docker, then set environment variables and run:

```bash
# Start SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# Required env vars
export ConnectionStrings__Default="Server=localhost,1433;Database=HackatonFiapUsersDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;"
export ASPNETCORE_ENVIRONMENT=Development

# In Development, Jwt:Key and Owner:Password are auto-generated if absent.
# The generated values are printed once to the console on first startup.
# For non-Development environments they are REQUIRED (fail-fast):
# export Jwt__Key="<32-byte minimum random key>"
# export Owner__Password="<initial owner password>"

dotnet run --project src/HackatonFiap.Users.API
```

## Architecture

```
src/
├── HackatonFiap.Users.Domain/           # Entities, Value Objects, Result<T>, UserRole enum
├── HackatonFiap.Users.Application/      # CQRS handlers, DTOs, interfaces, error definitions
├── HackatonFiap.Users.Infrastructure/   # EF Core, BCrypt, JWT, Serilog, OpenTelemetry
└── HackatonFiap.Users.API/              # Controllers, middleware, DI wiring, Program.cs

tests/
└── HackatonFiap.Users.UnitTests/        # 57 unit tests
```

**Dependency flow:** Domain ← Application ← Infrastructure; API → Application + Infrastructure

### Domain layer

- `User` entity — private setters, factory method, `PersonType` (Individual/Company), `Document`, `UserRole` (Doador/GestorONG), `IsActive`
- `RefreshToken` entity
- `Document` value object (CPF/CNPJ validation)
- `Result<T>` / `Error` — used by all handlers instead of throwing exceptions

### Application layer

- **Commands**: `Register`, `Login`, `RefreshToken`, `Logout`, `CreateUser`, `UpdateUser`, `ChangeRole`, `ActivateUser`, `DeactivateUser`, `UpdateProfile`, `ResetPassword`
- **Queries**: `GetUsers`, `GetUserById`, `GetMe`
- Interfaces: `IUserRepository`, `IRefreshTokenRepository`, `IPasswordHasher`, `IJwtTokenGenerator`

### Infrastructure layer

- `ApplicationDbContext` — EF Core, configurations from assembly
- `UserRepository` / `RefreshTokenRepository`
- `BcryptPasswordHasher`
- `JwtTokenGenerator`
- OpenTelemetry setup (traces + metrics exported to Prometheus)

### API layer

- `AuthController` — `/api/auth/*`
- `UsersController` — `/api/users/*`
- ASP.NET Core Health Checks — `/health` (liveness, no dependency check) and `/ready` (readiness, checks SQL Server via `AddDbContextCheck<ApplicationDbContext>`)
- Prometheus metrics middleware — `/metrics`
- `CorrelationMiddleware`, `RequestResponseLoggingMiddleware` (password masking)

## API Endpoints

### Auth (public)

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/auth/register` | Donor self-registration — `{ personType, document, name, email, password }` → 201/400/409 |
| `POST` | `/api/auth/login` | Authenticate — `{ email, password }` → 200 `{ accessToken, refreshToken, expiresIn }` / 401 |
| `POST` | `/api/auth/refresh` | Refresh token — `{ refreshToken }` → 200/401 |
| `POST` | `/api/auth/logout` | Revoke refresh token (requires auth) — `{ refreshToken }` → 204 |

### Users (role-protected)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/users` | GestorONG/Owner | Create user `{ personType, document, name, email, password, role }` → 201/400/403/409 |
| `PUT` | `/api/users/{id}` | GestorONG/Owner | Update user `{ name }` → 200/403/404 |
| `PATCH` | `/api/users/{id}/role` | Owner only | Change role `{ role }` → 204/403/404 |
| `PATCH` | `/api/users/{id}/deactivate` | GestorONG/Owner | Deactivate → 204/403/404 |
| `PATCH` | `/api/users/{id}/reactivate` | GestorONG/Owner | Reactivate → 204/403/404 |
| `GET` | `/api/users` | GestorONG | List users → 200 |
| `GET` | `/api/users/{id}` | GestorONG | Get by id → 200/404 |

### Self-service (authenticated)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/users/me` | Get own profile |
| `PUT` | `/api/users/me` | Update own profile `{ name }` |
| `POST` | `/api/users/me/reset-password` | Change own password `{ currentPassword, newPassword }` |

### Observability

| Route | Description |
|-------|-------------|
| `GET /health` | Liveness probe (no dependency check) |
| `GET /ready` | Readiness probe — checks SQL Server connectivity (returns 503 when DB is unreachable) |
| `GET /metrics` | Prometheus metrics |

## Configuration & Security Model

Config keys (double-underscore = environment variable separator):

| Key | Env Var | Notes |
|-----|---------|-------|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | SQL Server connection string |
| `Jwt:Issuer` | `Jwt__Issuer` | Token issuer (`conexaosolidaria.local` default) |
| `Jwt:Audience` | `Jwt__Audience` | Token audience (`conexaosolidaria.clients` default) |
| `Jwt:Key` | `Jwt__Key` | Min 32 bytes. **Auto-generated in Development; required outside Development.** |
| `Owner:Email` | `Owner__Email` | Seed owner email |
| `Owner:Document` | `Owner__Document` | Seed owner CPF |
| `Owner:Name` | `Owner__Name` | Seed owner name |
| `Owner:Password` | `Owner__Password` | **Auto-generated in Development; required outside Development.** |
| `ApplicationInsights:ConnectionString` | `ApplicationInsights__ConnectionString` | Optional; disabled if empty |

**Security rule:** `Jwt:Key` and `Owner:Password` are **never committed to source**. In Development the app auto-generates them at startup and logs the values once. Outside Development the app fails fast if they are absent or empty.

## Testing Patterns

- **xUnit** + **NSubstitute** (mocking) + **FluentAssertions** (assertions)
- All handlers tested via mocked interfaces — no EF InMemory
- AAA pattern (Arrange / Act / Assert)
- Domain tests cover `User` entity, `Document` value object, `Result<T>`

## Key Dependencies

- `Microsoft.EntityFrameworkCore.SqlServer` 8.x
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.x
- `BCrypt.Net-Next`
- `Serilog.AspNetCore`
- `OpenTelemetry.AspNetCore`, `OpenTelemetry.Exporter.Prometheus.AspNetCore`
- `xunit`, `NSubstitute`, `FluentAssertions`
