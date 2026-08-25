# Data model

## Entities

| Entity | Description | Storage |
|---|---|---|
| Case | Demo scenario (`case-01` … `case-05`) | `cases/catalog.json`, per-case folders |
| POS transaction batch | Weekly sales for a SKU/store | `fabric-pre-requisite-data/pos_transaction_batch/` |
| Inventory snapshot | On-hand stock by SKU/store/date | `fabric-pre-requisite-data/inventory_snapshot/` |
| Supplier profile | Lead time, MOQ, capacity | `fabric-pre-requisite-data/supplier_profile/` |
| Promotion event | Campaign windows and uplift | `fabric-pre-requisite-data/promotion_event/` |
| Policy | Replenishment and budget rules | `policies.json` |

## Relationships

- Each case targets one primary SKU and store.
- Supplier profiles link shipments and replenishment constraints.
- Policies reference thresholds used by planner-copilot tools (`BG-300`, `RP-200`, etc.).

## File schemas

Normalized JSON files are generated from `data-generation/scripts/` and follow entity-type folder conventions. Raw ingest files (CSV, TXT) mirror retail export formats for document preview in the UI.

Policy schema (`policies.json`):

```json
{
  "policies": [
    {
      "policyRef": "BG-300",
      "rule": "...",
      "threshold": "...",
      "action": "...",
      "exception": "...",
      "content": "..."
    }
  ]
}
```

## Example records

Case catalog entry:

```json
{
  "caseId": "case-01",
  "title": "Seasonal happy path",
  "description": "Plan seasonal replenishment for Christmas week...",
  "outcomeTag": "HealthyRun",
  "legacyId": "IPF-001",
  "context": {
    "category": "Household — Gift Wrap Assortment",
    "budgetCap": 250000,
    "targetFillRate": 0.95
  }
}
```

## How agents use the data

| Agent | Data access |
|---|---|
| Signal ingestion | MCP tools read case JSON; document API serves ingest files |
| Feature and causality | Driver context and promotions via MCP |
| Forecasting | Trend patterns and signal evidence via MCP |
| Replenishment and allocation | Replenishment signals and recommendations via MCP |
| Planner copilot | Policies and planning constraints via Foundry IQ and MCP |

Only signal ingestion reads case-scoped operational data directly in the default demo path; downstream agents rely on workflow memory plus MCP context tools.

Ground truth for validation: `data-generation/ground-truth/IPF-*.json`.
