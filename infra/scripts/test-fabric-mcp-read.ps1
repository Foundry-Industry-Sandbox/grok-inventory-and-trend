param(
    [Parameter(Mandatory = $true)]
    [string]$WorkspaceName,

    [Parameter(Mandatory = $true)]
    [string]$LakehouseName,

    [string]$WorkspaceId = '',

    [string]$LakehouseId = '',

    [string]$CaseId = 'case-01',

    [string]$EvidenceRoot = 'Files/bronze',

    [string]$DfsEndpoint = 'https://onelake.dfs.fabric.microsoft.com',

    [string]$BlobEndpoint = 'https://onelake.blob.fabric.microsoft.com'
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($WorkspaceId)) { $WorkspaceId = $env:FABRIC_WORKSPACE_ID }
if ([string]::IsNullOrWhiteSpace($LakehouseId)) { $LakehouseId = $env:FABRIC_LAKEHOUSE_ID }

function Get-OneLakeAccessToken {
    $resource = 'https://storage.azure.com'

    if (-not [string]::IsNullOrWhiteSpace($env:AZURE_CLIENT_ID)) {
        try {
            $uri = "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=$resource&client_id=$($env:AZURE_CLIENT_ID)"
            $resp = Invoke-RestMethod -Uri $uri -Headers @{ Metadata = 'true' }
            if (-not [string]::IsNullOrWhiteSpace($resp.access_token)) {
                return $resp.access_token
            }
        }
        catch {
            Write-Verbose "IMDS token request failed: $_"
        }
    }

    try {
        return (Get-AzAccessToken -ResourceUrl $resource).Token
    }
    catch {}

    $token = az account get-access-token --resource $resource --query accessToken -o tsv 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($token)) {
        return $token
    }

    throw "Unable to acquire access token for '$resource'."
}

function Test-OneLakePath {
    param(
        [string]$Label,
        [string]$ListUri,
        [string]$ReadUri,
        [string]$Token
    )

    Write-Host "=== $Label ==="
    Write-Host "List URI: $ListUri"

    $headers = @{ Authorization = "Bearer $Token" }
    try {
        $listResp = Invoke-WebRequest -Method GET -Uri $ListUri -Headers $headers -UseBasicParsing
        Write-Host "List: OK ($($listResp.StatusCode))"
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        Write-Host "List: FAILED ($status) — $($_.Exception.Message)"
        return [pscustomobject]@{
            Label     = $Label
            ListOk    = $false
            ReadOk    = $false
            ListUri   = $ListUri
            ReadUri   = $ReadUri
        }
    }

    Write-Host "Read URI: $ReadUri"
    try {
        $readResp = Invoke-WebRequest -Method GET -Uri $ReadUri -Headers $headers -UseBasicParsing
        Write-Host "Read: OK ($($readResp.StatusCode), $($readResp.RawContentLength) bytes)"
        return [pscustomobject]@{
            Label     = $Label
            ListOk    = $true
            ReadOk    = $true
            ListUri   = $ListUri
            ReadUri   = $ReadUri
        }
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        Write-Host "Read: FAILED ($status) — $($_.Exception.Message)"
        return [pscustomobject]@{
            Label     = $Label
            ListOk    = $true
            ReadOk    = $false
            ListUri   = $ListUri
            ReadUri   = $ReadUri
        }
    }
}

Write-Host '=== Fabric MCP read path verification ==='
Write-Host "WorkspaceName: $WorkspaceName"
Write-Host "LakehouseName: $LakehouseName"
Write-Host "WorkspaceId: $WorkspaceId"
Write-Host "LakehouseId: $LakehouseId"
Write-Host "CaseId: $CaseId"
Write-Host "EvidenceRoot: $EvidenceRoot"

$token = Get-OneLakeAccessToken
$relativeDir = "$EvidenceRoot/cases/$CaseId/fabric-pre-requisite-data/pos_transaction_batch"
$sampleFile = Get-ChildItem -Path (Join-Path $PSScriptRoot "../../dataset-seed/cases/$CaseId/fabric-pre-requisite-data/pos_transaction_batch") -Filter '*.json' -ErrorAction SilentlyContinue |
    Select-Object -First 1

if (-not $sampleFile) {
    throw "No sample JSON found under dataset-seed/cases/$CaseId/fabric-pre-requisite-data/pos_transaction_batch"
}

$relativeFile = "$relativeDir/$($sampleFile.Name)"
$results = @()

# MCP client convention: workspace/lakehouse display names + .lakehouse suffix
$mcpListUri = "$DfsEndpoint/$WorkspaceName/$LakehouseName.lakehouse/$relativeDir`?resource=filesystem&recursive=true"
$mcpReadUri = "$BlobEndpoint/$WorkspaceName/$LakehouseName.lakehouse/$relativeFile"
$results += Test-OneLakePath -Label 'McpNamePath' -ListUri $mcpListUri -ReadUri $mcpReadUri -Token $token

# Seed script convention: workspace/lakehouse GUIDs (no .lakehouse suffix on lakehouse id)
if (-not [string]::IsNullOrWhiteSpace($WorkspaceId) -and -not [string]::IsNullOrWhiteSpace($LakehouseId)) {
    $seedListUri = "$DfsEndpoint/$WorkspaceId/$LakehouseId/$relativeDir`?resource=filesystem&recursive=true"
    $seedReadUri = "$BlobEndpoint/$WorkspaceId/$LakehouseId/$relativeFile"
    $results += Test-OneLakePath -Label 'SeedIdPath' -ListUri $seedListUri -ReadUri $seedReadUri -Token $token
}
else {
    Write-Host '=== SeedIdPath ==='
    Write-Host 'Skipped (FABRIC_WORKSPACE_ID / FABRIC_LAKEHOUSE_ID not provided).'
}

Write-Host ''
Write-Host '=== Summary ==='
$results | Format-Table -AutoSize

$mcpOk = @($results | Where-Object { $_.Label -eq 'McpNamePath' -and $_.ReadOk }).Count -gt 0
$seedOk = @($results | Where-Object { $_.Label -eq 'SeedIdPath' -and $_.ReadOk }).Count -gt 0

if ($mcpOk) {
    Write-Host 'MCP name-based read path is reachable. FabricPlanningDataStore should work with workspace/lakehouse display names.'
    exit 0
}

if ($seedOk -and -not $mcpOk) {
    Write-Host 'WARNING: Seed ID-based path works but MCP name-based path does not. Verify workspace/lakehouse display names match Fabric portal values.'
    exit 1
}

Write-Host 'ERROR: Neither MCP nor seed read path succeeded. Confirm bronze seed completed and identity has OneLake access.'
exit 1
