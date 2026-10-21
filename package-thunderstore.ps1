[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [Parameter(Mandatory = $true)][string]$ProfileDir,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$manifestPath = Join-Path $root 'thunderstore\manifest.json'
$readmePath = Join-Path $root 'thunderstore\README.md'
$changelogPath = Join-Path $root 'CHANGELOG.md'
$iconPath = Join-Path $root 'thunderstore\icon.png'
$sourcePath = Join-Path $root 'src\UndoRingSelectionPlugin.cs'
$dllPath = Join-Path $root 'bin\Release\netstandard2.1\StonewardsUndoRingSelection.dll'

foreach ($path in @($manifestPath, $readmePath, $changelogPath, $iconPath, $sourcePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing required release file: $path (a 256x256 PNG icon is needed at thunderstore/icon.png)"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.name -ne 'UndoRingSelection' -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Invalid UndoRingSelection manifest name or semantic version.'
}

$source = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
$expectedVersion = 'public const string PluginVersion = "' + $manifest.version_number + '";'
if (-not $source.Contains($expectedVersion)) {
    throw 'Plugin version must match the manifest version.'
}

# PNG signature and IHDR dimensions: Thunderstore requires 256x256.
$bytes = [System.IO.File]::ReadAllBytes($iconPath)
$signature = [byte[]](137,80,78,71,13,10,26,10)
if ($bytes.Length -lt 24) { throw 'Invalid icon PNG.' }
for ($i = 0; $i -lt 8; $i++) {
    if ($bytes[$i] -ne $signature[$i]) { throw 'Invalid icon PNG signature.' }
}
$width = ([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor
         ([int]$bytes[18] -shl 8) -bor [int]$bytes[19]
$height = ([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor
          ([int]$bytes[22] -shl 8) -bor [int]$bytes[23]
if ($width -ne 256 -or $height -ne 256) {
    throw "Thunderstore icon must be 256x256 pixels, got ${width}x${height}."
}

if (-not $SkipBuild) {
    $core = Join-Path $ProfileDir 'BepInEx\core'
    foreach ($ref in @(
        (Join-Path $GameDir 'Stonewards_Data\Managed\Assembly-CSharp.dll'),
        (Join-Path $core 'BepInEx.dll'),
        (Join-Path $core '0Harmony.dll')
    )) {
        if (-not (Test-Path -LiteralPath $ref -PathType Leaf)) {
            throw "Missing build reference: $ref"
        }
    }
    Write-Host "Building UndoRingSelection $($manifest.version_number)..." -ForegroundColor Cyan
    # Build without installing: an open game may lock the plugin DLL in its profile.
    & dotnet build (Join-Path $root 'StonewardsUndoRingSelection.csproj') -c Release "-p:GameDir=$GameDir" "-p:BepInExCoreDir=$core" --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }
}

if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "Compiled DLL not found: $dllPath. Build before packaging."
}
if ((Get-Item -LiteralPath $dllPath).Length -lt 4096) {
    throw 'DLL is unexpectedly small.'
}

$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'undo-ring-selection-staging'
$archive = Join-Path $dist ("UndoRingSelection-" + $manifest.version_number + '.zip')
New-Item -ItemType Directory -Path $dist -Force | Out-Null
if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Force -Recurse
}

try {
    $pluginFolder = Join-Path $stage 'plugins\StonewardsUndoRingSelection'
    New-Item -ItemType Directory -Path $pluginFolder -Force | Out-Null
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $stage 'manifest.json')
    Copy-Item -LiteralPath $readmePath -Destination (Join-Path $stage 'README.md')
    Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $stage 'CHANGELOG.md')
    Copy-Item -LiteralPath $iconPath -Destination (Join-Path $stage 'icon.png')
    Copy-Item -LiteralPath $dllPath -Destination (Join-Path $pluginFolder 'StonewardsUndoRingSelection.dll')

    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -CompressionLevel Optimal

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
        foreach ($required in @('manifest.json', 'README.md', 'CHANGELOG.md', 'icon.png',
                                'plugins/StonewardsUndoRingSelection/StonewardsUndoRingSelection.dll')) {
            if ($entries -notcontains $required) { throw "Archive missing: $required" }
        }
    } finally {
        $zip.Dispose()
    }

    Write-Host "Thunderstore upload archive: $archive" -ForegroundColor Green
    Write-Host 'Review ZIP contents before publishing; nothing was uploaded.' -ForegroundColor Yellow
} finally {
    if (Test-Path -LiteralPath $stage) {
        Remove-Item -LiteralPath $stage -Force -Recurse
    }
}
