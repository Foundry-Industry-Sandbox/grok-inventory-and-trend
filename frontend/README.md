# Frontend

## Purpose

Blazor Server web app for running the inventory planning workflow, inspecting agent outputs, and performing client-side human approval.

Project: `frontend/src/GrokInventoryAndTrend.WebApp`

## User experience

1. **Home** — pick a demo case from `dataset-seed/cases/catalog.json`.
2. **Plan workspace** — start the workflow, poll agent steps, and view structured outputs.
3. **Planner review** — approve or reject the final recommendation (client-side only; not persisted to the API).

## Configuration

Settings in `appsettings.json`:

| Setting | Default | Description |
|---|---|---|
| `PlanningApi:BaseUrl` | `http://localhost:5038/` | Backend API base URL |
| `DatasetSeed:RootPath` | `../../../dataset-seed` | Path to case catalog |
| `WorkflowPolling:IntervalSeconds` | `2` | Status poll interval |
| `WorkflowPolling:MaxDurationMinutes` | `25` | Max poll duration |

## Local run

```powershell
cd frontend/src/GrokInventoryAndTrend.WebApp
dotnet run --launch-profile http
```

Open `http://localhost:5147`.

**VS Code / Cursor:** use the **WebApp** launch configuration (start **API + MCP** first).

## Build

```powershell
cd frontend/src/GrokInventoryAndTrend.WebApp
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

Container image: `ghcr.io/foundry-industry-sandbox/inventoryplanning-web:demo` (see [infra/README.md](../infra/README.md)).

## Environment variables

| Variable | Description |
|---|---|
| `PlanningApi__BaseUrl` | Override API URL (e.g. deployed `apiUrl`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |

## Connecting to backend

The UI calls the planning API via `PlanningApiClient`. In Azure, the Container App receives `PlanningApi__BaseUrl` from Bicep. Locally, ensure the API is running on the URL in `appsettings.json`.

Integration details: [BACKEND_INTEGRATION.md](./src/GrokInventoryAndTrend.WebApp/BACKEND_INTEGRATION.md).

## Troubleshooting

| Symptom | Possible cause | Resolution |
|---|---|---|
| Cases not loading | Missing `catalog.json` | Verify `DatasetSeed:RootPath` |
| Workflow stuck on Running | API or Microsoft Foundry issue | Check API logs and poll `/executions/{id}/basic/status` |
| Connection refused | API not running | Start API on port `5038` or update `PlanningApi__BaseUrl` |
