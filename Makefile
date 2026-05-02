# Convenience commands. Override variables on the CLI, e.g.
#   make deploy-infra ENV=prod RG=WardRanch REPO_TOKEN=$GH_PAT

ENV         ?= dev
RG          ?= WardRanch
LOCATION    ?= westus2
API_DIR     := src/EquineBooking.Api
WEB_DIR     := src/frontend
INFRA_DIR   := infra
API_URL     ?= http://localhost:7071
REPO_TOKEN  ?=

# Function app name follows the same naming convention as Bicep:
# func-equine-<env>-<hash>. Pass FUNC_APP=... to override.
FUNC_APP    ?=

.DEFAULT_GOAL := help

.PHONY: help
help:
	@echo "Targets:"
	@echo "  setup           install backend + frontend dependencies"
	@echo "  dev             start Cosmos+Azurite, then API and web"
	@echo "  dev-api         start the Functions API only (requires docker compose up)"
	@echo "  dev-web         start the Angular dev server only"
	@echo "  test            run all dotnet tests"
	@echo "  test-web        run frontend tests"
	@echo "  seed            POST /admin/seed against the local API"
	@echo "  deploy-infra    az deployment group create against ENV=$(ENV) RG=$(RG)"
	@echo "  deploy-api      zip-deploy the Functions API to FUNC_APP=$(FUNC_APP)"
	@echo "  clean           remove build artefacts"

.PHONY: setup
setup:
	dotnet restore
	cd $(WEB_DIR) && npm ci
	@if [ ! -f $(API_DIR)/local.settings.json ]; then \
	  cp $(API_DIR)/local.settings.template.json $(API_DIR)/local.settings.json; \
	  echo "Created $(API_DIR)/local.settings.json from template — fill in B2C values."; \
	fi

.PHONY: dev
dev:
	docker compose up -d
	@echo "Cosmos + Azurite are up. Starting API and web in parallel..."
	@$(MAKE) -j 2 dev-api dev-web

.PHONY: dev-api
dev-api:
	cd $(API_DIR) && func start

.PHONY: dev-web
dev-web:
	cd $(WEB_DIR) && npm start

.PHONY: test
test:
	dotnet test

.PHONY: test-web
test-web:
	cd $(WEB_DIR) && npm test -- --watch=false --browsers=ChromeHeadless

.PHONY: seed
seed:
	curl -fsS -X POST $(API_URL)/api/seed | tee /dev/stderr

.PHONY: deploy-infra
deploy-infra:
	@if [ -z "$(REPO_TOKEN)" ]; then \
	  echo "REPO_TOKEN is empty — Static Web App will deploy without GitHub source-control wiring."; \
	fi
	az group create -n $(RG) -l $(LOCATION) -o none
	az deployment group create \
	  --resource-group $(RG) \
	  --template-file $(INFRA_DIR)/main.bicep \
	  --parameters $(INFRA_DIR)/main.$(ENV).bicepparam \
	  --parameters repositoryToken="$(REPO_TOKEN)"

.PHONY: deploy-api
deploy-api:
	@if [ -z "$(FUNC_APP)" ]; then \
	  echo "Set FUNC_APP=<function-app-name>. Find it in 'az deployment group show ... --query properties.outputs.functionAppUrl'."; \
	  exit 1; \
	fi
	dotnet publish $(API_DIR) -c Release -o $(API_DIR)/publish
	cd $(API_DIR)/publish && zip -r ../publish.zip . > /dev/null
	az functionapp deployment source config-zip \
	  --resource-group $(RG) \
	  --name $(FUNC_APP) \
	  --src $(API_DIR)/publish.zip
	rm -f $(API_DIR)/publish.zip
	rm -rf $(API_DIR)/publish

.PHONY: clean
clean:
	dotnet clean -v quiet
	rm -rf $(WEB_DIR)/dist $(WEB_DIR)/.angular
	rm -rf $(API_DIR)/bin $(API_DIR)/obj $(API_DIR)/publish $(API_DIR)/publish.zip
