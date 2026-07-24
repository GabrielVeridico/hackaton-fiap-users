# HackatonFiap.Users — UserAPI

Microsserviço de **Identidade & Acesso** da plataforma **Conexão Solidária** (Hackathon FIAP PosTech): cadastro de usuários (PF/PJ), autenticação **JWT + refresh token rotativo** e **RBAC** por papel. É um bounded context isolado — **não publica eventos cross-service**.

> **Ecossistema (6 repos):** `users` (este) · `donations` · `payments` · `notifications` · `front` · `orchestration`. Mapa completo no [orchestration](https://github.com/GabrielVeridico/hackaton-fiap-orchestration#-ecossistema).

## Stack & escolhas
- **.NET 8 / ASP.NET Core**, **Clean Architecture** (Domain → Application → Infrastructure → API).
- **CQRS manual** (handlers + `Result<T>`, **sem MediatR**) — simplicidade e controle explícito do fluxo, sem dependência extra.
- **EF Core 8 + SQL Server** (database-per-service; migrations aplicadas no startup).
- **JWT** (access 4h) + **refresh token** rotativo 7d (hash SHA-256, detecção de reuso); **BCrypt** para senha.
- **Serilog** + **OpenTelemetry** (traces + métricas → `/metrics` Prometheus).
- Testes: **xUnit + NSubstitute + FluentAssertions** (57).

## Como rodar localmente
Pré-requisitos: **.NET 8 SDK** e **Docker** (para o SQL Server).

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# 1) SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# 2) Variáveis (em Development, Jwt__Key e Owner__Password são gerados e impressos no log)
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5001          # porta do ecossistema (docker-compose)
export ConnectionStrings__Default="Server=localhost,1433;Database=HackatonFiapUsersDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;"

# 3) Executar (migrations aplicam no startup)
dotnet run --project src/HackatonFiap.Users.API
```
API em `http://localhost:5001`. Ambiente completo (3 serviços + saga) em [orchestration/local](https://github.com/GabrielVeridico/hackaton-fiap-orchestration/tree/master/local). Alternativa: `docker compose up --build`.

> Fora de Development, `Jwt__Key` e `Owner__Password` são **obrigatórios** (fail-fast). Nunca commitados — em produção vêm do **Azure Key Vault**.

## Endpoints
**Auth (`/api/auth`, público):** `POST /register` (201) · `POST /login` → `{ accessToken, refreshToken, expiresIn }` · `POST /refresh` · `POST /logout` (204).

**Usuários (`/api/users`, autenticado):**

| Método | Rota | Permissão |
|--------|------|-----------|
| POST | `/api/users` | GestorONG/Owner |
| PUT | `/api/users/{id}` | GestorONG/Owner |
| PATCH | `/api/users/{id}/role` | **Owner** |
| PATCH | `/api/users/{id}/deactivate` · `/reactivate` | GestorONG/Owner |
| GET | `/api/users` · `/api/users/{id}` | GestorONG |
| GET/PUT | `/api/users/me` · POST `/api/users/me/reset-password` | Autenticado |

Papéis: `Doador`, `GestorONG` (o **Owner** é um GestorONG com `isOwner`, semeado no startup e imutável). Observabilidade: `/health`, `/ready`, `/metrics`.

```bash
# Exemplo: login
curl -X POST http://localhost:5001/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"owner@conexaosolidaria.org","password":"<senha do owner>"}'
```

## Testes
```bash
dotnet test --nologo   # 57 testes (domínio, handlers, validators)
```

## CI/CD
`.github/workflows/ci-cd.yml` (GitHub Actions): a cada push/PR na `main` → **build + test + build da imagem Docker** (sempre, sem depender de secrets). O job de **deploy é opcional/gated** por `vars.DEPLOY_TO_AKS == 'true'` — a CI passa verde sem credenciais Azure.

## Deploy
Gated no CI (push na `main` + `DEPLOY_TO_AKS=true`): `docker build`/push para o **ACR** → `kubectl apply` dos manifests (`.github/k8s/`) + `kubectl set image` no namespace **`conexao-solidaria`** do AKS. Segredos via **Key Vault + CSI + Workload Identity**. Runbook completo em [orchestration/iac/DEPLOY-AZURE.md](https://github.com/GabrielVeridico/hackaton-fiap-orchestration/blob/master/iac/DEPLOY-AZURE.md).

## Arquitetura & migrations
```
src/  Domain (entidades, VOs, Result<T>) · Application (CQRS handlers, DTOs) · Infrastructure (EF, BCrypt, JWT, OTel) · API (controllers, DI)
tests/ HackatonFiap.Users.UnitTests
```
EF Core é **local tool** (`.config/dotnet-tools.json`): `dotnet tool restore` → `dotnet ef migrations add <nome> --project src/HackatonFiap.Users.Infrastructure --startup-project src/HackatonFiap.Users.API` (o startup já aplica as migrations).
