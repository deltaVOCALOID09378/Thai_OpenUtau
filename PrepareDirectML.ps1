# Made And Checked By DELTA SYNTH & Gemini AI
# Original by Patiphat Wongyai (Delta)

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("x64", "x86", "arm64")]
    [string] $Architecture,

    [Parameter(Mandatory)]
    [string] $Destination,

    [ValidatePattern("^\d+\.\d+\.\d+$")]
    [string] $Version = "1.15.4"
)

$ErrorActionPreference = "Stop"
$destinationPath = [System.IO.Path]::GetFullPath($Destination)
if (-not (Test-Path -LiteralPath $destinationPath -PathType Container)) {
    throw "The DirectML destination does not exist: $destinationPath"
}

$relativePackagePath = ".nuget\packages\microsoft.ai.directml\$Version\bin\$Architecture-win\DirectML.dll"
$userProfile = [System.Environment]::GetFolderPath(
    [System.Environment+SpecialFolder]::UserProfile)
$cachedDirectML = Join-Path $userProfile $relativePackagePath
$targetDirectML = Join-Path $destinationPath "DirectML.dll"

if (Test-Path -LiteralPath $cachedDirectML -PathType Leaf) {
    Copy-Item -LiteralPath $cachedDirectML -Destination $targetDirectML -Force
} else {
    $temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
        "openutau-directml-" + [System.Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    try {
        $archivePath = Join-Path $temporaryRoot "Microsoft.AI.DirectML.nupkg"
        $extractPath = Join-Path $temporaryRoot "package"
        $packageUrl = "https://www.nuget.org/api/v2/package/Microsoft.AI.DirectML/$Version"
        Invoke-WebRequest -Uri $packageUrl -OutFile $archivePath
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $extractPath)

        $downloadedDirectML = Join-Path $extractPath "bin\$Architecture-win\DirectML.dll"
        if (-not (Test-Path -LiteralPath $downloadedDirectML -PathType Leaf)) {
            throw "Microsoft.AI.DirectML $Version does not contain bin\$Architecture-win\DirectML.dll."
        }
        Copy-Item -LiteralPath $downloadedDirectML -Destination $targetDirectML -Force
    } finally {
        if (Test-Path -LiteralPath $temporaryRoot) {
            Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
        }
    }
}

$targetInfo = Get-Item -LiteralPath $targetDirectML
if ($targetInfo.Length -lt 1MB) {
    throw "DirectML.dll is unexpectedly small ($($targetInfo.Length) bytes): $targetDirectML"
}

Write-Output "DirectML $Version ($Architecture) prepared at $targetDirectML"
