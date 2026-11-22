# Minimal Unity MCP (HTTP/SSE) client for WWG. PowerShell 5.1 compatible.
# Usage: powershell -File _tools/unity_mcp.ps1 -Tool execute_menu_item -ArgsJson '{"menu_path":"WWG/..."}'
param(
    [string]$Tool = "tools/list",
    [string]$ArgsJson = "{}",
    [string]$Uri = "http://127.0.0.1:8080/mcp",
    [switch]$Names,
    [string]$SchemaFor = "",
    [switch]$Raw
)

$ErrorActionPreference = "Stop"
$script:sessionId = $null
$script:id = 100

function Invoke-Rpc([string]$method, $params, [bool]$isNotification = $false) {
    $script:id++
    $payload = @{ jsonrpc = "2.0"; method = $method }
    if (-not $isNotification) { $payload.id = $script:id }
    if ($null -ne $params) { $payload.params = $params }
    $json = $payload | ConvertTo-Json -Depth 30 -Compress
    $file = Join-Path $env:TEMP ("mcp_" + [guid]::NewGuid().ToString("N") + ".json")
    [System.IO.File]::WriteAllText($file, $json, (New-Object System.Text.UTF8Encoding($false)))
    $hdr = @("Content-Type: application/json", "Accept: application/json, text/event-stream")
    if ($script:sessionId) { $hdr += "mcp-session-id: $($script:sessionId)" }
    $curlArgs = @("-s", "-i", "-N", "-X", "POST", $Uri, "--max-time", "180")
    foreach ($h in $hdr) { $curlArgs += @("-H", $h) }
    $curlArgs += @("--data-binary", "@$file")
    $out = & curl.exe @curlArgs 2>$null
    Remove-Item $file -Force -ErrorAction SilentlyContinue
    $text = ($out -join "`n")
    $m = [regex]::Match($text, '(?im)^mcp-session-id:\s*(\S+)\s*$')
    if ($m.Success) { $script:sessionId = $m.Groups[1].Value }
    $result = $null
    foreach ($line in ($text -split "`n")) {
        if ($line -notmatch '^data: ') { continue }
        $body = $line.Substring(6).Trim()
        try { $obj = $body | ConvertFrom-Json } catch { continue }
        if ($isNotification) { continue }
        if ($obj.id -eq $script:id) { $result = $obj; break }
    }
    if ($null -eq $result -and -not $isNotification) {
        # Server may reply with plain JSON instead of SSE.
        $trim = $text.Trim()
        $start = $trim.IndexOf('{"jsonrpc"')
        if ($start -ge 0) { try { $result = $trim.Substring($start) | ConvertFrom-Json } catch { } }
    }
    return $result
}

$init = Invoke-Rpc "initialize" @{
    protocolVersion = "2024-11-05"
    capabilities = @{}
    clientInfo = @{ name = "dsh"; version = "0.1" }
}
if ($null -eq $init) { Write-Error "Unity MCP initialize failed"; exit 2 }
Invoke-Rpc "notifications/initialized" $null $true | Out-Null

if ($Tool -eq "tools/list") {
    $res = Invoke-Rpc "tools/list" @{}
    if ($SchemaFor) {
        ($res.result.tools | Where-Object { $_.name -eq $SchemaFor }) | ConvertTo-Json -Depth 40
    } elseif ($Names) {
        foreach ($t in $res.result.tools) { $t.name }
    } else {
        $res.result.tools | ForEach-Object { "{0} | {1}" -f $_.name, $_.description.Split("`n")[0] }
    }
    exit 0
}

$argObj = $ArgsJson | ConvertFrom-Json
$call = Invoke-Rpc "tools/call" @{ name = $Tool; arguments = $argObj }
if ($Raw) { $call | ConvertTo-Json -Depth 40; exit 0 }
if ($call -and $call.error) {
    Write-Error ("MCP error: " + ($call.error | ConvertTo-Json -Depth 10))
    exit 3
}
if ($call -and $call.result) {
    if ($call.result.structuredContent) {
        $sc = $call.result.structuredContent
        if ($sc.result) { $sc.result | ConvertTo-Json -Depth 40 } else { $sc | ConvertTo-Json -Depth 40 }
    } elseif ($call.result.content) {
        foreach ($c in $call.result.content) {
            if ($c.type -eq "text") { $c.text } else { $c | ConvertTo-Json -Depth 20 }
        }
    } else {
        $call.result | ConvertTo-Json -Depth 30
    }
} else {
    "NO RESPONSE"
}
