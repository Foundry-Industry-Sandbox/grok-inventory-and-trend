[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri]$BaseUrl
)

$ErrorActionPreference = 'Stop'
if (-not $BaseUrl.IsAbsoluteUri -or $BaseUrl.Scheme -notin @('http', 'https')) {
    throw 'BaseUrl must be an absolute HTTP or HTTPS URL.'
}

$root = [uri]::new($BaseUrl.AbsoluteUri.TrimEnd('/') + '/')
$health = Invoke-RestMethod -Uri ([uri]::new($root, 'health')) -TimeoutSec 30
if ($health.status -ne 'ok') {
    throw 'Frontend health endpoint did not return status=ok.'
}

$page = Invoke-WebRequest -Uri $root -TimeoutSec 30
$scriptMatch = [regex]::Match(
    $page.Content,
    'src=["''](?<src>[^"'']*_framework/blazor\.web(?:\.[^"''/]+)?\.js)["'']'
)
if (-not $scriptMatch.Success) {
    throw 'The landing page does not reference the Blazor web bootstrap script.'
}

$scriptUrl = [uri]::new($root, $scriptMatch.Groups['src'].Value)
if ($scriptUrl.Authority -ne $root.Authority -or $scriptUrl.Scheme -ne $root.Scheme) {
    throw "The Blazor script points outside the frontend: $scriptUrl"
}

$script = Invoke-WebRequest -Uri $scriptUrl -TimeoutSec 30
if ($script.Headers['Content-Type'] -notmatch 'javascript' -or
    $script.RawContentLength -lt 10000 -or $script.Content -notmatch 'Blazor') {
    throw "The Blazor endpoint did not return the expected JavaScript: $scriptUrl"
}

$negotiate = Invoke-RestMethod -Method Post `
    -Uri ([uri]::new($root, '_blazor/negotiate?negotiateVersion=1')) `
    -TimeoutSec 30
if (-not $negotiate.connectionToken -or
    'WebSockets' -notin $negotiate.availableTransports.transport) {
    throw 'Blazor SignalR negotiation did not offer a WebSocket connection.'
}

Write-Host "Frontend health, Blazor JavaScript, and SignalR negotiation passed for $root"
