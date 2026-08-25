# Backend

## Purpose

Hosts the planning orchestrator API, the five-agent Agent Framework workflow, and the MCP tool server used by Microsoft Foundry prompt agents.

Projects:

| Project | Role |
|---|---|
| `GrokInventoryAndTrend.Api` | REST API and workflow orchestration |
| `GrokInventoryAndTrend.Mcp` | MCP endpoints and retrieval tools |

## Architecture

```
Frontend → API (orchestrator) → Microsoft Foundry prompt agents → MCP tools → dataset-seed / Foundry IQ
```

The API coordinates agents sequentially and stores execution state in memory. Foundry agents call the public MCP Container App over HTTPS.

## Main workflow

1. Client starts a workflow for a demo case (`case-01` … `case-05`).
2. API invokes agents in order: signal ingestion → feature and causality → forecasting → replenishment and allocation → planner copilot.
3. Each agent may call MCP tools scoped by `caseId` and `executionId`.
4. Client polls status until `Completed` or `Failed`.

## Agent Framework orchestration

Implemented in `GrokInventoryAndTrend.Api` using `Microsoft.Agents.AI.Foundry` and `Microsoft.Agents.AI.Workflows`. The orchestrator passes accumulated context between agents via workflow memory.

## MCP servers

Five MCP endpoints on the MCP host (default port `5040` locally):

| MCP endpoint | Agent |
|---|---|
| `/signal-ingestion/mcp` | Signal ingestion |
| `/feature-and-causality/mcp` | Feature and causality |
| `/forecasting/mcp` | Forecasting |
| `/replenishment-and-allocation/mcp` | Replenishment and allocation |
| `/planner-copilot/mcp` | Planner copilot |

Tool details: [GrokInventoryAndTrend.Mcp/README.md](./GrokInventoryAndTrend.Mcp/README.md).

## REST API

Base route: `/api/inventory-planning`

## API endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/health` | Health probe |
| `GET` | `/api/inventory-planning/cases` | List demo cases from `catalog.json` |
| `POST` | `/api/inventory-planning/cases/{caseId}/workflow/basic/start` | Start the agentic workflow |
| `GET` | `/api/inventory-planning/executions/{executionId}/basic/status` | Get workflow status and agent outputs |
| `GET` | `/api/inventory-planning/cases/{caseId}/documents` | List ingest documents for a case |
| `GET` | `/api/inventory-planning/cases/{caseId}/documents/content?documentPath=...` | Download a case document |

Supported cases: `case-01` … `case-05`.

## Configuration

Copy [`GrokInventoryAndTrend.Api/.env.local.example`](./GrokInventoryAndTrend.Api/.env.local.example) to `.env.local`:

| Setting | Description |
|---|---|
| `AZURE_FOUNDRY_PROJECT_ENDPOINT` | Foundry project endpoint |
| `Dataset__RootPath` | Path to `dataset-seed` |
| `Dataset__CasesRelativePath` | Cases subfolder (default: `cases`) |

MCP configuration: see [`GrokInventoryAndTrend.Mcp/appsettings.json`](./GrokInventoryAndTrend.Mcp/appsettings.json) and [docs/configuration.md](../docs/configuration.md).

## Local run

**VS Code / Cursor (recommended):**

1. Copy `.env.local.example` to `.env.local` and fill in Foundry values.
2. Run **API + MCP** compound launch configuration.
3. Optionally start **WebApp** for the UI.

**Command line:**

```powershell
# API
cd backend/GrokInventoryAndTrend.Api
dotnet run --launch-profile http
# http://localhost:5038

# MCP (separate terminal)
cd backend/GrokInventoryAndTrend.Mcp
dotnet run
# http://localhost:5040
```

Requires `az login` or another credential for `DefaultAzureCredential`, and the five prompt agents already provisioned in your Foundry project.

## Tests

No automated test project is included in this sample. Validate manually by starting a workflow for `case-01` and polling status until completion.

## Observability

When `APPLICATIONINSIGHTS_CONNECTION_STRING` is set, the API emits telemetry via OpenTelemetry and Azure Monitor. Use deployment output `appInsightsLiveMetricsUrl` in Azure.

## Troubleshooting

| Symptom | Possible cause | Resolution |
|---|---|---|
| `503` on workflow start | Foundry endpoint or agents missing | Verify `.env.local` and agent provisioning |
| Case not found | Unsupported `caseId` | Use `case-01` … `case-05` |
| MCP tool failures | MCP not running or wrong URL | Start MCP; confirm agents were provisioned with correct `MCP_BASE_URL` |
| Execution not found after restart | In-memory store | Restart clears executions; start a new workflow |

See [docs/troubleshooting.md](../docs/troubleshooting.md).
