# Troubleshooting

## Deployment issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Deployment fails early | Invalid parameters or quota | Check Azure Portal deployment error; verify region model availability |
| Post-deploy runner fails or exceeds 15–30 minutes | Bootstrap or provisioning job failed or is slow | Inspect the `post-deploy-runner` container logs and Container Apps job execution logs |
| Container image pull failed | Private GHCR packages | Make `ghcr.io/foundry-industry-sandbox/inventoryplanning-*` images public |
| Fabric deploy fails | Missing UAMI or workspace | Run `setup-fabric-provision-identity.ps1` in the deployment resource group; pass `fabricIdentityName` |

## Model availability issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Agent errors mentioning capacity | `no_capacity` on Grok deployment | Increase `modelDeploymentCapacity` in Bicep parameters |
| Model not found | Region or catalog mismatch | Confirm Grok 4.6 is available in target region |
| Embedding failures | Missing embed deployment | Verify foundry.bicep completed; check `embedDeploymentName` output |

## Agent provisioning issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Provisioning job failed | Wrong MCP URL | Re-run job with correct public `mcpUrl` |
| Agents missing tools | Bootstrap order issue | Ensure IQ bootstrap completed before provisioning |
| Invalid structured output | Schema mismatch | Check agent version and shared JSON schemas |

## Backend startup issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| API fails on start | Missing Foundry endpoint | Set `AZURE_FOUNDRY_PROJECT_ENDPOINT` in `.env.local` |
| Workflow `503` | Agents not provisioned | Run agent provisioning CLI or redeploy |
| Execution not found | API restarted | In-memory store cleared; start a new workflow |

## Frontend connection issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Cannot load cases | Wrong dataset path | Verify `DatasetSeed:RootPath` points to `dataset-seed` |
| API connection refused | Backend not running | Start API; set `PlanningApi__BaseUrl` |
| Landing page loads but controls do nothing; `/_framework/blazor.web.js` returns `404` | Frontend image was published without the .NET 10 Blazor asset package because Docker restored before copying Razor files | Rebuild the frontend with `RequiresAspNetWebAssets=true` in its project file and deploy the new image. Run `infra/scripts/test-frontend.ps1 -BaseUrl <frontend-url>`. Do not switch to Development or restart the same incomplete image. See the private-image recovery option in `infra/README.md`. |
| Workflow polling timeout | Slow agents or failure | Check status endpoint; review API and Foundry logs |

## Dataset issues

| Symptom | Possible cause | Resolution |
|---|---|---|
| Case not found | Unsupported case ID | Use `case-01` … `case-05` |
| Empty documents list | Missing ingest folder | Regenerate from `data-generation` or verify `dataset-seed` |
| MCP tool data empty | Wrong data mode | Confirm `DataSource:Mode` is `Local` or Fabric is seeded; run `test-fabric-mcp-read.ps1`; check `GET /health` for `fabricReachable` |
| RAG knowledge file missing | Paths configured but files absent | Add files under `dataset-seed/*-rag/` or update MCP paths |

## Quota and regional availability

| Symptom | Possible cause | Resolution |
|---|---|---|
| Search SKU unavailable | Region restriction | Try `eastus` or adjust `searchSku` |
| Foundry quota exceeded | Subscription limits | Request quota increase or reduce deployment capacity |
| Grok not in region | Model rollout | Select a supported region or update model parameters |

See also [infra/README.md](../infra/README.md) and [backend/README.md](../backend/README.md).
