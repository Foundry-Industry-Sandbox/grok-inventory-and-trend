# Dataset seed

## Purpose

Runtime demo dataset for the inventory planning workflow. Cases `case-01` through `case-05` are bundled into the MCP container and used by the API and frontend without running data-generation scripts.

> The generated data is synthetic and is intended only for demonstration purposes.

## Dataset contents

| File / folder | Description | Used by |
|---|---|---|
| `cases/catalog.json` | Case metadata for UI scenario picker | Frontend, API |
| `cases/case-01/` … `case-05/` | Per-case ingest files and normalized JSON | API documents, MCP tools |
| `policies.json` | Structured replenishment policies | Foundry IQ bootstrap |
| `policy_rag.txt` | Policy text source | Bootstrap / reference |

Each case folder contains:

- `README.md` — scenario summary and expected outcome
- `ingest/` — flat POS, inventory, supplier, promotion files (API document endpoints)
- `fabric-pre-requisite-data/` — normalized JSON entities (MCP signal tools)

## File formats

| Format | Location | Content |
|---|---|---|
| JSON | `fabric-pre-requisite-data/` | Normalized POS batches, inventory snapshots, supplier profiles, promotions |
| CSV / TXT | `ingest/` | Raw exports exposed via document API |
| JSON | `catalog.json`, `policies.json` | Metadata and policy definitions |

## Schema

Normalized entities follow naming patterns such as:

- `pos_transaction_batch/POS-{SKU}-{STORE}-{DATE}.json`
- `inventory_snapshot/INV-{SKU}-{STORE}-{DATE}.json`
- `supplier_profile/SUP-{ID}.json`
- `promotion_event/PROMO-{ID}.json`

See [docs/data-model.md](../docs/data-model.md) and per-case README files.

## Data relationships

- Each case maps to one legacy scenario ID (`IPF-001` … `IPF-005`).
- Signal data is scoped to one primary SKU and store per case.
- Policies in `policies.json` ground planner-copilot constraint checks via Foundry IQ.

## How the backend uses this data

- **API** — reads `catalog.json` and serves `ingest/` documents.
- **MCP** — reads `fabric-pre-requisite-data/` for tool responses; indexes `policies.json` at deploy time.
- **Fabric mode** — when enabled, equivalent data is uploaded to the lakehouse; MCP reads from Fabric instead of local files.

## Regenerating the dataset

See [`../data-generation/README.md`](../data-generation/README.md). Regeneration updates `dataset-seed/` but requires rebuilding container images and redeploying.

## Limitations

- Only five cases are wired in the API allow-list.
- One SKU per scenario; multi-SKU allocation is not supported.
- RAG knowledge file paths are configured in MCP (`promotions-price-rag/`, `signal-quality-rag/`, etc.) — verify these folders exist if local RAG tools are required.

## Demo cases

| Case | Title | Expected outcome |
|---|---|---|
| `case-01` | Seasonal happy path | Clean holiday forecast; order approved |
| `case-02` | Promotion → budget review | Order within budget; planner budget HITL |
| `case-03` | Supplier delay → expedite | Expedite required; planner service-level HITL |
| `case-04` | Partial fill → reorder | Reorder approved (MOQ) |
| `case-05` | Demand anomaly → no action | Anomaly flagged; no supply order; forecasting HITL |
