# Demo script

## Demo goal

Show how five specialized agents plus human oversight turn retail signals into an explainable replenishment recommendation on Microsoft Foundry.

## Setup

1. Complete [Quick deploy](../README.md#quick-deploy) or use a pre-provisioned environment.
2. Open `retailSiteUrl` from deployment outputs.
3. Confirm post-deploy jobs finished (Foundry IQ bootstrap and agent provisioning).

## Step-by-step flow

1. **Open the frontend** — show the case picker and highlight synthetic data disclaimer.
2. **Select `case-01`** — seasonal happy path for a clean first run.
3. **Start the workflow** — explain signal ingestion, feature analysis, forecasting, replenishment, and planner copilot stages.
4. **Walk agent steps** — point to structured outputs: `summary`, `decision`, `evidence`.
5. **Human approval** — approve or reject; explain this is a client-side demo gate, not a persisted backend action.
6. **Optional contrast** — repeat with `case-02` (budget HITL) or `case-05` (demand anomaly) if time allows.

## Expected outputs

| Case | Story | Expected outcome |
|---|---|---|
| `case-01` | Seasonal happy path | Approved order (~208 units), no HITL gate |
| `case-02` | Promotion spike | Order within budget, budget review HITL |
| `case-05` | Demand anomaly | Anomaly flagged, no supply order |

## Resetting the demo

- Start a new workflow for the same or a different case (new `executionId`).
- Restart the API to clear in-memory execution state.
- Delete the resource group when finished to stop Azure charges.

See also [troubleshooting.md](./troubleshooting.md).
