#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PackageFeed
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$packageFeedPath = [System.IO.Path]::GetFullPath($PackageFeed, $repositoryRoot)
$projectProperties = [xml][System.IO.File]::ReadAllText((Join-Path $repositoryRoot 'Directory.Build.props'))
$packageVersion = [string] $projectProperties.Project.PropertyGroup.VersionPrefix
$packagePath = Join-Path $packageFeedPath "SourceGauge.Tool.$packageVersion.nupkg"
$manifestPath = Join-Path $repositoryRoot '.config/dotnet-tools.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

if ($manifest.tools.'sourcegauge.tool'.version -ne $packageVersion) {
    throw "Tool manifest version does not match VersionPrefix '$packageVersion'."
}

if (-not (Test-Path $packagePath -PathType Leaf)) {
    throw "Expected package was not found: $packagePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entries = @($archive.Entries | ForEach-Object FullName)
    foreach ($requiredEntry in @(
        'README.md',
        'tools/net10.0/any/SourceGauge.Cli.dll',
        'tools/net10.0/any/SourceGauge.CSharp.dll',
        'tools/net10.0/any/SourceGauge.Core.dll',
        'tools/net10.0/any/SourceGauge.Cli.runtimeconfig.json')) {
        if ($requiredEntry -notin $entries) {
            throw "Tool package is missing '$requiredEntry'."
        }
    }

    $nuspecEntry = $archive.GetEntry('SourceGauge.Tool.nuspec')
    if ($null -eq $nuspecEntry) {
        throw 'Tool package is missing its nuspec metadata.'
    }

    $nuspecReader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try {
        $nuspec = [xml] $nuspecReader.ReadToEnd()
    }
    finally {
        $nuspecReader.Dispose()
    }

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($nuspec.NameTable)
    $namespaceManager.AddNamespace('n', 'http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd')
    $metadata = $nuspec.SelectSingleNode('/n:package/n:metadata', $namespaceManager)
    $toolType = $nuspec.SelectSingleNode('/n:package/n:metadata/n:packageTypes/n:packageType[@name="DotnetTool"]', $namespaceManager)
    if (($metadata.id -ne 'SourceGauge.Tool') -or ($metadata.version -ne $packageVersion) -or ($metadata.readme -ne 'README.md') -or ($null -eq $toolType)) {
        throw 'Tool package nuspec metadata does not match the expected ID, version, README, and DotnetTool package type.'
    }

    if ($entries -match '(^|/)Microsoft\.Build\.Framework\.dll$') {
        throw 'Tool package must not include Microsoft.Build.Framework.dll from the installed SDK.'
    }
}
finally {
    $archive.Dispose()
}

$temporaryDirectory = Join-Path $repositoryRoot "artifacts/tool-smoke.$([guid]::NewGuid().ToString('N'))"
$temporaryManifestDirectory = Join-Path $temporaryDirectory '.config'
New-Item -ItemType Directory -Path $temporaryManifestDirectory | Out-Null
$temporaryManifestPath = Join-Path $temporaryManifestDirectory 'dotnet-tools.json'
Copy-Item $manifestPath $temporaryManifestPath
$fixtureProject = Join-Path $repositoryRoot 'tests/SourceGauge.Cli.Tests/Fixtures/FixtureProject.csproj'

Push-Location $temporaryDirectory
try {
    & dotnet tool restore --add-source $packageFeedPath --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) {
        throw 'Local tool restore failed.'
    }

    $versionOutput = & dotnet tool run sourcegauge --version
    if ($LASTEXITCODE -ne 0 -or ($versionOutput -join "`n").Trim() -ne $packageVersion) {
        throw "The installed tool did not report package version '$packageVersion'."
    }

    $jsonLines = & dotnet tool run sourcegauge -- $fixtureProject
    if ($LASTEXITCODE -ne 0) {
        throw 'The installed tool failed to analyze the fixture project.'
    }

    $result = ($jsonLines -join [System.Environment]::NewLine) | ConvertFrom-Json
    if ($result.schemaVersion -ne '1' -or @($result.metrics | Where-Object id -eq 'sourcegauge.fanin').Count -eq 0) {
        throw 'The installed tool did not emit the expected versioned FanIn JSON result.'
    }

    & dotnet tool uninstall SourceGauge.Tool
    if ($LASTEXITCODE -ne 0) {
        throw 'Local tool uninstall failed.'
    }

    $remainingManifest = Get-Content $temporaryManifestPath -Raw | ConvertFrom-Json
    if ($remainingManifest.tools.PSObject.Properties.Name -contains 'sourcegauge.tool') {
        throw 'Local tool uninstall did not remove the package from the temporary manifest.'
    }
}
finally {
    Pop-Location
    [System.IO.File]::Delete($temporaryManifestPath)
    [System.IO.Directory]::Delete($temporaryManifestDirectory)
    [System.IO.Directory]::Delete($temporaryDirectory)
}

Write-Host "Tool package $packageVersion passed install, version, analysis, and uninstall smoke tests."
