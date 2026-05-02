# Equine Booking

Booking system for a small equine facility — clients reserve corrals (hourly) and stalls (daily); admins approve, decline, and manage spaces and users.

## Stack

| Layer       | Tech                                                                |
|-------------|---------------------------------------------------------------------|
| Frontend    | Angular 19, standalone components, MSAL Angular, Angular Material   |
| API         | Azure Functions (.NET 8 isolated), Microsoft.Identity.Web for B2C   |
| Data        | Azure Cosmos DB (serverless)                                        |
| Messaging   | Azure Service Bus                                                   |
| Email       | SendGrid                                                            |
| Auth        | Azure AD B2C                                                        |
| Infra       | Bicep (`infra/`)                                                    |
| Hosting     | Azure Functions (consumption, Linux) + Azure Static Web Apps (Free) |

## Repository layout

```
src/
  EquineBooking.Core/            domain models, DTOs, repository interfaces
  EquineBooking.Infrastructure/  Cosmos repositories, SendGrid notifier, Service Bus publisher
  EquineBooking.Api/             Azure Functions HTTP API
  frontend/                      Angular SPA
infra/                           Bicep (main + modules + parameter files)
tests/                           xUnit test projects
docker-compose.yml               local Cosmos + Azurite
Makefile                         common commands
```

## Prerequisites

| Tool                          | Version | Notes                                                       |
|-------------------------------|---------|-------------------------------------------------------------|
| .NET SDK                      | 8.0     | Required for the API and tests                              |
| Node.js                       | 20+     | Required for the Angular frontend                           |
| Angular CLI                   | 19      | `npm i -g @angular/cli@19`                                  |
| Azure Functions Core Tools    | v4      | `npm i -g azure-functions-core-tools@4 --unsafe-perm true`  |
| Docker (or Docker Desktop)    | latest  | Runs the local Cosmos emulator and Azurite                  |
| Azure CLI                     | 2.60+   | Bicep deploys (`az bicep build` is bundled)                 |
| make                          | any     | Optional convenience wrapper for the workflows below        |

## First-time setup

```bash
# 1. Clone and install dependencies for both halves
git clone <this repo>
cd equine-booking
make setup        # equivalent to: dotnet restore  &&  cd src/frontend && npm ci

# 2. Copy the Functions config template and fill in your B2C tenant values
cp src/EquineBooking.Api/local.settings.template.json \
   src/EquineBooking.Api/local.settings.json

# 3. Bring up Cosmos + Azurite
docker compose up -d

# 4. Wait for the Cosmos emulator to come up (it self-signs a TLS cert on first run;
#    initial start can take ~30s). The healthcheck in docker-compose.yml will report
#    "healthy" once it's ready:
docker compose ps
```

The Cosmos emulator self-signs a TLS cert. The first time the API hits it locally you may
need to trust the cert — `https://localhost:8081/_explorer/emulator.pem` exposes it.

## Running locally

In separate terminals:

```bash
# API
make dev-api          # cd src/EquineBooking.Api && func start

# Frontend
make dev-web          # cd src/frontend && npm start
```

Or both via docker + parallel processes:

```bash
make dev              # docker compose up -d, then API and web in the foreground
```

URLs:

| Surface          | URL                              |
|------------------|----------------------------------|
| API              | http://localhost:7071/api        |
| Frontend         | http://localhost:4200            |
| Cosmos Explorer  | https://localhost:8081/_explorer |

### Seed reference data (first run)

Once the API is up, populate the local database with two spaces and two dev users:

```bash
make seed
# or:
curl -X POST http://localhost:7071/api/admin/seed
```

The endpoint is open in `Development` and admin-only in every other environment. It is
idempotent — re-running it returns the per-resource `created`/`skipped` counts.

### Local email

`INotificationService` is replaced by `LocalDevNotificationService` when
`DOTNET_ENVIRONMENT=Development`. It logs the intended recipient and payload to the
Functions console (and Application Insights, if wired) instead of sending. No SendGrid
key needed.

## Tests

```bash
make test             # dotnet test
```

Frontend tests run with Karma:

```bash
cd src/frontend && npm test
```

## Deploy

Infrastructure is in `infra/`. Two parameter files: `main.dev.bicepparam` and `main.prod.bicepparam`.

```bash
# Resource group (one-time)
az group create -n WardRanch -l westus2

# Infrastructure
make deploy-infra ENV=dev RG=WardRanch REPO_TOKEN=$GH_PAT
# expands to:
#   az deployment group create \
#     --resource-group WardRanch \
#     --template-file infra/main.bicep \
#     --parameters infra/main.dev.bicepparam \
#     --parameters repositoryToken=$GH_PAT

# API (Functions zip deploy)
make deploy-api ENV=dev RG=WardRanch
```

The Static Web App is wired to GitHub via the Bicep template; pushing to `main` triggers
the SWA's GitHub Actions workflow that builds and publishes the Angular bundle.

## Architecture

A full architectural write-up lives at [`docs/architecture.md`](docs/architecture.md) — if
that file isn't present yet, the high-level summary is:

- Clients authenticate against B2C and receive a JWT.
- The Angular SPA calls the Functions API with the JWT in `Authorization`.
- `AuthMiddleware` validates the token and populates a `UserContext`.
- `AuthorizationHelper` reads the user's persisted role (Admin vs Client) so authorisation can be revoked without re-issuing tokens.
- Repositories partition by `spaceId` (bookings) or `id` (spaces, users) for predictable RU usage.
- Booking lifecycle events (created/approved/declined/cancelled) flow through `INotificationService` → SendGrid in production, console logger in dev.

## Azure portal links

After running `make deploy-infra`, the resources land in the resource group with these
naming patterns (`<resource>-equine-<env>-<6-char-hash>`):

| Resource             | Portal blade                                                               |
|----------------------|----------------------------------------------------------------------------|
| Function app         | `func-equine-<env>-<hash>`  → App Service blade                            |
| Static Web App       | `swa-equine-<env>-<hash>`   → Static Web Apps blade                        |
| Cosmos DB            | `cosmos-equine-<env>-<hash>`→ Azure Cosmos DB blade                        |
| Key Vault            | `kv-equine-<env>-<hash>`    → Key Vaults blade                             |
| App Insights         | `ai-equine-<env>`           → Application Insights blade                   |
| Log Analytics        | `log-equine-<env>`          → Log Analytics workspaces blade               |
| Storage (Functions)  | `stequine<env><hash>`       → Storage accounts blade                       |

`make deploy-infra` prints `functionAppUrl`, `staticWebAppUrl`, and `keyVaultName` as
deployment outputs — capture those into your environment for `make deploy-api` and any
manual SWA configuration.
