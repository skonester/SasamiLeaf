param(
    [string]$Destination = (Join-Path $PSScriptRoot '../Plugins/YoutubeDL/Libs'),
    [switch]$Update
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$Destination = [IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
if (-not $Update -and (Test-Path -LiteralPath (Join-Path $Destination 'yt-dlp.exe')) -and (Test-Path -LiteralPath (Join-Path $Destination 'deno.exe')) -and (Test-Path -LiteralPath (Join-Path $Destination 'web-playback-tools.json'))) {
    return
}

# Download from the official projects and verify their published SHA-256 hashes.
$headers = @{ 'User-Agent' = 'SasamiLeaf-build' }
function Read-ReleaseText([string]$Url) {
    $response = Invoke-WebRequest $Url -UseBasicParsing -TimeoutSec 120
    if ($response.Content -is [byte[]]) { return [Text.Encoding]::UTF8.GetString($response.Content) }
    return [string]$response.Content
}
$staging = Join-Path ([IO.Path]::GetTempPath()) ('SasamiLeaf-web-tools-' + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    $ytRelease = Invoke-RestMethod 'https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest' -Headers $headers
    $denoRelease = Invoke-RestMethod 'https://api.github.com/repos/denoland/deno/releases/latest' -Headers $headers
    $ytBase = "https://github.com/yt-dlp/yt-dlp/releases/download/$($ytRelease.tag_name)"
    $denoBase = "https://github.com/denoland/deno/releases/download/$($denoRelease.tag_name)"
    $denoArchive = 'deno-x86_64-pc-windows-msvc.zip'
    Invoke-WebRequest "$ytBase/yt-dlp.exe" -OutFile (Join-Path $staging 'yt-dlp.exe') -UseBasicParsing
    $ytHashes = Read-ReleaseText "$ytBase/SHA2-256SUMS"
    $ytHash = [regex]::Match($ytHashes, '(?m)^([a-fA-F0-9]{64})\s+\*?yt-dlp\.exe\s*$').Groups[1].Value
    Invoke-WebRequest "$denoBase/$denoArchive" -OutFile (Join-Path $staging $denoArchive) -UseBasicParsing
    $denoHashes = Read-ReleaseText "$denoBase/$denoArchive.sha256sum"
    $denoHash = [regex]::Match($denoHashes, '\b([a-fA-F0-9]{64})\b').Groups[1].Value
    foreach ($item in @(@{ Name = 'yt-dlp.exe'; Hash = $ytHash }, @{ Name = $denoArchive; Hash = $denoHash })) {
        $actual = (Get-FileHash -LiteralPath (Join-Path $staging $item.Name) -Algorithm SHA256).Hash
        if (-not $item.Hash -or $actual -ine $item.Hash) { throw "SHA-256 verification failed for $($item.Name)." }
    }
    Expand-Archive -LiteralPath (Join-Path $staging $denoArchive) -DestinationPath (Join-Path $staging 'deno')
    # Retain upstream notices alongside the distributed executables.
    Invoke-WebRequest "https://raw.githubusercontent.com/yt-dlp/yt-dlp/$($ytRelease.tag_name)/THIRD_PARTY_LICENSES.txt" -OutFile (Join-Path $Destination 'yt-dlp-THIRD-PARTY-LICENSES.txt') -UseBasicParsing
    Invoke-WebRequest "https://raw.githubusercontent.com/denoland/deno/$($denoRelease.tag_name)/LICENSE.md" -OutFile (Join-Path $Destination 'deno-LICENSE.md') -UseBasicParsing
    Copy-Item -LiteralPath (Join-Path $staging 'yt-dlp.exe') -Destination $Destination -Force
    Copy-Item -LiteralPath (Join-Path $staging 'deno/deno.exe') -Destination $Destination -Force
    @{ ytDlp = $ytRelease.tag_name; deno = $denoRelease.tag_name; ytDlpSha256 = $ytHash; denoArchiveSha256 = $denoHash } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'web-playback-tools.json') -Encoding UTF8
    Write-Host "Prepared yt-dlp $($ytRelease.tag_name) and Deno $($denoRelease.tag_name)."
} finally {
    # The only recursively removed path is the unique staging directory created above.
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not ([IO.Path]::GetFullPath($staging).StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) -or
        [IO.Path]::GetFileName($staging) -notlike 'SasamiLeaf-web-tools-*') { throw 'Unexpected staging path.' }
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
}
