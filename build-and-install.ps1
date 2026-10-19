[CmdletBinding()]
param(
    [string]$GameDir = 'F:\Steam\steamapps\common\Stonewards',
    [string]$ProfileDir = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$managed = Join-Path $GameDir 'Stonewards_Data\Managed'
$core = if ($ProfileDir) {
    Join-Path $ProfileDir 'BepInEx\core'
} else {
    Join-Path $GameDir 'BepInEx\core'
}
$plugins = if ($ProfileDir) {
    Join-Path $ProfileDir 'BepInEx\plugins'
} else {
    Join-Path $GameDir 'BepInEx\plugins'
}
foreach ($required in @(
    (Join-Path $managed 'Assembly-CSharp.dll'),
    (Join-Path $managed 'UnityEngine.UIElementsModule.dll'),
    (Join-Path $core 'BepInEx.dll'),
    (Join-Path $core '0Harmony.dll')
)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Missing reference: $required"
    }
}
$project = Join-Path $repo 'StonewardsUndoRingSelection.csproj'
& dotnet build $project -c Release "-p:GameDir=$GameDir" "-p:BepInExCoreDir=$core" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed; no DLL was installed.' }
$dll = Join-Path $repo 'bin\Release\netstandard2.1\StonewardsUndoRingSelection.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw "Build artifact missing: $dll" }
$dest = Join-Path $plugins 'StonewardsUndoRingSelection'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination $dest -Force
Write-Host "Installed to: $dest" -ForegroundColor Green
Write-Host "Test profile name: stonewards-undo-ring-selection"
Write-Host 'If anything fails, provide BepInEx/LogOutput.log and the game build number.'
