# Agent provisioning

## Purpose

Creates or updates the five Microsoft Foundry prompt agents required by the inventory planning workflow. In a standard Azure deployment, provisioning runs automatically as a Container Apps Job after infrastructure and MCP wiring complete.

## Prompt agents included

| Agent | Model | Tools | Memory | Purpose |
|---|---|---|---|---|
| `signal-ingestion-agent` | Grok 4.3 (`AZURE_AI_MODEL_DEPLOYMENT_NAME`) | MCP: `/signal-ingestion/mcp` | Workflow context | Ingest POS, inventory, supplier, and promotion signals; validate data quality |
| `feature-and-causality-agent` | Grok 4.3 | MCP: `/feature-and-causality/mcp` | Workflow context | Build predictors; measure demand drivers |
| `forecasting-agent` | Grok 4.3 | MCP: `/forecasting/mcp` | Workflow context | Short-term demand forecast; detect anomalies |
| `replenishment-and-allocation-agent` | Grok 4.3 | MCP: `/replenishment-and-allocation/mcp` | Workflow context | Recommend stock targets and draft PO/TO orders |
| `planner-copilot-agent` | Grok 4.3 | MCP: `/planner-copilot/mcp` | Workflow context | Enforce budget and service-level constraints for human approval |

The **Planning orchestrator** runs in the API via Agent Framework and is not provisioned by this project.

## Required environment variables

| Variable | Required | Description |
|---|---:|---|
| `AZURE_FOUNDRY_PROJECT_ENDPOINT` | Yes | Foundry project API endpoint |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Yes | Model deployment name (default deploy: `grok-4.3`) |
| `MCP_BASE_URL` | Yes | Public HTTPS base URL of the MCP Container App |

## Provisioning flow

During **Deploy to Azure**, [`infra/main.bicep`](../infra/main.bicep):

1. Provisions Foundry, Search, and model deployments.
2. Starts the Foundry IQ bootstrap job (indexes policies from `dataset-seed/policies.json`).
3. Starts the agent provisioning Container Apps Job.
4. Waits for both jobs before completing the deployment.

## How to create agents

For local or maintenance runs after MCP is reachable:

```powershell
$env:AZURE_FOUNDRY_PROJECT_ENDPOINT = "https://..."
$env:AZURE_AI_MODEL_DEPLOYMENT_NAME = "grok-4.3"
$env:MCP_BASE_URL = "https://..."

dotnet run --project agent-provisioning/src/GrokInventoryAndTrend.AgentProvisioning
```

Or use `./agent-provisioning/scripts/provision-agents.ps1`.

## How to update agents

1. Edit agent instructions or schemas under `src/GrokInventoryAndTrend.AgentProvisioning/agents/` or `shared/`.
2. Rebuild the provisioning container image (or run the CLI locally).
3. Redeploy or re-run provisioning. The CLI compares definition fingerprints and creates a new version only when the definition changed.

Agent-as-code layout per agent:

```text
agents/<agent-name>/
  agent.json
  instructions.md
  mcp.json
```

## How to validate provisioning

1. Open the Foundry project URL from deployment outputs (`foundryProjectUrl`).
2. Confirm all five agents exist with MCP tool bindings pointing to `{mcpUrl}/<agent-path>/mcp`.
3. Start a demo workflow from the frontend and verify each agent step completes.

## Troubleshooting

| Symptom | Possible cause | Resolution |
|---|---|---|
| Provisioning job failed | Missing model deployment or MCP URL | Check Container Apps job logs; verify `modelDeploymentName` and `mcpUrl` outputs |
| Agent version unchanged after edit | Fingerprint match | Confirm assets were copied into the published image; force rebuild |
| MCP tool errors at runtime | Wrong `MCP_BASE_URL` at provision time | Re-run provisioning with the correct public MCP URL |

See also [`docs/agent-foundry-governance.md`](../docs/agent-foundry-governance.md) for governance posture.
