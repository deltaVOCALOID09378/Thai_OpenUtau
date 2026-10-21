# ============================================================================
# Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
# File Name    : BumpVersion.ps1
# Version      : 1.5
# Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai (Delta) ]
# File Location: BumpVersion.ps1
# Description  : Professional version calculation pipeline implementing DELTA SYNTH formula:
#                Base >= 2.0, +0.5 monthly increment, +0.1 per modified file, and compile tracking.
# ============================================================================

[CmdletBinding()]
param(
    [string] $ProjectPath = "OpenUtau\OpenUtau.csproj",
    [switch] $Preview
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

# 1. Base Version Retrieval (minimum >= 2.0, starting from 3.2 series)
[double]$baseVersion = 3.2
[string]$lastDateStr = ""
[int]$buildNumber = 0

if (Test-Path -LiteralPath $versionFile) {
    $lines = Get-Content -LiteralPath $versionFile
    foreach ($line in $lines) {
        if ($line -match "BaseVersion=([0-9\.]+)") {
            [double]::TryParse($matches[1], [ref]$baseVersion) | Out-Null
        }
        if ($line -match "LastDate=([0-9\-]+)") {
            $lastDateStr = $matches[1]
        }
        if ($line -match "BuildNumber=([0-9]+)") {
            [int]::TryParse($matches[1], [ref]$buildNumber) | Out-Null
        }
    }
} else {
    $existingVer = $versionNodes[0].InnerText
    if ($existingVer -match "^([0-9]+)\.([0-9]+)") {
        $baseVersion = [double]"$($matches[1]).$($matches[2])"
    }
    if ($baseVersion -lt 2.0) { $baseVersion = 2.0 }
}

# 2. Monthly Increment Calculation (+0.5 if month changed or first run)
[double]$monthIncrement = 0.0
if (-not [string]::IsNullOrEmpty($lastDateStr)) {
    try {
        $lastDate = [DateTime]::ParseExact($lastDateStr, "yyyy-MM-dd", [System.Globalization.CultureInfo]::InvariantCulture)
        if ($currentDate.Year -ne $lastDate.Year -or $currentDate.Month -ne $lastDate.Month) {
            $monthIncrement = 0.5
        }
    } catch {
        $monthIncrement = 0.5
    }
} else {
    $monthIncrement = 0.5
}

# 3. File Modification Increment (0.1 per modified/staged/untracked file)
$modifiedFilesCount = 0
try {
    $gitStatus = & git status --porcelain 2>$null
    if ($gitStatus) {
        $modifiedFilesCount = ($gitStatus | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    }
} catch {
    $modifiedFilesCount = 0
}

[double]$filesIncrement = [Math]::Round($modifiedFilesCount * 0.1, 1)

# 4. Final Numerical Version Assembly
$buildNumber++
[double]$calculatedDecimal = [Math]::Round($baseVersion + $monthIncrement + $filesIncrement, 1)
[int]$major = [int][Math]::Floor($calculatedDecimal)
[int]$minor = [int][Math]::Round(($calculatedDecimal - $major) * 10)
if ($minor -ge 10) {
    $major += [int][Math]::Floor($minor / 10)
    $minor = $minor % 10
}

$fullVersionString = "$major.$minor.$buildNumber"
$shortVersionString = "$major.$minor"

Write-Host "======================================================================"
Write-Host " DELTA SYNTH Version Bumper: v$fullVersionString"
Write-Host " - Base Version     : $baseVersion"
Write-Host " - Monthly Increment: +$monthIncrement"
Write-Host " - Modified Files   : $modifiedFilesCount files (+$filesIncrement)"
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
            $node.InnerText = $shortVersionString
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
BaseVersion=$calculatedDecimal
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
