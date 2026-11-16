[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$runRoot = Join-Path $repoRoot ('artifacts/releases/' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff'))
$feedRoot = Join-Path $runRoot 'packages'
$restoreRoot = Join-Path $runRoot 'restore-packages'
New-Item -ItemType Directory -Path $feedRoot -Force | Out-Null
$targets = @(
    @{ Major = 17; Core = '17.5.0'; Ai = '17.0.0'; Cms = '[17.0.0,18.0.0)'; AiCms = '[17.3.0,18.0.0)'; UmbracoAi = '[1.10.1,2.0.0)'; Backoffice = $null },
    @{ Major = 18; Core = '18.0.0'; Ai = '18.0.0'; Cms = '[18.2.0,19.0.0)'; AiCms = '[18.2.0,19.0.0)'; UmbracoAi = '[18.3.5,19.0.0)'; Backoffice = '18.2.0' }
)
function Invoke-Checked([string] $Command, [string[]] $Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE." }
}
function Read-ZipText($Zip, [string] $Path) {
    $entry = $Zip.GetEntry($Path)
    if (!$entry) { throw "Missing package entry: $Path" }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Assert-Dependency($Dependencies, [string] $Id, [string] $Range) {
    $matching = @($Dependencies | Where-Object id -eq $Id)
    if ($matching.Count -ne 1 -or ($matching[0].version -replace '\s', '') -ne $Range) {
        throw "Incorrect dependency $Id; expected $Range."
    }
}

Push-Location $repoRoot
try {
    Invoke-Checked node @('--test', 'scripts/test-scheduling-ai-context.mjs')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $evidence = @()
    foreach ($target in $targets) {
        $stageRoot = Join-Path $runRoot "stage-v$($target.Major)"
        foreach ($folder in @('Diplo.GodMode', 'Diplo.GodMode.AI', 'Diplo.GodMode.Tests', 'Screenshots')) {
            & robocopy (Join-Path $repoRoot $folder) (Join-Path $stageRoot $folder) /E /XD bin obj node_modules wwwroot /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "Could not stage $folder." }
        }
        foreach ($file in @('Directory.Packages.props', 'global.json')) {
            Copy-Item -LiteralPath (Join-Path $repoRoot $file) -Destination $stageRoot
        }
        # Explicit config avoids CLI source/path normalization and inherited feeds.
        $escapedFeed = [Security.SecurityElement]::Escape($feedRoot)
        "<configuration><packageSources><clear /><add key='release' value='$escapedFeed' /><add key='nuget.org' value='https://api.nuget.org/v3/index.json' /></packageSources></configuration>" |
            Set-Content -LiteralPath (Join-Path $stageRoot 'NuGet.Config')
        $propsPath = Join-Path $stageRoot 'Directory.Packages.props'
        [xml] $props = Get-Content -LiteralPath $propsPath -Raw
        $props.SelectSingleNode('//GodModeCmsVersion').InnerText = $target.Cms
        $props.SelectSingleNode('//GodModeAiCmsVersion').InnerText = $target.AiCms
        $props.SelectSingleNode('//GodModeUmbracoAiVersion').InnerText = $target.UmbracoAi
        $props.Save($propsPath)

        foreach ($package in @('Diplo.GodMode', 'Diplo.GodMode.AI')) {
            $isAi = $package -eq 'Diplo.GodMode.AI'
            $version = if ($isAi) { $target.Ai } else { $target.Core }
            $projectPath = Join-Path $stageRoot "$package/$package.csproj"
            [xml] $project = Get-Content -LiteralPath $projectPath -Raw
            $project.SelectSingleNode('//VersionPrefix').InnerText = $version
            if (!$isAi) { $project.SelectSingleNode('//Description').InnerText = "Diplo God Mode makes Umbraco $($target.Major) developers invincible!" }
            $project.Save($projectPath)
            $client = Join-Path $stageRoot "$package/Client"
            $manifestPath = Join-Path $client 'public/umbraco-package.json'
            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            $manifest.version = $version
            foreach ($extension in $manifest.extensions) {
                if ($extension.js) { $extension.js = ($extension.js -replace '\?v=.*$', '') + "?v=$version" }
            }
            $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath
            Push-Location $client
            try {
                Invoke-Checked npm @('ci', '--no-audit', '--no-fund')
                if ($target.Backoffice) {
                    Invoke-Checked npm @('install', '--no-save', '--package-lock=false', '--no-audit', '--no-fund', "@umbraco-cms/backoffice@$($target.Backoffice)")
                }
                Invoke-Checked npm @('run', 'type-check')
                Invoke-Checked npm @('run', 'build')
            } finally { Pop-Location }
            $readmePath = Join-Path $stageRoot "$package/NuGet.md"
            $readme = Get-Content -LiteralPath $readmePath -Raw
            # Preserve all documentation; make this artifact's target explicit at the top.
            $readme = "**Release $version for Umbraco $($target.Major). Requires CMS $(if ($isAi) { $target.AiCms } else { $target.Cms }) and .NET 10.**`n`n" + $readme
            Set-Content -LiteralPath $readmePath -Value $readme
        }
        # Never reuse a cached same-version local build from another release run.
        $common = @('-p:SkipFrontendBuild=true', '-p:UseSharedCompilation=false', "-p:RestorePackagesPath=$restoreRoot")
        Invoke-Checked dotnet (@('test', '--project', (Join-Path $stageRoot 'Diplo.GodMode.Tests/Diplo.GodMode.Tests.csproj'),
            '--configuration', 'Release', '--report-trx', '--results-directory', (Join-Path $stageRoot 'test-results')) + $common)
        foreach ($package in @('Diplo.GodMode', 'Diplo.GodMode.AI')) {
            $isAi = $package -eq 'Diplo.GodMode.AI'
            $version = if ($isAi) { $target.Ai } else { $target.Core }
            $args = @('pack', (Join-Path $stageRoot "$package/$package.csproj"), '--configuration', 'Release', '--output', $feedRoot) + $common
            $coreRange = "[$($target.Core),$($target.Major + 1).0.0)"
            if ($isAi) {
                # Store ranges in XML: commas in -p values are MSBuild switch separators.
                $projectPath = Join-Path $stageRoot "$package/$package.csproj"
                [xml] $project = Get-Content -LiteralPath $projectPath -Raw
                $coreVersion = $project.CreateElement('GodModeCorePackageVersion')
                $coreVersion.InnerText = $coreRange
                $null = $project.SelectSingleNode('//PropertyGroup').AppendChild($coreVersion)
                $project.Save($projectPath)
            }
            Invoke-Checked dotnet $args
            $packagePath = Join-Path $feedRoot "$package.$version.nupkg"
            $zip = [IO.Compression.ZipFile]::OpenRead($packagePath)
            try {
                $plugin = if ($isAi) { 'DiploGodModeAI' } else { 'DiploGodMode' }
                $assetRoot = "staticwebassets/App_Plugins/$plugin"
                $manifest = Read-ZipText $zip "$assetRoot/umbraco-package.json" | ConvertFrom-Json
                if ($manifest.version -ne $version) { throw "Incorrect $package manifest version." }
                foreach ($extension in $manifest.extensions) {
                    if ($extension.js) {
                        if ($extension.js -notmatch "\?v=$([regex]::Escape($version))$") { throw 'Incorrect entry-point cache version.' }
                        $asset = 'staticwebassets' + ($extension.js -replace '\?.*$', '')
                        if (!$zip.GetEntry($asset)) { throw "Missing entry-point asset: $asset" }
                    }
                }
                $nuspecEntry = @($zip.Entries | Where-Object FullName -like '*.nuspec')[0]
                [xml] $nuspec = Read-ZipText $zip $nuspecEntry.FullName
                if ($nuspec.package.metadata.id -ne $package -or $nuspec.package.metadata.version -ne $version) { throw 'Incorrect NuGet identity.' }
                if (!$zip.GetEntry('lib/net10.0/' + $package + '.dll') -or !$zip.GetEntry('GodModeIcon.png')) { throw 'Missing library/icon.' }
                $readme = Read-ZipText $zip 'NuGet.md'
                if ($readme -notmatch "Release $([regex]::Escape($version)) for Umbraco $($target.Major)") { throw 'Incorrect package readme target.' }
                $dependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']"))
                Assert-Dependency $dependencies 'Umbraco.Cms.Core' $(if ($isAi) { $target.AiCms } else { $target.Cms })
                foreach ($dependency in $dependencies) {
                    if ($dependency.id -like 'Umbraco.Cms.*') {
                        Assert-Dependency $dependencies $dependency.id $(if ($isAi) { $target.AiCms } else { $target.Cms })
                    }
                }
                if ($isAi) {
                    Assert-Dependency $dependencies 'Diplo.GodMode' $coreRange
                    Assert-Dependency $dependencies 'Umbraco.AI' $target.UmbracoAi
                } elseif (@($dependencies | Where-Object id -eq 'Diplo.GodMode.AI').Count) { throw 'Core must not depend on AI.' }
            } finally { $zip.Dispose() }
            $evidence += @{ package = $package; version = $version; cms = $target.Major; sha256 = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash }
        }
    }
    $commit = & git rev-parse HEAD
    @{ sourceCommit = $commit; sourceDirty = [bool](& git status --porcelain); builtAtUtc = [DateTime]::UtcNow.ToString('o'); packages = $evidence } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $runRoot 'release-manifest.json')
    $evidence | ForEach-Object { "$($_.sha256)  $($_.package).$($_.version).nupkg" } |
        Set-Content -LiteralPath (Join-Path $feedRoot 'SHA256SUMS.txt')
    Write-Host "Verified four release packages: $feedRoot"
} finally { Pop-Location }
