[CmdletBinding()]
param(
    [string]$GameDir = 'F:\Steam\steamapps\common\Stonewards',
    [string]$ProfileDir = '',
    [string]$OutputFile = ''
)
$ErrorActionPreference = 'Stop'

$repoDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    $OutputFile = Join-Path $repoDir 'ring-selection-inspection.txt'
}
$gameAssembly = Join-Path $GameDir 'Stonewards_Data\Managed\Assembly-CSharp.dll'
if (-not (Test-Path -LiteralPath $gameAssembly)) {
    throw "Stonewards Assembly-CSharp.dll not found: $gameAssembly"
}
$bepinexRoot = if ($ProfileDir) { Join-Path $ProfileDir 'BepInEx' } else { Join-Path $GameDir 'BepInEx' }
$cecilCandidates = @(
    (Join-Path $bepinexRoot 'core\Mono.Cecil.dll'),
    (Join-Path $GameDir 'BepInEx\core\Mono.Cecil.dll')
)
$cecil = $cecilCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $cecil) {
    throw "BepInEx Mono.Cecil.dll not found. Please install/enable BepInEx 5 in the chosen profile."
}
Add-Type -Path $cecil

function Flatten-Types($types) {
    foreach ($type in $types) {
        $type
        if ($type.HasNestedTypes) { Flatten-Types $type.NestedTypes }
    }
}
function Method-Signature($method) {
    $parameters = @($method.Parameters | ForEach-Object {
        "$($_.ParameterType.FullName) $($_.Name)"
    }) -join ', '
    "$($method.ReturnType.FullName) $($method.DeclaringType.FullName)::$($method.Name)($parameters)"
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('Stonewards ring selection inspector')
$lines.Add('Game assembly: ' + $gameAssembly)
$lines.Add('Build size: ' + (Get-Item -LiteralPath $gameAssembly).Length)
$lines.Add('Inspection is read-only. No game or save files are modified.')
$lines.Add('')

$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($gameAssembly)
try {
    $allTypes = @(Flatten-Types $assembly.MainModule.Types)
    # Includes nested UpgradePopupController/UpgradeButton and nearby controllers.
    $targetTypes = @($allTypes | Where-Object {
        $_.FullName -match '(?i)(RogueLikeUpgradeMenu|UpgradePopupController|UpgradeButton|UpgradeDraftChoice)'
    })
    $lines.Add('=== MATCHING TYPES AND MEMBERS (' + $targetTypes.Count + ') ===')
    foreach ($type in $targetTypes) {
        $lines.Add('')
        $lines.Add('TYPE ' + $type.FullName + ' : ' + $type.BaseType.FullName)
        foreach ($field in $type.Fields) {
            $lines.Add('  FIELD ' + $field.FieldType.FullName + ' ' + $field.Name)
        }
        foreach ($property in $type.Properties) {
            $lines.Add('  PROP ' + $property.PropertyType.FullName + ' ' + $property.Name)
        }
        foreach ($method in $type.Methods) {
            $lines.Add('  METHOD ' + (Method-Signature $method))
        }
    }
    $lines.Add('')
    $lines.Add('=== METHOD BODIES: UI SELECTION, CLICK, FOCUS, AND ENABLE ===')
    foreach ($type in $targetTypes) {
        foreach ($method in $type.Methods) {
            if (-not $method.HasBody) { continue }
            $lines.Add('')
            $lines.Add('----------------------------------------------------------------')
            $lines.Add((Method-Signature $method))
            foreach ($instruction in $method.Body.Instructions) {
                $lines.Add('  ' + $instruction.ToString())
            }
        }
    }

    $lines.Add('')
    $lines.Add('=== ALL CALLERS OF TARGET METHODS / FIELDS ===')
    $refPattern = '(?i)(RogueLikeUpgradeMenu|UpgradePopupController|UpgradeButton|OnUpgradeClicked|EnableOptions|SelectUpgrade|SelectedUpgrade|SetSelected|OnClick|Focus|Blur)'
    $callerLines = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($type in $allTypes) {
        foreach ($method in $type.Methods) {
            if (-not $method.HasBody) { continue }
            $methodName = Method-Signature $method
            foreach ($instruction in $method.Body.Instructions) {
                if ($null -eq $instruction.Operand) { continue }
                if ($instruction.Operand.ToString() -match $refPattern) {
                    [void]$callerLines.Add($methodName + ' | ' + $instruction.ToString())
                }
            }
        }
    }
    foreach ($entry in ($callerLines | Sort-Object)) {
        $lines.Add($entry)
    }
}
finally {
    $assembly.Dispose()
}

$parent = Split-Path -Parent $OutputFile
if ($parent -and -not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}
$lines | Set-Content -LiteralPath $OutputFile -Encoding UTF8
Write-Host "Inspection saved: $OutputFile" -ForegroundColor Green
Write-Host 'Attach ring-selection-inspection.txt to our chat; game files remain untouched.'
