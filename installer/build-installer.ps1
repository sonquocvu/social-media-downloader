[CmdletBinding()]
param(
    [string]$WixBinPath = "C:\Program Files (x86)\WiX Toolset v3.11\bin",
    [switch]$NoRestore,
    [switch]$SkipMsiValidation,
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$versionPropsPath = Join-Path $repositoryRoot "Directory.Build.props"
$applicationProject = Join-Path $repositoryRoot "src\SVVideoDownloader.App\SVVideoDownloader.App.csproj"
$publishDirectory = Join-Path $repositoryRoot "artifacts\publish\win-x64"
$publishedExecutable = Join-Path $publishDirectory "SVVideoDownloader.App.exe"
$installerDirectory = Join-Path $repositoryRoot "artifacts\installer"
$intermediateDirectory = Join-Path $installerDirectory "obj"
$wixSource = Join-Path $PSScriptRoot "Product.wxs"
$candlePath = Join-Path $WixBinPath "candle.exe"
$lightPath = Join-Path $WixBinPath "light.exe"

[xml]$versionProps = Get-Content -LiteralPath $versionPropsPath -Raw -Encoding UTF8
$versionNode = $versionProps.SelectSingleNode("/Project/PropertyGroup/VersionPrefix")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "VersionPrefix was not found in Directory.Build.props."
}

$productVersion = $versionNode.InnerText.Trim()
if ($productVersion -notmatch '^\d+\.\d+\.\d+$') {
    throw "VersionPrefix must use the major.minor.patch format required by MSI."
}

foreach ($toolPath in @($candlePath, $lightPath)) {
    if (-not (Test-Path -LiteralPath $toolPath -PathType Leaf)) {
        throw "WiX Toolset was not found at: $toolPath"
    }
}

$localDotnet = Join-Path $repositoryRoot ".dotnet\dotnet.exe"
if (Test-Path -LiteralPath $localDotnet -PathType Leaf) {
    $dotnetPath = $localDotnet
}
else {
    $dotnetCommand = Get-Command dotnet -ErrorAction Stop
    $dotnetPath = $dotnetCommand.Source
}

if (-not $SkipPublish) {
    $publishArguments = @(
        "publish",
        $applicationProject,
        "--configuration",
        "Release",
        "-p:PublishProfile=win-x64"
    )
    if ($NoRestore) {
        $publishArguments += "--no-restore"
    }

    & $dotnetPath @publishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "The win-x64 publish step failed."
    }
}

if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
    throw "The published executable was not found at: $publishedExecutable"
}

New-Item -ItemType Directory -Path $intermediateDirectory -Force | Out-Null

$wixObject = Join-Path $intermediateDirectory "Product.wixobj"
$msiPath = Join-Path $installerDirectory "SVVideoDownloader-$productVersion-win-x64.msi"

& $candlePath -nologo -arch x64 "-dRepositoryRoot=$repositoryRoot" "-dPublishDir=$publishDirectory" "-dProductVersion=$productVersion" -out $wixObject $wixSource
if ($LASTEXITCODE -ne 0) {
    throw "The WiX source compilation step failed."
}

$lightArguments = @("-nologo", "-spdb", "-out", $msiPath, $wixObject)
if ($SkipMsiValidation) {
    $lightArguments = @("-sval") + $lightArguments
}

& $lightPath @lightArguments
if ($LASTEXITCODE -ne 0) {
    throw "The MSI link step failed."
}

$msiFile = Get-Item -LiteralPath $msiPath
$hash = Get-FileHash -LiteralPath $msiPath -Algorithm SHA256

Write-Host "Installer created: $($msiFile.FullName)"
Write-Host "Version: $productVersion"
Write-Host "Size: $($msiFile.Length) bytes"
Write-Host "SHA-256: $($hash.Hash)"
