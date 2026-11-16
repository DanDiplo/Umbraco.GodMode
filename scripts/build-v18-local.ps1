[CmdletBinding()]
param(
    [string] $CmsVersion = '18.2.0',
    [string] $UmbracoAiVersion = '18.3.5'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$version = '18.0.0-local.' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff')
$stageRoot = Join-Path $repoRoot "artifacts/v18-build/$version"
$feedRoot = Join-Path $repoRoot 'artifacts/v18-feed'

function Invoke-Checked([string] $Command, [string[]] $Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE." }
}

# Use the CMS 18 line selected by the existing host; a new minimum needs acceptance.
if ($CmsVersion -notmatch '^18\.\d+\.\d+$' -or $UmbracoAiVersion -notmatch '^18\.\d+\.\d+$') {
    throw 'Specify stable CMS and Umbraco.AI 18 versions.'
}
New-Item -ItemType Directory -Path $stageRoot, $feedRoot -Force | Out-Null
foreach ($folder in @('Diplo.GodMode', 'Diplo.GodMode.AI', 'Diplo.GodMode.Tests', 'Screenshots')) {
    & robocopy (Join-Path $repoRoot $folder) (Join-Path $stageRoot $folder) /E /XD bin obj node_modules wwwroot /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Could not stage $folder." }
}
foreach ($file in @('Directory.Packages.props', 'global.json')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $file) -Destination $stageRoot
}

# Change only the staged dependency graph. Normal project builds stay on v17.
$propsPath = Join-Path $stageRoot 'Directory.Packages.props'
[xml] $props = Get-Content -LiteralPath $propsPath -Raw
$range = "[$CmsVersion,19.0.0)"
$props.SelectSingleNode('//GodModeCmsVersion').InnerText = $range
$props.SelectSingleNode('//GodModeAiCmsVersion').InnerText = $range
$props.SelectSingleNode('//GodModeUmbracoAiVersion').InnerText = $UmbracoAiVersion
$props.Save($propsPath)

foreach ($package in @('Diplo.GodMode', 'Diplo.GodMode.AI')) {
    $client = Join-Path $stageRoot "$package/Client"
    Push-Location $client
    try {
        Invoke-Checked npm @('ci', '--no-audit', '--no-fund')
        # Preserve tooling from the checked-in lock; change only backoffice in staging.
        Invoke-Checked npm @('install', '--no-save', '--package-lock=false', '--no-audit', '--no-fund', "@umbraco-cms/backoffice@$CmsVersion")
        $manifestPath = Join-Path $client 'public/umbraco-package.json'
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $manifest.version = $version
        foreach ($extension in $manifest.extensions) {
            if ($extension.js) { $extension.js = $extension.js -replace '\?v=.*$', "?v=$version" }
        }
        $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath
        Invoke-Checked npm @('run', 'type-check')
        Invoke-Checked npm @('run', 'build')
    } finally { Pop-Location }

    $readmePath = Join-Path $stageRoot "$package/NuGet.md"
    # Local test artifacts have explicit requirements; v17 docs remain unchanged.
    Set-Content -LiteralPath $readmePath -Value "# $package - local Umbraco 18 test build`n`nVersion: $version. Requires CMS $range and .NET 10. The AI companion requires Umbraco.AI $UmbracoAiVersion and the matching local core package. Runtime acceptance is in progress. Not for publication."
}

$common = @('-p:SkipFrontendBuild=true', '-p:UseSharedCompilation=false', "-p:Version=$version", "-p:PackageVersion=$version")
$testProject = Join-Path $stageRoot 'Diplo.GodMode.Tests/Diplo.GodMode.Tests.csproj'
Invoke-Checked dotnet (@('test', '--project', $testProject, '--configuration', 'Release', '--report-trx', '--results-directory', (Join-Path $stageRoot 'test-results')) + $common)

foreach ($package in @('Diplo.GodMode', 'Diplo.GodMode.AI')) {
    $projectPath = Join-Path $stageRoot "$package/$package.csproj"
    Invoke-Checked dotnet (@('pack', $projectPath, '--configuration', 'Release', '--output', $feedRoot,
        '-p:Description=Local Umbraco 18 compatibility test build.',
        '-p:PackageReleaseNotes=Local compatibility test artifact. Not for publication.') + $common)
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$hashes = @{}
foreach ($package in @('Diplo.GodMode', 'Diplo.GodMode.AI')) {
    $packagePath = Join-Path $feedRoot "$package.$version.nupkg"
    $zip = [IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $plugin = if ($package -eq 'Diplo.GodMode') { 'DiploGodMode' } else { 'DiploGodModeAI' }
        foreach ($asset in @('umbraco-package.json', 'index.js')) {
            if (!$zip.GetEntry("staticwebassets/App_Plugins/$plugin/$asset")) { throw "Missing $package asset: $asset" }
        }
        $reader = [IO.StreamReader]::new($zip.GetEntry("staticwebassets/App_Plugins/$plugin/umbraco-package.json").Open())
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($manifest.version -ne $version) { throw "Incorrect $package manifest version." }
        $entry = @($zip.Entries | Where-Object FullName -like '*.nuspec')[0]
        $reader = [IO.StreamReader]::new($entry.Open())
        try { [xml] $nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $dependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']"))
        if (!@($dependencies | Where-Object id -eq 'Umbraco.Cms.Core').Count) { throw "Missing CMS dependency in $package." }
        foreach ($dependency in $dependencies) {
            if ($dependency.id -like 'Umbraco.Cms.*' -and ($dependency.version -replace '\s', '') -ne $range) {
                throw "Unexpected CMS dependency: $($dependency.id) $($dependency.version)"
            }
            if ($dependency.id -eq 'Diplo.GodMode' -and $dependency.version -notlike "*$version*") {
                throw 'The AI package does not depend on the matching core build.'
            }
        }
        if ($package -eq 'Diplo.GodMode.AI') {
            if (!@($dependencies | Where-Object id -eq 'Diplo.GodMode').Count) { throw 'Missing core dependency in AI package.' }
            $aiDependency = @($dependencies | Where-Object id -eq 'Umbraco.AI')[0]
            if (!$aiDependency -or $aiDependency.version -notlike "*$UmbracoAiVersion*") { throw 'Incorrect Umbraco.AI dependency.' }
        }
    } finally { $zip.Dispose() }
    $hashes[$package] = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
}

# Select only packages whose build, tests and asset/dependency inspection succeeded.
$selectionPath = Join-Path $repoRoot 'Diplo.GodMode.Testsite18/GodModePackage.local.props'
@"
<Project>
  <PropertyGroup>
    <UsePackedGodModePackage>true</UsePackedGodModePackage>
    <GodModePackageVersion>$version</GodModePackageVersion>
    <GodModeAiPackageVersion>$version</GodModeAiPackageVersion>
  </PropertyGroup>
</Project>
"@ | Set-Content -LiteralPath $selectionPath
@{ version = $version; cms = $CmsVersion; umbracoAi = $UmbracoAiVersion; sha256 = $hashes } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $stageRoot 'build-manifest.json')
Write-Host "Selected $version for the v18 test host. Restore/build and restart it to load these packages."
Write-Host "Staging and test evidence: $stageRoot"
