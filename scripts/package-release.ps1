[CmdletBinding()]
param(
    [Parameter()]
    [string] $SptPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ProjectProperty {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlDocument] $ProjectDocument,

        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $propertyNode = $ProjectDocument.SelectSingleNode(
        "/Project/PropertyGroup/$Name")
    if ($null -eq $propertyNode -or
        [string]::IsNullOrWhiteSpace($propertyNode.InnerText)) {
        throw "Missing required project property: $Name"
    }

    return $propertyNode.InnerText.Trim()
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repositoryRoot "Pause.sln"
$projectPath = Join-Path $repositoryRoot "source\Pause.csproj"
[xml] $projectDocument = Get-Content -LiteralPath $projectPath -Raw

$version = Get-ProjectProperty $projectDocument "Version"
$targetFramework = Get-ProjectProperty $projectDocument "TargetFramework"
$assemblyName = Get-ProjectProperty $projectDocument "AssemblyName"

[string[]] $buildArguments = @(
    "build",
    $solutionPath,
    "--configuration",
    "Release",
    "--nologo",
    "-p:DeployToSpt=false"
)
if (-not [string]::IsNullOrWhiteSpace($SptPath)) {
    $buildArguments += "-p:SptPath=$SptPath"
}

& dotnet @buildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Release build failed with exit code $LASTEXITCODE."
}

$outputDirectory = Join-Path $repositoryRoot (
    "source\bin\Release\$targetFramework")
$assemblyPath = Join-Path $outputDirectory "$assemblyName.dll"
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "Release assembly was not found: $assemblyPath"
}

$packageName = "$($assemblyName.Replace('.', '-'))-$version.zip"
$packagePath = Join-Path $outputDirectory $packageName
$stagingDirectory = Join-Path (
    [IO.Path]::GetTempPath()) (
    "pause-release-$([Guid]::NewGuid().ToString('N'))")
$pluginDirectory = Join-Path $stagingDirectory "BepInEx\plugins"
$temporaryPackagePath = Join-Path $stagingDirectory $packageName

try {
    New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
    Copy-Item -LiteralPath $assemblyPath -Destination $pluginDirectory
    Compress-Archive `
        -LiteralPath (Join-Path $stagingDirectory "BepInEx") `
        -DestinationPath $temporaryPackagePath
    Move-Item `
        -LiteralPath $temporaryPackagePath `
        -Destination $packagePath `
        -Force
}
finally {
    if (Test-Path -LiteralPath $stagingDirectory) {
        Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
    }
}

$sha256 = [Security.Cryptography.SHA256]::Create()
try {
    $packageStream = [IO.File]::OpenRead($packagePath)
    try {
        $hashBytes = $sha256.ComputeHash($packageStream)
    }
    finally {
        $packageStream.Dispose()
    }
}
finally {
    $sha256.Dispose()
}
$packageHash = [BitConverter]::ToString($hashBytes).Replace("-", "")
Write-Output "Package: $packagePath"
Write-Output "SHA256: $packageHash"
