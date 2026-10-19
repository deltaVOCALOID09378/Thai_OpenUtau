# ============================================================================
# Made And Checked By DELTA SYNTH & Gemini AI
# Original by Patiphat Wongyai (Delta)
# Version: 1.3 | Date: 2026-07-28
# Description: เพิ่มหมายเลขแพตช์ของโปรเจกต์และบันทึกผลแบบ UTF-8 อย่างปลอดภัย
# ============================================================================

[CmdletBinding()]
param(
    [string] $ProjectPath = "OpenUtau\OpenUtau.csproj",
    [switch] $Preview
)

$ErrorActionPreference = "Stop"
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
[xml] $projectXml = Get-Content -LiteralPath $resolvedProject -Raw -Encoding UTF8

$versionNodes = @($projectXml.SelectNodes("/Project/PropertyGroup/Version"))
if ($versionNodes.Count -eq 0) {
    throw "The project does not contain a Version element: $resolvedProject"
}

$currentVersion = [Version] $versionNodes[0].InnerText
$build = if ($currentVersion.Build -ge 0) { $currentVersion.Build + 1 } else { 1 }
$newVersion = [Version]::new(
    $currentVersion.Major,
    $currentVersion.Minor,
    $build)

if (-not $Preview) {
    foreach ($node in $versionNodes) {
        $node.InnerText = $newVersion.ToString(3)
    }
    foreach ($nodeName in @("CFBundleVersion", "CFBundleShortVersionString")) {
        foreach ($node in @($projectXml.SelectNodes("/Project/PropertyGroup/$nodeName"))) {
            $node.InnerText = $newVersion.ToString(3)
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

    $versionFile = Join-Path (Split-Path -Parent $resolvedProject) "..\version.txt"
    [System.IO.File]::WriteAllText(
        [System.IO.Path]::GetFullPath($versionFile),
        $newVersion.ToString(3),
        [System.Text.UTF8Encoding]::new($false))
}

$newVersion.ToString(3)
