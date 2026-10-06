# Infrastructure

Bicep templates and scripts to deploy the sample into an Azure subscription. Data generation is not deployed.

## Resources deployed

| Resource | Purpose |
|---|---|
| Microsoft Foundry account and project | Prompt agents and model deployments |
| SpaceXAI Grok 4.7 deployment | All five planning agents |
| text-embedding-3-small deployment | Policy indexing for Foundry IQ |
| Azure AI Search | Policy retrieval index |
| Container Apps Environment | Hosts API, MCP, and frontend |
| Container Apps Jobs | Foundry IQ bootstrap and agent provisioning |
| Azure Container Instance | Runs post-deploy jobs sequentially with managed identity |
| User-assigned managed identities | API, MCP, and provisioning workloads |
| Log Analytics / Application Insights | Telemetry |
| Microsoft Fabric (optional) | Lakehouse for case data when enabled |

## Bicep modules

| Module | File |
|---|---|
| Naming | `modules/naming.bicep` |
| Data services | `modules/data-services.bicep` |
| Platform | `modules/platform.bicep` |
| Foundry | `modules/foundry.bicep` |
| Security / RBAC | `modules/security.bicep` |
| Container Apps | `modules/container-apps.bicep` |
| Private frontend registry (optional) | `modules/frontend-registry.bicep` |
| Container Jobs | `modules/container-jobs.bicep` |
| Fabric (optional) | `modules/fabric-provision.bicep` |
| Post-deploy runner | `modules/post-deploy-scripts.bicep` |

Entry point: [`main.bicep`](main.bicep).

## Parameters

Key parameters in `main.bicep`:

| Parameter | Default | Description |
|---|---|---|
| `baseName` | `grokinventory` | Resource name prefix |
| `location` | Resource group location | Azure region for all deployed resources |
| `modelDeploymentName` | `grok-4.7` | Agent model deployment |
| `embedDeploymentSkuName` | `GlobalStandard` | SKU for text-embedding-3-small |
| `embedDeploymentCapacity` | `1000` | Capacity for text-embedding-3-small |
| `enableFabric` | `false` | Enable Fabric lakehouse integration |
| `apiContainerImage` | `ghcr.io/foundry-industry-sandbox/inventoryplanning-api:demo` | API container image |
| `mcpContainerImage` | `ghcr.io/foundry-industry-sandbox/inventoryplanning-mcp:demo` | MCP container image |
| `frontendContainerImage` | `ghcr.io/foundry-industry-sandbox/inventoryplanning-web:demo` | Frontend container image |
| `frontendRegistryName` | `''` | Optional private ACR in the deployment resource group, with a dedicated managed identity scoped to `AcrPull` |

See [`main.parameters.json`](main.parameters.json) and the Deploy to Azure UI in [`createUiDefinition.json`](createUiDefinition.json). Full parameter reference: [`docs/deployment-parameters.md`](../docs/deployment-parameters.md).

## Outputs

| Output | Use |
|---|---|
| `retailSiteUrl` | Open the demo UI |
| `apiUrl` | Backend REST API |
| `mcpUrl` | MCP base URL for agent tools |
| `foundryProjectEndpoint` | Microsoft Foundry project endpoint |
| `searchServiceEndpoint` | Azure AI Search endpoint |

## Generate ARM template

The repository includes a precompiled ARM template at [`mainTemplate.json`](mainTemplate.json). Regenerate after Bicep changes:

```bash
az bicep build --file infra/main.bicep --outfile infra/mainTemplate.json
```

The Bicep source includes the existing Marketplace attribution deployment so regeneration preserves it. After updating
`mainTemplate.json` or `createUiDefinition.json`, refresh the matching entries in
`partner-center/AzureApplicationPackage-Grok-Retail.zip` before publishing the managed application.

## Deploy to Azure button support

The root README **Deploy to Azure** button targets [`mainTemplate.json`](mainTemplate.json) with [`createUiDefinition.json`](createUiDefinition.json).

After changing Bicep, rebuild `mainTemplate.json` and commit it before the button reflects your changes.

### Frontend image readiness

The frontend liveness probe uses `/health`; readiness requires `/_framework/blazor.web.js`.
A successful server-rendered landing page alone does not prove that Blazor can activate its controls.

The .NET 10 frontend explicitly sets `RequiresAspNetWebAssets=true`. Docker restores the project
before copying its Razor components, so relying on component discovery during restore omits
`Microsoft.AspNetCore.App.Internal.Assets`. A subsequent `publish --no-restore` can succeed without
the Blazor script. The Dockerfile also checks the published script and its asset-manifest route,
failing the image build if either is missing. Rebuild and publish the image after changing the
frontend project; changing environment variables or restarting an old image cannot restore missing files.

Check a deployed image with:

```powershell
.\infra\scripts\test-frontend.ps1 -BaseUrl https://<frontend-fqdn>/
```

The check verifies the script referenced by the landing page, its JavaScript response, and Blazor
SignalR negotiation. Also open the site and confirm **Start workflow** opens the plan workspace.

### Private frontend image (optional)

The public GHCR image remains the default. To use a private frontend image, first deploy the
registry module and build the image from the corrected checkout in the same deployment resource group:

```powershell
az deployment group create --resource-group <resource-group> --name frontend-registry `
  --template-file .\infra\modules\frontend-registry.bicep --parameters registryName=<registry-name>
az acr build --registry <registry-name> --image inventoryplanning-web:<release-tag> `
  --file .\frontend\src\GrokInventoryAndTrend.WebApp\Dockerfile .
az acr repository show --name <registry-name> --image inventoryplanning-web:<release-tag> --query digest -o tsv
```

Then set `frontendRegistryName=<registry-name>` and
`frontendContainerImage=<login-server>/inventoryplanning-web@sha256:<digest>` when deploying `main.bicep`
or the compiled ARM template. Obtain the login server from the registry module output.
The registry is Basic SKU (additional cost), with admin credentials and anonymous pulls disabled.
The template reuses the registry and its dedicated pull identity. Keep both parameter overrides on
subsequent deployments; omitting them selects the public image again. The template does not build or
push private images automatically.

### Post-deploy jobs

The one-shot post-deploy container uses Microsoft Entra managed identity authentication and runs this sequence (typically 15–30 minutes):

1. Container Apps become reachable.
2. Foundry IQ bootstrap job completes.
3. Agent provisioning job completes.
4. Fabric seed (when `enableFabric=true`).

ARM reports the container group as provisioned after the runner starts. Confirm the container group reaches `Succeeded` before using the application; a `Failed` state indicates that bootstrap or provisioning failed.

## Cleanup

```bash
az group delete --name <resource-group-name> --yes --no-wait
```

Additional scripts:

| Script | Purpose |
|---|---|
| `scripts/setup-fabric-provision-identity.ps1` | Create Fabric UAMI and workspace role (required before `enableFabric=true`) |
| `scripts/test-fabric-mcp-read.ps1` | Verify seeded OneLake paths match MCP read convention |

### Fabric integration (optional)

See [docs/fabric-setup.md](../docs/fabric-setup.md) for prerequisites and the full setup flow.

When `enableFabric=true`:

1. Run `scripts/setup-fabric-provision-identity.ps1` against the **same resource group** you will deploy to, then pass the printed `fabricIdentityName` to Bicep (default: `fabric-identity`).
2. Set `fabricWorkspaceName` and `fabricLakehouseName` in deploy parameters.
3. MCP Container App receives `DataSource__Mode=Fabric` and lakehouse settings automatically.
4. After deploy, run `scripts/test-fabric-mcp-read.ps1` and check MCP `GET /health` for `fabricReachable: true`.

See [docs/deployment-parameters.md](../docs/deployment-parameters.md).
