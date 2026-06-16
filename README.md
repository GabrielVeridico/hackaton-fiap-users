# HackatonFiap.Users — UserAPI

Microsserviço de **Identidade e Acesso** para a plataforma "Conexão Solidária" (FIAP PosTech Hackathon). Gerencia cadastro de usuários, autenticação JWT com refresh token e controle de acesso por papel.

**Self-contained:** não publica eventos cross-service no MVP; auth/users são um bounded context isolado.

## Tecnologias

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 (SQL Server) — migrations automáticas no startup
- JWT (access + refresh token), BCrypt
- Serilog, OpenTelemetry (traces + métricas), Prometheus em `/metrics`
- xUnit + NSubstitute + FluentAssertions (57 testes)

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker](https://www.docker.com/) (para SQL Server local)

## Rodando localmente

### 1. Suba o SQL Server

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Configure as variáveis de ambiente

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Default="Server=localhost,1433;Database=HackatonFiapUsersDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;"
```

Em **Development**, `Jwt__Key` e `Owner__Password` são **gerados automaticamente** se ausentes. Os valores gerados são impressos uma vez no console no primeiro startup — guarde a senha do Owner para acessar a API.

Em ambientes não-Development esses valores são **obrigatórios** (a aplicação falha no startup se estiverem vazios):

```bash
export Jwt__Key="<chave aleatória de 32+ bytes>"
export Owner__Password="<senha inicial do owner>"
```

### 3. Execute a API

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet run --project src/HackatonFiap.Users.API
```

A API estará disponível em `http://localhost:5081`. As migrations são aplicadas automaticamente.

### Alternativa: Docker Compose (SQL Server + API)

```bash
docker compose up --build
```

## Endpoints

### Autenticação (público)

```bash
# Registrar doador
curl -X POST http://localhost:5081/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"personType":"Individual","document":"52998224725","name":"João Silva","email":"joao@email.com","password":"Senha@123"}'
# → 201 Created

# Login
curl -X POST http://localhost:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"joao@email.com","password":"Senha@123"}'
# → 200 { "accessToken": "eyJ...", "refreshToken": "...", "expiresIn": 3600 }

# Renovar token
curl -X POST http://localhost:5081/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<refresh_token>"}'
# → 200 { "accessToken": "...", "refreshToken": "...", "expiresIn": 3600 }

# Logout (revoga refresh token)
curl -X POST http://localhost:5081/api/auth/logout \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<refresh_token>"}'
# → 204 No Content
```

### Usuários (autenticado)

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| `POST` | `/api/users` | GestorONG/Owner | Criar usuário com papel específico |
| `PUT` | `/api/users/{id}` | GestorONG/Owner | Atualizar nome |
| `PATCH` | `/api/users/{id}/role` | Owner | Alterar papel |
| `PATCH` | `/api/users/{id}/deactivate` | GestorONG/Owner | Desativar |
| `PATCH` | `/api/users/{id}/reactivate` | GestorONG/Owner | Reativar |
| `GET` | `/api/users` | GestorONG | Listar usuários |
| `GET` | `/api/users/{id}` | GestorONG | Buscar por ID |
| `GET` | `/api/users/me` | Autenticado | Perfil próprio |
| `PUT` | `/api/users/me` | Autenticado | Atualizar perfil próprio |
| `POST` | `/api/users/me/reset-password` | Autenticado | Alterar senha |

Papéis disponíveis: `Doador`, `GestorONG`

### Observabilidade

| Rota | Descrição |
|------|-----------|
| `GET /health` | Liveness probe |
| `GET /ready` | Readiness probe |
| `GET /metrics` | Métricas Prometheus |

## Testes

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test --nologo
```

57 testes unitários com **xUnit + NSubstitute + FluentAssertions**. Cobrem todos os handlers, validators e entidades de domínio.

## Migrations (EF Core)

A ferramenta EF é um **local tool** declarada em `.config/dotnet-tools.json`.

```bash
# Restaurar a ferramenta (uma vez por máquina)
dotnet tool restore

# Adicionar nova migration
dotnet ef migrations add <NomeDaMigration> \
  --project src/HackatonFiap.Users.Infrastructure \
  --startup-project src/HackatonFiap.Users.API

# Aplicar manualmente (opcional — o startup já aplica automaticamente)
dotnet ef database update \
  --project src/HackatonFiap.Users.Infrastructure \
  --startup-project src/HackatonFiap.Users.API
```

## Configuração

| Variável de Ambiente | Descrição |
|----------------------|-----------|
| `ConnectionStrings__Default` | Connection string do SQL Server |
| `Jwt__Issuer` | Emissor do JWT (padrão: `conexaosolidaria.local`) |
| `Jwt__Audience` | Audiência do JWT (padrão: `conexaosolidaria.clients`) |
| `Jwt__Key` | Chave secreta (32+ bytes). Auto-gerada em Development; **obrigatória** fora de Development. |
| `Owner__Email` | Email do usuário owner seed |
| `Owner__Document` | CPF do owner seed |
| `Owner__Name` | Nome do owner seed |
| `Owner__Password` | Senha do owner. Auto-gerada em Development (impressa no log); **obrigatória** fora de Development. |
| `ApplicationInsights__ConnectionString` | Application Insights (opcional) |

> **Segurança:** `Jwt__Key` e `Owner__Password` nunca devem estar em arquivos versionados. Em Development, o app os gera automaticamente e imprime no console. Em produção/staging, forneça via env vars ou Azure Key Vault.

## Arquitetura

```
src/
├── HackatonFiap.Users.Domain/           # Entidades, Value Objects, Result<T>
├── HackatonFiap.Users.Application/      # Handlers CQRS, DTOs, interfaces
├── HackatonFiap.Users.Infrastructure/   # EF Core, BCrypt, JWT, OpenTelemetry
└── HackatonFiap.Users.API/              # Controllers, middleware, DI

tests/
└── HackatonFiap.Users.UnitTests/        # 57 testes unitários
```

CI/CD: GitHub Actions — restore → build → test → Docker image → AKS deploy (`.github/workflows/ci-cd.yml`).
