# ============================================================================
# Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
# File Name    : BumpVersion.ps1
# Version      : 1.6
# Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
# File Location: BumpVersion.ps1
# Description  : Professional version management pipeline implementing DELTA SYNTH formula
#                without runaway BaseVersion compounding on uncommitted working trees.
# ============================================================================

[CmdletBinding()]
param(
    [string] $ProjectPath = "OpenUtau\OpenUtau.csproj",
    [switch] $Preview,
    [switch] $ForceIncrement
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$resolvedProject = (Resolve-Path -LiteralPath (Join-Path $scriptDir $ProjectPath)).Path
[xml] $projectXml = Get-Content -LiteralPath $resolvedProject -Raw -Encoding UTF8

$versionNodes = @($projectXml.SelectNodes("/Project/PropertyGroup/Version"))
if ($versionNodes.Count -eq 0) {
    throw "The project does not contain a Version element: $resolvedProject"
}

$versionFile = Join-Path $scriptDir "version.txt"
$currentDate = Get-Date

[string]$explicitVersion = ""
[double]$baseVersion = 3.9
[string]$lastDateStr = ""
[int]$buildNumber = 5

if (Test-Path -LiteralPath $versionFile) {
    $lines = Get-Content -LiteralPath $versionFile
    foreach ($line in $lines) {
        if ($line -match "^Version=([0-9]+\.[0-9]+\.[0-9]+)") {
            $explicitVersion = $matches[1]
        }
        if ($line -match "^BaseVersion=([0-9\.]+)") {
            [double]::TryParse($matches[1], [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$baseVersion) | Out-Null
        }
        if ($line -match "^LastDate=([0-9\-]+)") {
            $lastDateStr = $matches[1]
        }
        if ($line -match "^BuildNumber=([0-9]+)") {
            [int]::TryParse($matches[1], [ref]$buildNumber) | Out-Null
        }
    }
}

if ($baseVersion -lt 2.0 -or $baseVersion -ge 10.0) {
    $baseVersion = 3.9
}

$modifiedFilesCount = 0
try {
    $gitStatus = & git status --porcelain 2>$null
    if ($gitStatus) {
        $modifiedFilesCount = ($gitStatus | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    }
} catch {
    $modifiedFilesCount = 0
}

[double]$monthIncrement = 0.0
if ($ForceIncrement -and -not [string]::IsNullOrEmpty($lastDateStr)) {
    try {
        $lastDate = [DateTime]::ParseExact($lastDateStr, "yyyy-MM-dd", [System.Globalization.CultureInfo]::InvariantCulture)
        if ($currentDate.Year -ne $lastDate.Year -or $currentDate.Month -ne $lastDate.Month) {
            $monthIncrement = 0.5
        }
    } catch {
        $monthIncrement = 0.0
    }
}

if ($ForceIncrement) {
    $buildNumber++
    $baseVersion = [Math]::Round($baseVersion + $monthIncrement, 1)
}

if (-not $ForceIncrement -and -not [string]::IsNullOrEmpty($explicitVersion)) {
    $parts = $explicitVersion.Split('.')
    [int]$major = [int]$parts[0]
    [int]$minor = [int]$parts[1]
    [int]$buildNumber = [int]$parts[2]
} else {
    [int]$major = [int][Math]::Floor($baseVersion)
    [int]$minor = [int][Math]::Round(($baseVersion - $major) * 10)
    if ($minor -ge 10) {
        $major += [int][Math]::Floor($minor / 10)
        $minor = $minor % 10
    }
}

$fullVersionString = "$major.$minor.$buildNumber"
$shortVersionString = "$major.$minor"

Write-Host "======================================================================"
Write-Host " DELTA SYNTH Version Bumper: v$fullVersionString"
Write-Host " - Base Version     : $shortVersionString"
Write-Host " - Monthly Increment: +$monthIncrement"
Write-Host " - Modified Files   : $modifiedFilesCount files"
Write-Host " - Build Sequence   : #$buildNumber"
Write-Host "======================================================================"

if (-not $Preview) {
    foreach ($node in $versionNodes) {
        $node.InnerText = $fullVersionString
    }
    
    $assemblyVersionNodes = @($projectXml.SelectNodes("/Project/PropertyGroup/AssemblyVersion"))
    foreach ($node in $assemblyVersionNodes) {
        $node.InnerText = "$major.$minor.$buildNumber.0"
    }
    
    $fileVersionNodes = @($projectXml.SelectNodes("/Project/PropertyGroup/FileVersion"))
    foreach ($node in $fileVersionNodes) {
        $node.InnerText = "$major.$minor.$buildNumber.0"
    }

    foreach ($nodeName in @("CFBundleVersion", "CFBundleShortVersionString")) {
        foreach ($node in @($projectXml.SelectNodes("/Project/PropertyGroup/$nodeName"))) {
            $node.InnerText = if ($nodeName -eq "CFBundleVersion") { $fullVersionString } else { $shortVersionString }
        }
    }

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $true
    $writer = [System.Xml.XmlWriter]::Create($resolvedProject, $settings)
    try {
        $projectXml.Save($writer)
    } finally {
        $writer.Dispose()
    }

    $versionFileContent = @"
Version=$fullVersionString
BaseVersion=$shortVersionString
LastDate=$($currentDate.ToString("yyyy-MM-dd"))
BuildNumber=$buildNumber
ModifiedFilesCount=$modifiedFilesCount
"@
    [System.IO.File]::WriteAllText(
        $versionFile,
        $versionFileContent,
        [System.Text.UTF8Encoding]::new($false))
}

$fullVersionString

