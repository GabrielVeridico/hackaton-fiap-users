# FGC.Users API

Microsserviço de cadastro, autenticação e gerenciamento de perfis de usuários, desenvolvido com .NET 8 e ASP.NET Core Web API. Projeto da **Fase 3 do Tech Challenge — PosTech FIAP**.

## Fluxo de Comunicação entre Microsserviços

```mermaid
graph LR
    Client([Cliente]) -->|HTTP| APIM[API Gateway]
    APIM -->|/api/users/**| Users[FGC.Users API]
    APIM -->|/api/games/**| Games[FCG.Games API]

    Games -->|OrderPlacedEvent| Q1[/order-placed/]
    Q1 -->|ServiceBusTrigger| Payments[FCG.Payments Function]
    Payments -->|PaymentProcessedEvent| Q2[/payments-processed/]
    Q2 -->|BackgroundService| Games
    Payments -->|PaymentProcessedEvent| Q3[/notifications-payment-processed/]
    Q3 -->|ServiceBusTrigger| Notifications[FGC.Notifications Function]

    Users --- DB1[(FGCUsersDb)]
    Games --- DB2[(FCGGamesDb)]
    Payments --- DB3[(FCGPaymentsDb)]

    Games -.->|Logs & Traces| AI[Application Insights]
    Users -.->|Logs & Traces| AI
    Payments -.->|Logs & Traces| AI
    Notifications -.->|Logs & Traces| AI
```

## Fluxo de Autenticação

```mermaid
sequenceDiagram
    participant C as Cliente
    participant U as FGC.Users API
    participant G as FCG.Games API
    participant DB as SQL Server

    C->>U: POST /api/users/register
    U->>DB: Cria User (BCrypt hash)
    U-->>C: 201 Created { id, name, email }

    C->>U: POST /api/auth/login
    U->>DB: Busca User por email
    U->>U: Verifica senha (BCrypt)
    U-->>C: 200 OK { token: "eyJhb..." }

    C->>U: GET /api/users/me (Bearer token)
    U->>DB: Busca perfil por userId (JWT claim)
    U-->>C: 200 OK { id, name, email, role }

    C->>G: POST /api/games/{id}/purchase (Bearer token)
    Note over G: Mesmo JWT emitido pelo Users<br/>é validado pelo Games API
```

## Diagrama de Arquitetura

```mermaid
graph TB
    subgraph "FGC.Users - Microsserviço de Usuários"
        subgraph "API Layer"
            UC[UsersController]
            AC[AuthController]
            HC[HealthController]
            CM[CorrelationMiddleware]
            RL[RequestResponseLoggingMiddleware]
        end

        subgraph "Application Layer"
            CUH[CreateUserCommandHandler]
            AUH[AuthenticateUserCommandHandler]
            UPH[UpdateProfileCommandHandler]
            GPH[GetProfileQueryHandler]
            CUV[CreateUserCommandValidator]
        end

        subgraph "Domain Layer"
            UE[User Entity]
            PVO[Password Value Object]
            RP[Result Pattern]
            DE[Domain Events]
        end

        subgraph "Infrastructure Layer"
            UR[UserRepository]
            PH[BcryptPasswordHasher]
            JTG[JwtTokenGenerator]
            AS[AuditService]
            EP[ServiceBusEventPublisher]
            DB[(SQL Server)]
        end
    end

    Client([Cliente HTTP]) --> CM --> UC & AC & HC
    UC --> CUH & UPH & GPH
    AC --> AUH
    CUH --> UE & CUV & UR & PH & AS & EP
    AUH --> UR & PH & JTG
    UPH --> UR & AS
    GPH --> UR
    UR --> DB
    AS --> DB
```

## Arquitetura

O projeto segue **Clean Architecture** com **CQRS**, organizado em 4 camadas:

```
src/
├── FGC.Users.Domain/           # Entidades, Value Objects, Eventos (zero dependências NuGet)
├── FGC.Users.Application/      # Commands, Queries, Handlers, Validators (FluentValidation), DTOs
├── FGC.Users.Infrastructure/   # EF Core (SQL Server), BCrypt, JWT, Audit, Service Bus
└── FGC.Users.API/              # Controllers, Middlewares, Startup (JWT + Swagger)
tests/
└── FGC.Users.UnitTests/        # 42 testes unitários (xUnit + Moq + FluentAssertions)
```

**Fluxo de dependências:** Domain ← Application ← Infrastructure; API → Application + Infrastructure

## Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/users/register` | Não | Registrar novo usuário |
| `POST` | `/api/auth/login` | Não | Autenticar e obter JWT |
| `GET` | `/api/users/me` | JWT | Obter perfil do usuário logado |
| `PUT` | `/api/users/me` | JWT | Atualizar perfil do usuário logado |
| `GET` | `/health` | Não | Health check |
| `GET` | `/ready` | Não | Readiness check |

## Domínio

### Entidades

| Entidade | Campos principais |
|----------|-------------------|
| `User` | Id, Name, Email, Password (VO), Role, IsActive, CreatedAtUtc, UpdatedAtUtc |
| `AuditEvent` | Id, AggregateType, AggregateId, EventType, BeforeJson, AfterJson, CreatedAtUtc, CorrelationId, TraceId, UserId |

### Eventos de Domínio

| Evento | Descrição |
|--------|-----------|
| `UserRegistered` | Emitido ao registrar usuário (UserId, Email, Name) |
| `UserProfileUpdated` | Emitido ao atualizar perfil (UserId, campos alterados) |

## Configuração

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ConnectionStrings__Default` | Connection string do SQL Server | (obrigatório) |
| `Jwt__Key` | Chave secreta para assinar tokens JWT (32+ chars) | `super-secret-key-for-dev-environment-only` |
| `Jwt__Issuer` | Emissor do token JWT | `fgc.local` |
| `Jwt__Audience` | Audiência do token JWT | `fgc.clients` |
| `ApplicationInsights__ConnectionString` | Application Insights (opcional) | (desabilitado se vazio) |

## CI/CD

Pipeline GitHub Actions (`.github/workflows/ci-cd.yml`):

- **CI** (push + PR na master): restore → build → test
- **CD** (apenas push na master): build Docker → push ACR → deploy Azure Container App

## Build & Run

```bash
# Build
dotnet build

# Executar API (http://localhost:5081)
dotnet run --project src/FGC.Users.API

# Executar testes (42 testes)
dotnet test
```

## Docker

```bash
# Build
docker build -f src/FGC.Users.API/Dockerfile -t fgc-users .

# Run
docker run -p 5081:8080 \
  -e ConnectionStrings__Default="Server=tcp:..." \
  -e Jwt__Key="super-secret-key-for-dev-environment-only" \
  fgc-users
```

## Testes

42 testes unitários com xUnit + Moq + FluentAssertions:

| Categoria | Testes |
|-----------|--------|
| Commands (CreateUser, AuthenticateUser, UpdateProfile) | 18 |
| Queries (GetProfile) | 4 |
| Validators (CreateUserCommandValidator) | 10 |
| Domain (User Entity, Password VO) | 10 |

## Observabilidade

- **Serilog** com sinks para Console e Application Insights
- **CorrelationMiddleware** propaga `x-correlation-id` entre requests
- **RequestResponseLoggingMiddleware** loga request/response com mascaramento de dados sensíveis
- **Audit Trail** com snapshots before/after de entidades
- **Application Insights** para logs, traces e métricas centralizados

## Tecnologias

- .NET 8.0 / ASP.NET Core Web API
- Entity Framework Core 8 (SQL Server)
- FluentValidation
- BCrypt.Net
- Serilog + Application Insights
- xUnit + Moq + FluentAssertions
