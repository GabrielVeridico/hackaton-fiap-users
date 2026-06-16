# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/FGC.Users.API

# Run all tests (42 tests)
dotnet test

# Run a specific test class
dotnet test --filter "FullyQualifiedName~CreateUserCommandHandlerTests"

# Run a single test
dotnet test --filter "Name=HandleAsync_WithValidCommand_ShouldReturnSuccess"

# Docker (SQL Server + API)
docker compose up --build

# EF Core migrations
dotnet ef migrations add MigrationName --project src/FGC.Users.Infrastructure --startup-project src/FGC.Users.API
dotnet ef database update --project src/FGC.Users.Infrastructure --startup-project src/FGC.Users.API
```

## Architecture

This is a .NET 9.0 Web API following **Clean Architecture** with **CQRS** pattern, organized in `src/` and `tests/`:

### Source Projects (`src/`)

- **Domain** (`FGC.Users.Domain`) — Pure domain layer with no external dependencies. Contains `User` entity (with private setters, factory method), `Password` value object (validation only, no hashing), `UserRole` enum, domain events (`UserRegistered`, `UserProfileUpdated`), and Result pattern (`Result`, `Result<T>`, `Error`).

- **Application** (`FGC.Users.Application`) — CQRS handlers, validators, interfaces, DTOs, and error definitions.
  - **Commands**: `CreateUser`, `AuthenticateUser`, `UpdateProfile` — each with Command record, Handler, and Validator (FluentValidation).
  - **Queries**: `GetProfile` — with Query record and Handler.
  - **Interfaces**: `IUserRepository`, `IAuditService`, `IEventPublisher`, `IPasswordHasher`, `IJwtTokenGenerator`.
  - All handlers return `Result<T>` instead of throwing exceptions.

- **Infrastructure** (`FGC.Users.Infrastructure`) — EF Core persistence, audit, event publishers, JWT, BCrypt.
  - `ApplicationDbContext` with `ApplyConfigurationsFromAssembly`.
  - `UserConfiguration`: owned `Password` (column "PasswordHash"), `Role` as string, `IsActive` global query filter.
  - `UserRepository` implements `IUserRepository`.
  - `BcryptPasswordHasher` implements `IPasswordHasher`.
  - `JwtTokenGenerator` implements `IJwtTokenGenerator`.
  - `AuditService` with before/after JSON snapshots.
  - `InMemoryEventPublisher` / `ServiceBusEventPublisher`.

- **API** (`FGC.Users.API`) — Controllers, middlewares, DI wiring.
  - `UsersController`: `/api/users/register`, `/api/users/me` (GET/PUT).
  - `AuthController`: `/api/auth/login` (separated from users).
  - `HealthController`: `/health`, `/ready`.
  - `CorrelationMiddleware` + `RequestResponseLoggingMiddleware` (password masking).

### Test Project (`tests/`)

- **UnitTests** (`FGC.Users.UnitTests`) — 42 tests covering all handlers, validators, domain entities, and value objects.

## API Endpoints

- `POST /api/users/register` — Register (name, email, password)
- `POST /api/auth/login` — Authenticate, returns JWT
- `GET /api/users/me` — Get profile (requires auth)
- `PUT /api/users/me` — Update profile (requires auth)
- `GET /health`, `GET /ready` — Health probes

## Testing Patterns

- **xUnit** with **Moq** for mocking and **FluentAssertions** for assertions
- All handlers tested via mocked interfaces (no EF InMemory needed for handler tests)
- FluentValidation `TestHelper` for validator tests
- AAA pattern (Arrange-Act-Assert)
- Domain tests for `User` entity and `Password` value object

## Key Configuration

Environment variables / config keys:
- `ConnectionStrings:Default` — SQL Server connection string
- `Jwt:Secret` (required, 32+ chars), `Jwt:Issuer`, `Jwt:Audience`
- `ServiceBus:ConnectionString` — Optional; falls back to InMemoryEventPublisher
- `ApplicationInsights:ConnectionString` — Optional Azure Monitor connection

SQL Server configured with `EnableRetryOnFailure` (5 retries, 30s delay). EF migrations auto-applied on startup.
