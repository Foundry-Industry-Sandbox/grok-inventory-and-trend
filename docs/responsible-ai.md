# Responsible AI considerations

## Intended use

Demonstration of a multi-agent retail inventory planning workflow on Microsoft Foundry. Intended for evaluation, workshops, and proof-of-concept discussions—not production decision-making.

## Out-of-scope use

- Automated ordering without human review in production.
- Processing real customer or employee PII.
- Compliance-critical financial or safety decisions without independent validation.
- High-throughput batch planning at enterprise scale.

## Human oversight

- Planner copilot presents recommendations for human approval in the Blazor UI.
- Approval is client-side in this sample; production implementations should persist audit trails and enforce authorization.
- Cases `case-02`, `case-03`, and `case-05` demonstrate different human-review triggers.

## Evaluation

- Ground truth files in `data-generation/ground-truth/` support offline validation of expected outcomes.
- No automated evaluation pipeline is deployed with the sample. Review agent outputs manually during demos.

## Tracing and monitoring

- Application Insights is provisioned for API, MCP, and jobs.
- Use `appInsightsLiveMetricsUrl` to observe request flow during demos.
- Foundry tracing capabilities should be reviewed before production use.

## Safety and compliance

- Policies in `policies.json` demonstrate constraint grounding via Foundry IQ.
- Model outputs may be incorrect or incomplete. Always review before acting.
- This sample does not implement content safety filters beyond Foundry defaults.

## Data privacy

- All demo data is synthetic.
- Do not upload real customer data to the demo environment.
- Delete the resource group after demos to remove deployed resources and indexes.

## Limitations

- Not production-ready.
- Workflow memory is ephemeral.
- MCP authentication is open for demonstration.
- Model availability and behavior vary by region and quota.
- No certification or compliance guarantees are implied.
