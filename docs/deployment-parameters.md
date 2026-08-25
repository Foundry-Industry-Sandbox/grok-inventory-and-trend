# Deployment parameters

Parameters for the custom Deploy to Azure template ([`infra/mainTemplate.json`](../infra/mainTemplate.json)), defined in [`infra/main.bicep`](../infra/main.bicep). The portal UI is driven by [`infra/createUiDefinition.json`](../infra/createUiDefinition.json).

## Template parameters

| Parameter | Default | Description |
|---|---|---|
| `baseName` | `grokinventory` | Short prefix used for deployed resource names. |
| `location` | Resource group location | Azure region for all deployed resources. |
| `modelDeploymentName` | `grok-4.3` | Foundry model deployment name used by all prompt agents. |
| `modelDeploymentSkuName` | `GlobalStandard` | SKU for the agent model deployment. Use `GlobalStandard` for serverless; use a provisioned SKU only if available for the model and region. |
| `modelDeploymentCapacity` | `100` | Capacity units for the agent model deployment. Increase when agents fail with `no_capacity` during peak load. |
| `embedDeploymentSkuName` | `GlobalStandard` | SKU for the `text-embedding-3-small` deployment. Switch to `DataZoneStandard` or another regional SKU when `GlobalStandard` is not available in the target region. |
| `embedDeploymentCapacity` | `1000` | Capacity units for the `text-embedding-3-small` deployment used for policy indexing. |
| `agentModelFormat` | `xAI` | Foundry model provider format for the agent reasoning model. |
| `agentModelName` | `grok-4.3` | Grok 4.3 model name in the Foundry catalog. |
| `agentModelVersion` | `1` | Grok 4.3 model version. |
| `searchSku` | `standard` | Azure AI Search SKU for demo retrieval indexes. |
| `apiContainerImage` | `ghcr.io/foundry-industry-sandbox/inventoryplanning-api:demo` | Full container image URI for the API host Container App. |
| `mcpContainerImage` | `ghcr.io/foundry-industry-sandbox/inventoryplanning-mcp:demo` | Full container image URI for the MCP host Container App. |
| `provisioningContainerImage` | `ghcr.io/foundry-industry-sandbox/inventoryplanning-provisioning:demo` | Full container image URI for the agent provisioning Container Apps Job. |
| `frontendContainerImage` | `ghcr.io/foundry-industry-sandbox/inventoryplanning-web:demo` | Full container image URI for the frontend Container App. |
| `enableFabric` | `false` | When `true`, provisions a Fabric lakehouse, seeds lakehouse data, and configures MCP to read case context from Fabric. Requires `fabricWorkspaceName` and `fabricIdentityName`. |
| `fabricWorkspaceName` | `''` | Fabric workspace name. Required when `enableFabric` is `true`. Must be capacity-backed and accessible to the operator. |
| `fabricLakehouseName` | `InventoryPlanningLakehouse` | Fabric lakehouse name. Created at deploy time if missing when Fabric is enabled. |
| `fabricIdentityName` | `fabric-identity` | Pre-provisioned Fabric UAMI name. Required when `enableFabric` is `true`. Must exist in the deployment resource group. Create with `infra/scripts/setup-fabric-provision-identity.ps1`. |
| `fabricRepositoryArchiveUrl` | `https://github.com/foundry-industry-sandbox/grok-inventory-and-trend/archive/refs/heads/main.zip` | Repository archive URL the Fabric seed script downloads to fetch `infra/scripts/` and `dataset-seed/`. Provide via template parameters only (`main.parameters.json` / `fabricRepositoryArchiveUrl`); the seed script has no hardcoded fallback. |

## Deployment outputs

After a successful deployment, use these outputs:

| Output | Use |
|---|---|
| `retailSiteUrl` | Open the demo UI (primary entry point) |
| `frontendUrl` | Blazor frontend base URL |
| `apiUrl` | Backend REST API |
| `mcpUrl` | MCP base URL (used during agent provisioning) |
| `foundryProjectEndpoint` | Microsoft Foundry project endpoint |
| `foundryProjectUrl` | Foundry project in Azure Portal |
| `searchServiceEndpoint` | Azure AI Search endpoint |
| `appInsightsLiveMetricsUrl` | Application Insights live metrics |

See also [`infra/README.md`](../infra/README.md) and [architecture.md](./architecture.md).
