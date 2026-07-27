# HackatonFiap.Users — UserAPI

Microsserviço de identidade e acesso da plataforma **Conexão Solidária** (Hackathon FIAP PosTech). Cadastra usuários (pessoa física e jurídica), autentica com **JWT + refresh token rotativo** e controla o acesso por papel (RBAC). É um bounded context isolado: não consome nem publica eventos de integração.

> **Ecossistema (6 repositórios):** `users` (este) · `donations` · `payments` · `notifications` · `front` · `orchestration`. Mapa completo no [orchestration](https://github.com/GabrielVeridico/hackaton-fiap-orchestration#ecossistema).

## Stack

| Item | Escolha |
|------|---------|
| Runtime | .NET 8 / ASP.NET Core |
| Arquitetura | Clean Architecture — Domain → Application → Infrastructure → API |
| CQRS | Handlers manuais retornando `Result<T>` (sem MediatR) |
| Persistência | EF Core 8 + SQL Server (`HackatonFiapUsersDb`), migrations aplicadas no startup |
| Autenticação | JWT (access de 4 h) + refresh token rotativo de 7 dias, guardado como hash SHA-256 |
| Senha | BCrypt |
| Observabilidade | Serilog, métricas OpenTelemetry expostas em `/metrics` (Prometheus); traces instrumentados, sem exporter no MVP |
| Testes | xUnit + NSubstitute + FluentAssertions |

O CQRS é manual porque o serviço tem poucos casos de uso e o fluxo explícito (`controller → handler → repositório`) é mais simples de auditar do que um pipeline de mediator.

## Papel no fluxo

A UserAPI é a única emissora de token do ecossistema. DonationAPI e PaymentAPI apenas **validam** o JWT, usando o mesmo issuer, audience e chave de assinatura — não chamam a UserAPI em runtime.

- Papéis emitidos na claim `role`: `Doador` e `GestorONG`.
- O **Owner** é um `GestorONG` com a claim `isOwner=true`. Ele é semeado no startup quando não existe nenhum e não pode ser alterado nem desativado por outro gestor.
- Os eventos da saga de doação (`donation-requested`, `payment-result`) não passam por aqui. O identificador e o e-mail do doador viajam dentro dos próprios eventos, para que a NotificationFunction não precise consultar este serviço.
- O token carrega exatamente quatro claims: `sub`, `email`, `role` e `isOwner`. Não há claim de nome — por isso o campo `DonorName` dos eventos da saga trafega vazio no MVP.

## Endpoints

### Autenticação — `/api/auth` (público)

| Método | Rota | Corpo / resposta |
|--------|------|------------------|
| POST | `/api/auth/register` | `{ personType, document, name, email, password }` → 201 / 400 / 409. Autocadastro de doador. |
| POST | `/api/auth/login` | `{ email, password }` → 200 `{ accessToken, refreshToken, expiresIn }` / 401 |
| POST | `/api/auth/refresh` | `{ refreshToken }` → 200 (novo par de tokens) / 401 |
| POST | `/api/auth/logout` | `{ refreshToken }` → 204. Exige token válido. |

Reuso de um refresh token já rotacionado revoga **toda a cadeia** de tokens do usuário e devolve 401.

### Gestão de usuários — `/api/users` (autenticado)

| Método | Rota | Permissão | Resposta |
|--------|------|-----------|----------|
| POST | `/api/users` | GestorONG | 201 / 400 / 403 / 409 |
| PUT | `/api/users/{id}` | GestorONG | 200 / 403 / 404 |
| PATCH | `/api/users/{id}/role` | GestorONG **com `isOwner`** | 204 / 403 / 404 |
| PATCH | `/api/users/{id}/deactivate` | GestorONG | 204 / 403 / 404 |
| PATCH | `/api/users/{id}/reactivate` | GestorONG | 204 / 403 / 404 |
| GET | `/api/users` | GestorONG | 200 |
| GET | `/api/users/{id}` | GestorONG | 200 / 404 |
| GET | `/api/users/me` | Autenticado | 200 |
| PUT | `/api/users/me` | Autenticado | 200 |
| POST | `/api/users/me/reset-password` | Autenticado | 204 / 400 |

O papel `GestorONG` é exigido no atributo `[Authorize]`; a restrição adicional ao Owner é aplicada no handler e devolve 403 (`User.Forbidden`).

### Operação

| Rota | Descrição |
|------|-----------|
| `GET /health` | Liveness — não consulta dependências |
| `GET /ready` | Readiness — checa o SQL Server; 503 quando o banco está fora |
| `GET /metrics` | Métricas no formato Prometheus |
| `GET /scalar/v1` | Referência interativa da API (OpenAPI em `/swagger/v1/swagger.json`) |

## Como rodar localmente

Pré-requisitos: **.NET 8 SDK** e **Docker** (para o SQL Server).

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# 1) Escolha a senha do SA. Ela precisa atender à política do SQL Server
#    (8+ caracteres, com maiúscula, minúscula, número e símbolo). Não commite este valor.
export SA_PASSWORD='<SENHA_FORTE>'

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=$SA_PASSWORD" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# 2) Configuração mínima
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5001   # porta que o resto do ecossistema espera
export ConnectionStrings__Default="Server=localhost,1433;Database=HackatonFiapUsersDb;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=true;"

# 3) Executar — as migrations são aplicadas no startup
dotnet run --project src/HackatonFiap.Users.API
```

A API responde em `http://localhost:5001` (o perfil `dotnet run` sem `ASPNETCORE_URLS` sobe em `http://localhost:5081`).

Em `Development`, `Jwt:Key` e `Owner:Password` são gerados aleatoriamente e impressos uma vez no log de startup — leia a senha do Owner ali para o primeiro login:

```bash
curl -X POST http://localhost:5001/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"owner@conexaosolidaria.org","password":"<senha impressa no log>"}'
```

Para subir a plataforma inteira (quatro serviços, front, Service Bus emulado e a saga ponta a ponta), use [orchestration/local](https://github.com/GabrielVeridico/hackaton-fiap-orchestration/tree/master/local).

### Docker

```bash
docker build -t hackatonfiap-users:local -f src/HackatonFiap.Users.API/Dockerfile .
```

A imagem expõe a porta **8080**.

### Migrations

O EF Core é uma **local tool** declarada em `.config/dotnet-tools.json`:

```bash
dotnet tool restore
dotnet ef migrations add <Nome> \
  --project src/HackatonFiap.Users.Infrastructure \
  --startup-project src/HackatonFiap.Users.API
```

Não é preciso rodar `database update`: o startup aplica as migrations pendentes.

## Configuração

Todas as chaves usam `__` como separador quando lidas de variável de ambiente.

| Chave | Variável de ambiente | Obrigatória | Observação |
|-------|----------------------|-------------|------------|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | sempre | Connection string do SQL Server |
| `Jwt:Issuer` | `Jwt__Issuer` | não | Padrão `conexaosolidaria.local` |
| `Jwt:Audience` | `Jwt__Audience` | não | Padrão `conexaosolidaria.clients` |
| `Jwt:Key` | `Jwt__Key` | fora de `Development` | Mínimo de 32 bytes. Em `Development` é gerada a cada startup |
| `Owner:Email` | `Owner__Email` | não | Padrão `owner@conexaosolidaria.org` |
| `Owner:Document` | `Owner__Document` | não | CPF do Owner semeado |
| `Owner:Name` | `Owner__Name` | não | Nome do Owner semeado |
| `Owner:Password` | `Owner__Password` | fora de `Development` | Em `Development` é gerada e logada uma vez |
| `ApplicationInsights:ConnectionString` | `ApplicationInsights__ConnectionString` | não | Sink desabilitado quando vazio |

Fora de `Development` a aplicação falha no startup se `Jwt:Key` ou `Owner:Password` estiverem ausentes — não há valor padrão embutido. Nenhum segredo é versionado; no AKS eles chegam do **Azure Key Vault** via CSI Driver + Workload Identity.

## Testes

```bash
dotnet test --nologo
```

São **57 casos** cobrindo o domínio (`User`, `Document`, `Password`, `RefreshToken`), os handlers de comando (autenticação, refresh, logout, autocadastro e gestão de usuários) e as queries. Os handlers são testados contra interfaces mockadas — não há banco em memória.

## CI/CD

`.github/workflows/ci-cd.yml`. A cada push ou pull request na `main`, e sob `workflow_dispatch`:

- **Job `ci`** — `dotnet restore`, `build`, `test` e `docker build`. Roda sempre, sem depender de nenhum segredo.
- **Job `cd`** — condicionado a `vars.DEPLOY_TO_AKS == 'true'`. Sem essa variável o pipeline fecha verde só com a CI.

O deploy faz login federado por **OIDC** (nenhuma senha guardada no repositório), constrói e envia a imagem para o **ACR**, roda o scan **Trivy** com upload do SARIF para o GitHub Security, e então promove a imagem no AKS:

```bash
kubectl set image deployment/hackatonfiap-users \
  hackatonfiap-users=<acr>.azurecr.io/hackatonfiap-users:<sha> -n conexao-solidaria
kubectl rollout status deployment/hackatonfiap-users -n conexao-solidaria --timeout=180s
```

O Deployment em si é criado pelo **Helm** (`orchestration/iac/deploy-apps.ps1`); o CD apenas promove a imagem nova. Os manifestos plain-YAML de referência estão em [orchestration/k8s/](https://github.com/GabrielVeridico/hackaton-fiap-orchestration/tree/master/k8s) e o runbook completo em [orchestration/iac/DEPLOY-AZURE.md](https://github.com/GabrielVeridico/hackaton-fiap-orchestration/blob/master/iac/DEPLOY-AZURE.md).

## Estrutura de pastas

```
src/
├── HackatonFiap.Users.Domain/          # entidades, value objects, Result<T>, enums
├── HackatonFiap.Users.Application/     # comandos, queries, handlers, DTOs, interfaces
├── HackatonFiap.Users.Infrastructure/  # EF Core, BCrypt, JWT, auditoria, OpenTelemetry
└── HackatonFiap.Users.API/             # controllers, middlewares, DI, Program.cs
tests/
└── HackatonFiap.Users.UnitTests/
docs/
└── ARCHITECTURE.md                     # detalhamento das camadas e decisões
```

Fluxo de dependência: `Domain ← Application ← Infrastructure`, com a API apontando para Application e Infrastructure. O detalhamento das camadas, das decisões e dos pontos de integração está em [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
