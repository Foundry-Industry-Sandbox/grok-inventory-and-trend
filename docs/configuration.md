# Configuration

## Environment variables

### API (`GrokInventoryAndTrend.Api`)

| Setting | Required | Description | Example |
|---|:---:|---|---|
| `AZURE_FOUNDRY_PROJECT_ENDPOINT` | Yes | Foundry project endpoint | `https://{account}.services.ai.azure.com/api/projects/{project}` |
| `Dataset__RootPath` | Yes (local) | Path to dataset-seed | `../../dataset-seed` |
| `Dataset__CasesRelativePath` | No | Cases subfolder | `cases` |
| `Dataset__IngestSubfolder` | No | Ingest folder name | `ingest` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | No | App Insights telemetry | Set automatically in Azure |

### MCP (`GrokInventoryAndTrend.Mcp`)

| Setting | Required | Description | Example |
|---|:---:|---|---|
| `FoundryIq__SearchEndpoint` | Yes (Azure) | AI Search HTTPS endpoint | `https://{search}.search.windows.net` |
| `Dataset__RootPath` | Yes | Dataset root | `/app/dataset-seed` |
| `DataSource__Mode` | No | `Local` or `Fabric` mode | `Local` |
| `DataSource__FabricLakehouse__WorkspaceName` | Yes (Fabric) | Fabric workspace display name | Set by Bicep when `enableFabric=true` |
| `DataSource__FabricLakehouse__LakehouseName` | Yes (Fabric) | Lakehouse display name | `InventoryPlanningLakehouse` |
| `DataSource__FabricLakehouse__EvidenceRoot` | No | OneLake bronze root for signal JSON | `Files/bronze` |
| `DataSource__FabricLakehouse__TimeoutSeconds` | No | OneLake request timeout (seconds) | `30` |
| `AzureFoundryModels__RerankEndpoint` | No | Cohere rerank URL | Foundry account `/providers/cohere/v2/rerank` |
| `AZURE_CLIENT_ID` | Yes (Fabric/Azure) | Managed identity client ID for OneLake and Azure resources | Set by Bicep |

#### Fabric mode (local development)

1. Copy [`appsettings.Fabric.local.example.json`](../backend/GrokInventoryAndTrend.Mcp/appsettings.Fabric.local.example.json) to `appsettings.Fabric.local.json` (gitignored).
2. Set workspace and lakehouse display names from your Fabric portal.
3. Authenticate with Azure (`az login`) and ensure your identity or UAMI has OneLake read access.
4. Run with `dotnet run --environment Development` and load the local file via `appsettings.Deployment.local.json` or environment variables.

Post-seed verification script: [`infra/scripts/test-fabric-mcp-read.ps1`](../infra/scripts/test-fabric-mcp-read.ps1).

### Agent provisioning

| Setting | Required | Description | Example |
|---|:---:|---|---|
| `AZURE_FOUNDRY_PROJECT_ENDPOINT` | Yes | Foundry project endpoint | See API |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Yes | Agent model deployment | `grok-4.7` |
| `MCP_BASE_URL` | Yes | Public MCP base URL | `https://{mcp-app}.azurecontainerapps.io` |

### Frontend

| Setting | Required | Description | Example |
|---|:---:|---|---|
| `PlanningApi__BaseUrl` | Yes | Backend API URL | `http://localhost:5038/` |

## Azure resources

Created by [infra/main.bicep](../infra/main.bicep). Key outputs: `retailSiteUrl`, `apiUrl`, `mcpUrl`, `foundryProjectEndpoint`.

## Model configuration

| Parameter | Default | Notes |
|---|---|---|
| `modelDeploymentName` | `grok-4.7` | All five agents |
| `agentModelFormat` | `xAI` | Provider format |
| Embedding deployment | `text-embedding-3-small` | Fixed in foundry.bicep |

Cohere rerank is supported in MCP code but is not deployed by default Bicep. Configure manually if signal reranking is required.

## Agent configuration

Agent instructions and output schemas live under [agent-provisioning/src/GrokInventoryAndTrend.AgentProvisioning/agents/](../agent-provisioning/src/GrokInventoryAndTrend.AgentProvisioning/agents/). Provisioned automatically at deploy time.

## Backend configuration

Local: copy [`.env.local.example`](../backend/GrokInventoryAndTrend.Api/.env.local.example). Azure: environment variables injected by [container-apps.bicep](../infra/modules/container-apps.bicep).

## Frontend configuration

[appsettings.json](../frontend/src/GrokInventoryAndTrend.WebApp/appsettings.json) for local defaults. Azure overrides `PlanningApi__BaseUrl` via Bicep.
