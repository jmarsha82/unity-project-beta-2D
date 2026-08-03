$ErrorActionPreference = 'Stop'

function Assert-Contains {
    param(
        [string] $Text,
        [string] $Expected,
        [string] $Message
    )

    if (-not $Text.Contains($Expected)) {
        throw $Message
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$scenePath = Join-Path $repoRoot 'Assets/Settings/Scenes 1/FirstLevel.unity'
$manifestPath = Join-Path $repoRoot 'Packages/manifest.json'
$lockPath = Join-Path $repoRoot 'Packages/packages-lock.json'

if (-not (Test-Path -LiteralPath $scenePath)) {
    throw "Missing scene: $scenePath"
}

$scene = Get-Content -LiteralPath $scenePath -Raw

Assert-Contains $scene 'm_Name: Dirt Body' 'FirstLevel is missing the dirt body.'
Assert-Contains $scene 'm_LocalScale: {x: 210, y: 4, z: 1}' 'Dirt body does not preserve the expected 210-unit level length.'
Assert-Contains $scene 'm_Name: Grass Top Play Surface' 'FirstLevel is missing the grass play surface.'
Assert-Contains $scene 'BoxCollider2D:' 'Grass play surface is missing its 2D collider.'
Assert-Contains $scene 'm_Name: Start Flag Pole' 'FirstLevel is missing the start flag pole.'
Assert-Contains $scene 'm_Name: End Flag Pole' 'FirstLevel is missing the end flag pole.'
Assert-Contains $scene 'm_LocalPosition: {x: 208, y: 1, z: -0.1}' 'End flag pole is not at the expected level-end position.'
Assert-Contains $scene 'm_BackGroundColor: {r: 1, g: 1, b: 1, a: 1}' 'Main camera background is not white.'

$shadingObjects = @(
    'Grass Top Highlight',
    'Grass Front Edge Shadow',
    'Dirt Upper Highlight',
    'Dirt Bottom Shadow',
    'Start Pole Shadow',
    'End Pole Shadow',
    'Start Flag Cloth Highlight',
    'Start Flag Cloth Shadow',
    'End Flag Cloth Highlight',
    'End Flag Cloth Shadow'
)

foreach ($objectName in $shadingObjects) {
    Assert-Contains $scene "m_Name: $objectName" "Missing shading object: $objectName"
}

$serializedIds = [regex]::Matches($scene, '(?m)^--- !u!\d+ &(\d+)') | ForEach-Object { $_.Groups[1].Value }
$duplicateIds = $serializedIds | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicateIds) {
    throw "Duplicate Unity serialized IDs found: $($duplicateIds.Name -join ', ')"
}

Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json | Out-Null
Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json | Out-Null

Write-Host 'FirstLevel asset validation passed.'
