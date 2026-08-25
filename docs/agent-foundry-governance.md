# Agent Foundry Governance

This document describes how the inventory planning sample applies the [Agent Governance Toolkit (AGT)](https://microsoft.github.io/agent-governance-toolkit/) to the five Azure AI Foundry prompt agents.

## Three governance acts

1. **Tool sandboxing** - per-agent `governance.yaml` policies (`apiVersion: governance.toolkit/v1`) evaluated before tool execution.
2. **Rogue detection** - per-agent `rogue.yaml` sliding-window detection blocks repeated calls to a configured risky tool.
3. **Audit** - governance events append to a hash-chain JSONL store under `data/agent-governance-audit`.

## Enforcement

Remote Foundry agents call MCP tools over HTTP. Governance is enforced on the **MCP server** before each tool handler runs via `GovernedMcpServerTool` and `McpToolGovernanceCoordinator` in `GrokInventoryAndTrend.Mcp`.

## Co-located policy layout

Policies live beside each agent definition:

```text
agent-provisioning/agents/
  signal-ingestion-agent/
    agent.json
    instructions.md
    mcp.json
    governance.yaml
    rogue.yaml
  feature-and-causality-agent/
    ...
  forecasting-agent/
    ...
  replenishment-and-allocation-agent/
    ...
  planner-copilot-agent/
    ...
```

At runtime, `GrokInventoryAndTrend.Governance` copies these files to `policies/{agent-name}/` in the MCP server output directory.

## MCP-layer governance

1. **`McpAgentRoleMiddleware`** - resolves the caller agent from the MCP route.
2. **`GovernedMcpServerTool`** - wraps each MCP tool and delegates to `McpToolGovernanceCoordinator`.
3. **`McpToolGovernanceCoordinator`** - evaluates `governance.yaml`, applies rogue detection keyed by role + `caseId` + `executionId`, writes audit records, and logs blocked calls at warning level.

Blocked calls return an MCP error result; denied tools never reach the handler.

## Tool deny matrix

| Tool | signal | feature | forecast | repl | planner |
|------|:------:|:-------:|:--------:|:----:|:-------:|
| `get_planning_signals` | A | D | D | D | D |
| `search_signal_evidence` | A | A | A | D | D |
| `get_signal_quality_rules` | A | D | D | D | D |
| `get_planning_profile` | D | A | D | D | D |
| `get_driver_context` | D | A | D | D | D |
| `get_relevant_promotions` | D | A | A | D | D |
| `get_trend_patterns` | D | D | A | D | D |
| `get_forecasting_context` | D | D | A | D | D |
| `get_replenishment_signals` | D | D | D | A | D |
| `build_replenishment_recommendations` | D | D | D | A | D |
| `get_planning_constraints` | D | D | D | D | A |
| `get_relevant_policies` | D | D | D | D | A |
| `get_policies_by_refs` | D | D | D | D | A |

**D** = explicit deny rule in `governance.yaml`; **A** = allowed via `default_action: allow`.

## Rogue risky tools

| Agent | `riskyTool` | Rationale |
|-------|-------------|-----------|
| signal-ingestion | `build_replenishment_recommendations` | Cross-stage escalation to replenishment |
| feature-and-causality | `build_replenishment_recommendations` | Same |
| forecasting | `build_replenishment_recommendations` | Same |
| replenishment-and-allocation | `get_planning_constraints` | Planner-policy bypass |
| planner-copilot | `build_replenishment_recommendations` | Cross-stage execution escalation |

Defaults: `windowSize: 10`, `triggerCount: 5`.

## Configuration

`backend/GrokInventoryAndTrend.Mcp/appsettings.json`:

```json
"Governance": {
  "EnableMcpToolGovernance": true,
  "AgentAuditStoreDirectory": "data/agent-governance-audit"
}
```

Set `EnableMcpToolGovernance: false` to disable policy enforcement on the MCP server.

Mount a persistent volume for `AgentAuditStoreDirectory` in production.

## Audit verification

1. Run a workflow execution that triggers MCP tool calls.
2. Inspect `data/agent-governance-audit/agent-governance-audit.jsonl` on the MCP host.

## Provisioning traceability

Agent provisioning fingerprints include governance YAML content. The provisioner emits:

- `governanceToolkitVersion: 4.0.0`
- `policyBundleVersion: v1`
- `governedAgents: [...]`

Policy changes therefore bump the Foundry agent version fingerprint and trigger reprovisioning on the next deploy.
