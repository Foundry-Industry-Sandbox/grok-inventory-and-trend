$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

Push-Location $repoRoot
try {
    dotnet restore agent-provisioning/src/GrokInventoryAndTrend.AgentProvisioning --configfile NuGet.Config --source https://packagefeedproxy.microsoft.io/nuget/v3/index.json
    dotnet run --project agent-provisioning/src/GrokInventoryAndTrend.AgentProvisioning --no-restore
}
finally {
    Pop-Location
}
