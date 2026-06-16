# Arquitetura do Sistema — FIAP Cloud Games (FCG)

Documentação de arquitetura e fluxo de comunicação entre os microsserviços do projeto FCG, desenvolvido na **Fase 3 do Tech Challenge — PosTech FIAP**.

## Visão Geral da Arquitetura

O sistema é composto por **3 microsserviços independentes**, cada um com seu próprio banco de dados e repositório, comunicando-se de forma assíncrona via **Azure Service Bus (Queues)**.

```mermaid
graph TB
    subgraph "Clientes"
        WEB([Web / Mobile / Postman])
    end

    subgraph "Microsserviço de Usuários"
        UAPI[FGC.Users API<br/>:5081]
        UDB[(SQL Server<br/>FGCUsersDb)]
    end

    subgraph "Microsserviço de Jogos"
        GAPI[FCG.Games API<br/>:5105]
        GDB[(SQL Server<br/>FCGGamesDb)]
        GCON[ServiceBusConsumerService<br/>BackgroundService]
    end

    subgraph "Microsserviço de Pagamentos"
        PFUNC[FCG.Payments<br/>Azure Functions<br/>:5098]
        PDB[(SQL Server<br/>FCGPaymentsDb)]
    end

    subgraph "Infraestrutura"
        SB[/Azure Service Bus<br/>Queues - Plano Basic\]
        JWT{{JWT Token<br/>Chave Compartilhada}}
    end

    WEB -->|REST + JWT| UAPI
    WEB -->|REST + JWT| GAPI

    UAPI --> UDB
    GAPI --> GDB
    PFUNC --> PDB

    UAPI -.->|Gera Token JWT| JWT
    GAPI -.->|Valida Token JWT| JWT

    GAPI -->|OrderPlacedEvent| SB
    SB -->|queue: order-placed| PFUNC
    PFUNC -->|PaymentProcessedEvent| SB
    SB -->|queue: payments-processed| GCON
    GCON --> GAPI
```

## Diagrama de Comunicação entre Microsserviços

```mermaid
flowchart LR
    subgraph Users["FGC.Users :5081"]
        R[Register]
        L[Login]
        P[Profile]
    end

    subgraph Games["FCG.Games :5105"]
        CAT[Catálogo de Jogos]
        PUR[Compra de Jogo]
        LIB[Biblioteca]
        REC[Recomendações]
        CON[Consumer Service]
    end

    subgraph Payments["FCG.Payments :5098"]
        PPF[ProcessPaymentFunction]
    end

    subgraph ServiceBus["Azure Service Bus - Queues"]
        Q1[order-placed]
        Q2[payments-processed]
    end

    L -->|JWT Token| PUR
    PUR -->|OrderPlacedEvent| Q1
    Q1 -->|ServiceBusTrigger| PPF
    PPF -->|PaymentProcessedEvent| Q2
    Q2 -->|BackgroundService| CON
    CON -->|Approved: adiciona à biblioteca| LIB
```

## Fluxo Completo de Compra (E2E)

```mermaid
sequenceDiagram
    actor U as 🎮 Usuário
    participant UA as 👤 FGC.Users API
    participant GA as 🕹️ FCG.Games API
    participant Q1 as 📨 Queue: order-placed
    participant PF as ⚡ ProcessPaymentFunction
    participant Q2 as 📨 Queue: payments-processed
    participant CS as 🔄 ConsumerService

    rect rgb(59, 130, 246, 0.1)
        Note over U,CS: 🔐 1. Autenticação
        U->>+UA: POST /api/auth/login {email, password}
        UA-->>-U: ✅ {token, expiresAt}
    end

    rect rgb(16, 185, 129, 0.1)
        Note over U,CS: 🔍 2. Navegação
        U->>+GA: GET /api/games
        GA-->>-U: 📋 Lista de jogos [{id, title, price}]
    end

    rect rgb(245, 158, 11, 0.1)
        Note over U,CS: 🛒 3. Compra
        U->>+GA: POST /api/games/{id}/purchase [Bearer token]
        GA->>GA: Cria OrderGame (PendingPayment)
        GA-)Q1: OrderPlacedEvent {orderId, userId, gameId, price}
        GA-->>-U: 202 Accepted {orderId}
    end

    rect rgb(139, 92, 246, 0.1)
        Note over U,CS: 💳 4. Processamento Assíncrono
        Q1-)PF: ServiceBusTrigger dispara
        PF->>PF: Processa pagamento (centavos pares = Approved)
        PF-)Q2: PaymentProcessedEvent {orderId, status}
    end

    rect rgb(236, 72, 153, 0.1)
        Note over U,CS: 📦 5. Conclusão
        Q2-)CS: Consumer recebe evento
        alt ✅ Approved
            CS->>GA: Completa pedido + Adiciona à biblioteca
        else ❌ Rejected
            CS->>GA: Marca pedido como PaymentFailed
        end
    end

    rect rgb(20, 184, 166, 0.1)
        Note over U,CS: 📚 6. Verificação
        U->>+GA: GET /api/games/library [Bearer token]
        GA-->>-U: 🎮 Jogos na biblioteca do usuário
    end
```

## Padrões Arquiteturais

### Clean Architecture

Cada microsserviço segue Clean Architecture com 4 camadas:

```mermaid
graph LR
    subgraph "Camadas (dependência de fora para dentro)"
        API[API / Functions] --> APP[Application]
        APP --> DOM[Domain]
        INFRA[Infrastructure] --> APP
        API --> INFRA
    end
```

| Camada | Responsabilidade |
|--------|-----------------|
| **Domain** | Entidades, Value Objects, Eventos, Interfaces de repositório. Zero dependências externas. |
| **Application** | Commands, Queries, Handlers (CQRS), DTOs, Validadores. Depende apenas de Domain. |
| **Infrastructure** | EF Core, Service Bus, Repositórios, JWT, Audit. Implementa interfaces de Domain/Application. |
| **API / Functions** | Controllers, Middlewares, DI, Startup. Ponto de entrada HTTP ou trigger. |

### CQRS (Command Query Responsibility Segregation)

```mermaid
graph LR
    C[Controller] --> CMD[Command Handler]
    C --> QRY[Query Handler]
    CMD --> REPO[Repository Write]
    CMD --> EVT[Event Publisher]
    QRY --> REPOR[Repository Read]
    REPO --> DB[(Database)]
    REPOR --> DB
    EVT --> SB[/Service Bus\]
```

- **Commands**: CreateGame, UpdateGame, DeleteGame, PlaceOrder
- **Queries**: ListGames, GetGameById, GetRecommendations, GetUserLibrary
- **Events**: OrderPlacedEvent, PaymentProcessedEvent

### Comunicação entre Microsserviços

| De | Para | Mecanismo | Queue |
|----|------|-----------|-------|
| FCG.Games | FCG.Payments | Azure Service Bus (async) | `order-placed` |
| FCG.Payments | FCG.Games | Azure Service Bus (async) | `payments-processed` |
| Cliente | FGC.Users | REST (sync) | — |
| Cliente | FCG.Games | REST (sync) | — |

### Segurança

```mermaid
graph LR
    U[Usuário] -->|1. Login| UA[FGC.Users API]
    UA -->|2. JWT Token| U
    U -->|3. Bearer Token| GA[FCG.Games API]
    GA -->|4. Valida JWT<br/>mesma chave secreta| GA
```

- **JWT Bearer Token** compartilhado entre Users e Games (mesma chave, issuer e audience)
- **Roles**: `User` (comprar jogos) e `Admin` (CRUD de jogos)
- **Claim `sub`**: identificador único do usuário propagado nos eventos

### Observabilidade

| Componente | Implementação |
|------------|---------------|
| **Logs estruturados** | Serilog com Console + Application Insights sinks |
| **Correlation ID** | Middleware propaga `x-correlation-id` entre requests e eventos |
| **Audit Trail** | Games: override de `SaveChangesAsync` no EF Core. Users: `AuditService` com before/after JSON |
| **Tracing** | Serilog enrichers (MachineName, ThreadId, ServiceName) |

### Infraestrutura (Docker Compose)

```mermaid
graph TB
    subgraph "Docker Compose"
        SQL[SQL Server 2022<br/>:1433]
        SBSQL[Azure SQL Edge<br/>ServiceBus internal]
        SBE[Service Bus Emulator<br/>:5672]
        UAPI[fgc-users-api<br/>:5081]
        GAPI[fcg-games-api<br/>:5105]
        PFUNC[fcg-payments-func<br/>:5098]
    end

    SBSQL --> SBE
    SQL --> UAPI & GAPI & PFUNC
    SBE --> GAPI & PFUNC

    style SQL fill:#326CE5,color:#fff
    style SBE fill:#FF6B35,color:#fff
    style UAPI fill:#68BC71,color:#fff
    style GAPI fill:#68BC71,color:#fff
    style PFUNC fill:#9B59B6,color:#fff
```

| Container | Imagem | Porta |
|-----------|--------|-------|
| `fcg-sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 |
| `fcg-servicebus-sql` | `mcr.microsoft.com/azure-sql-edge:latest` | — |
| `fcg-servicebus` | `mcr.microsoft.com/azure-messaging/servicebus-emulator:latest` | 5672 |
| `fcg-users-api` | Build local (Alpine .NET 8) | 5081 |
| `fcg-games-api` | Build local (Alpine .NET 8) | 5105 |
| `fcg-payments-func` | Build local (Azure Functions .NET 8) | 5098 |
