[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_-]*$')]
    [string]$Name
)

$scriptsDirectory = Join-Path $PSScriptRoot 'Scripts'
if (-not (Test-Path -LiteralPath $scriptsDirectory -PathType Container)) {
    throw "Migration scripts directory was not found: $scriptsDirectory"
}

$latestVersion = Get-ChildItem -LiteralPath $scriptsDirectory -Filter '*.sql' -File |
    ForEach-Object {
        $match = [regex]::Match($_.Name, '^([0-9]{4,})_')
        if ($match.Success) {
            [long]$match.Groups[1].Value
        }
    } |
    Measure-Object -Maximum |
    Select-Object -ExpandProperty Maximum

$nextVersion = if ($null -eq $latestVersion) { 1 } else { $latestVersion + 1 }
$version = ([long]$nextVersion).ToString('D4', [Globalization.CultureInfo]::InvariantCulture)
$filePath = Join-Path $scriptsDirectory "${version}_${Name}.sql"

if (Test-Path -LiteralPath $filePath) {
    throw "Migration script already exists: $filePath"
}

New-Item -Path $filePath -ItemType File | Out-Null
Write-Output "Created migration script: $filePath"
