# Azure Pipelines

Three pipelines, one per concern. Each has path-based triggers so they only run
when their source changes.

| Pipeline                    | Trigger paths                                                    | Stages                       |
|-----------------------------|------------------------------------------------------------------|------------------------------|
| `.azure-pipelines/api.yml`  | `src/EquineBooking.{Api,Core,Infrastructure}/**`, `tests/**`     | Build → Deploy               |
| `.azure-pipelines/frontend.yml` | `src/frontend/**`                                            | Build → Deploy               |
| `.azure-pipelines/infra.yml`| `infra/**`                                                       | Validate → WhatIf → Deploy   |

## One-time Azure DevOps setup

These bits live outside YAML — configure them once in the Azure DevOps portal.

### 1. Service connection (workload identity federation)

`Project Settings → Service connections → New → Azure Resource Manager → Workload identity federation (automatic)`

- Name it **`azure-equine`** (matches the `azureServiceConnection` variable in
  every pipeline).
- Scope: the subscription containing `WardRanch`.
- Resource group: `WardRanch` (or grant subscription scope if you'll deploy
  multiple RGs).
- The service connection's auto-created Azure AD app needs:
  - **Contributor** on the resource group, plus
  - **User Access Administrator** on the resource group — required so the
    Bicep deploy can create the Cosmos data-plane and Key Vault role
    assignments.

This replaces stored client secrets with short-lived federated tokens. No
password ever touches the pipeline.

### 2. Environments (the approval gate)

`Pipelines → Environments → New environment` for each of:

- `equine-api-dev`, `equine-api-prod`
- `equine-web-dev`, `equine-web-prod`
- `equine-infra-dev`, `equine-infra-prod`

For prod environments only: `Approvals and checks → + → Approvals` and add
yourself (or a group) as a required approver. This is the equivalent of GitHub
Actions environment protection rules.

### 3. Pipeline variables / secrets

Add these as **secret** pipeline variables (or in a linked Variable Group, or
sourced from Key Vault via the Azure DevOps Library):

| Variable             | Used by                  | Where it comes from                                                     |
|----------------------|--------------------------|-------------------------------------------------------------------------|
| `SWA_DEPLOY_TOKEN`   | `frontend.yml`           | `az staticwebapp secrets list -n <swa> -g WardRanch --query properties.apiKey -o tsv` |
| `VENMO_LINK`         | `infra.yml`              | Whatever URL you want surfaced to clients in approval emails            |
| `REPO_TOKEN`         | `infra.yml`              | GitHub PAT, only if you also want SWA wired to a GitHub repo. Empty otherwise. |

Non-secret values (`functionAppName`, `resourceGroup`, etc.) can be set as
plain pipeline variables or overridden per-environment via Variable Groups.

After running `infra.yml` for the first time, grab the function app name from
the deployment outputs (published as the `infra-outputs-<env>` artifact) and
plug it into the API pipeline's `functionAppName` variable.

### 4. Branch policies (the CODEOWNERS substitute)

`Repos → Branches → main → ⋯ → Branch policies`

- **Require a minimum number of reviewers**: 1
- **Automatically include reviewers** → add yourself as a required reviewer
  with path filters `/src/**`, `/infra/**`, `/.azure-pipelines/**` as needed.

That gives you the same review gate that a GitHub `CODEOWNERS` file would.

### 5. Dependency updates (no native Dependabot)

Azure DevOps doesn't ship a Dependabot equivalent. Two paths:

- **Mend Renovate** (recommended) — install the Renovate extension from the
  Azure DevOps Marketplace, drop a `renovate.json` at the repo root.
- **Scheduled pipeline** — a `cron` trigger that runs `dotnet list package
  --outdated` and `npm outdated`, opening a work item with the diff.

Pick when you're ready; both can be added later without touching the pipelines
above.

## Local validation

The api/frontend pipelines run their build steps with the same commands the
`Makefile` uses, so anything that passes locally with `make test` and
`cd src/frontend && npm test` should pass in CI.

For the infra pipeline, validate locally with:

```bash
az bicep build --file infra/main.bicep --stdout > /dev/null
az bicep build-params --file infra/main.dev.bicepparam --stdout > /dev/null
```
