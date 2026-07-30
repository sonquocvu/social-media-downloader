[CmdletBinding()]
param(
    [string]$InnoSetupCompilerPath,
    [switch]$NoRestore,
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-InnoSetupVersion {
    param([Parameter(Mandatory)][string]$CompilerPath)

    $versionText = (Get-Item -LiteralPath $CompilerPath).VersionInfo.ProductVersion
    if ($versionText -eq "0.0.0.0") {
        $versionSource = Join-Path (Split-Path -Parent $CompilerPath) "unins000.exe"
        if (Test-Path -LiteralPath $versionSource -PathType Leaf) {
            $versionText = (Get-Item -LiteralPath $versionSource).VersionInfo.ProductVersion
        }
    }

    $versionMatch = [regex]::Match($versionText, '\d+\.\d+\.\d+')
    if (-not $versionMatch.Success) {
        return $null
    }

    return [version]$versionMatch.Value
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$versionPropsPath = Join-Path $repositoryRoot "Directory.Build.props"
$applicationProject = Join-Path $repositoryRoot "src\SVVideoDownloader.App\SVVideoDownloader.App.csproj"
$publishDirectory = Join-Path $repositoryRoot "artifacts\publish\win-x64"
$publishedExecutable = Join-Path $publishDirectory "SVVideoDownloader.App.exe"
$installerDirectory = Join-Path $repositoryRoot "artifacts\installer"
$innoSource = Join-Path $PSScriptRoot "SVVideoDownloader.iss"

[xml]$versionProps = Get-Content -LiteralPath $versionPropsPath -Raw -Encoding UTF8
$versionNode = $versionProps.SelectSingleNode("/Project/PropertyGroup/VersionPrefix")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "VersionPrefix was not found in Directory.Build.props."
}

$productVersion = $versionNode.InnerText.Trim()
if ($productVersion -notmatch '^\d+\.\d+\.\d+$') {
    throw "VersionPrefix must use the major.minor.patch format."
}

$versionParts = $productVersion.Split(".")

if ([string]::IsNullOrWhiteSpace($InnoSetupCompilerPath)) {
    $innoSetupCandidates = @()
    $isccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($null -ne $isccCommand) {
        $innoSetupCandidates += $isccCommand.Source
    }

    $innoSetupCandidates += @(
        (Join-Path $env:ProgramFiles "Inno Setup 7\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 7\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe")
    )

    $InnoSetupCompilerPath = $innoSetupCandidates |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Where-Object {
            $candidateVersion = Get-InnoSetupVersion -CompilerPath $_
            $null -ne $candidateVersion -and $candidateVersion -eq [version]"7.0.2"
        } |
        Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($InnoSetupCompilerPath) -or
    -not (Test-Path -LiteralPath $InnoSetupCompilerPath -PathType Leaf)) {
    throw "Inno Setup 7 compiler was not found. Install Inno Setup 7.0.2 x64 or pass -InnoSetupCompilerPath."
}

$compilerVersion = Get-InnoSetupVersion -CompilerPath $InnoSetupCompilerPath
if ($null -eq $compilerVersion -or $compilerVersion -ne [version]"7.0.2") {
    throw "Inno Setup 7.0.2 is required. Found: $compilerVersion"
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

New-Item -ItemType Directory -Path $installerDirectory -Force | Out-Null

$setupPath = Join-Path $installerDirectory "SVVideoDownloader-$productVersion-win-x64-setup.exe"
$compilerArguments = @(
    "/Qp",
    "/DProductVersion=$productVersion",
    "/DVersionMajor=$($versionParts[0])",
    "/DVersionMinor=$($versionParts[1])",
    "/DVersionPatch=$($versionParts[2])",
    "/DRepositoryRoot=$repositoryRoot",
    "/DPublishDir=$publishDirectory",
    "/DOutputDir=$installerDirectory",
    $innoSource
)

& $InnoSetupCompilerPath @compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "The Inno Setup compilation step failed."
}

if (-not (Test-Path -LiteralPath $setupPath -PathType Leaf)) {
    throw "The installer was not created at: $setupPath"
}

$setupFile = Get-Item -LiteralPath $setupPath
$hash = Get-FileHash -LiteralPath $setupPath -Algorithm SHA256
$checksumPath = "$setupPath.sha256"
$checksumLine = "$($hash.Hash)  $($setupFile.Name)$([Environment]::NewLine)"
[System.IO.File]::WriteAllText(
    $checksumPath,
    $checksumLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Installer created: $($setupFile.FullName)"
Write-Host "Version: $productVersion"
Write-Host "Size: $($setupFile.Length) bytes"
Write-Host "SHA-256: $($hash.Hash)"
Write-Host "Checksum file: $checksumPath"
