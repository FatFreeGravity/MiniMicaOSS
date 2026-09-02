$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repo 'artifacts'
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ('MiniMicaTemplateTest-' + [Guid]::NewGuid())
$installedPackageId = 'MiniMica.Templates'
$payloadBudgetBytes = 1310720 # 1.25 MiB; release payload excludes symbols.

function Build-Net48Project([string]$project) {
    & msbuild $project /restore /t:Build /p:Configuration=Release /m /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}

function Assert-SmallPayload([string]$projectDirectory, [string]$name) {
    $release = Join-Path $projectDirectory 'bin\Release'
    $files = Get-ChildItem $release -Recurse -File | Where-Object {
        $_.Extension -notin @('.pdb', '.xml')
    }
    $bytes = ($files | Measure-Object Length -Sum).Sum
    if (-not $bytes) { $bytes = 0 }
    $kb = [Math]::Round($bytes / 1KB, 1)
    Write-Host "$name shipping payload: $kb KiB"
    if ($bytes -gt $payloadBudgetBytes) {
        throw "$name exceeds the 1.25 MiB MiniMica payload budget ($bytes bytes)."
    }
}

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
New-Item -ItemType Directory -Force -Path $temp | Out-Null

try {
    $sourceProject = Join-Path $repo 'src\MiniMicaApp\MiniMicaApp.csproj'
    Build-Net48Project $sourceProject
    Assert-SmallPayload (Split-Path $sourceProject -Parent) 'Source template'

    # Runtime PackageReference dependencies defeat the small-payload design goal.
    $runtimePackages = Get-ChildItem (Join-Path $repo 'src\MiniMicaApp') -Recurse -Filter '*.csproj' |
        Select-String -Pattern '<PackageReference\b' -ErrorAction SilentlyContinue
    if ($runtimePackages) { throw 'MiniMica source contains a runtime PackageReference.' }

    & dotnet pack (Join-Path $repo 'templates\MiniMica.Templates\MiniMica.Templates.csproj') -c Release -o $artifacts
    if ($LASTEXITCODE -ne 0) { throw 'Template pack failed.' }

    $package = Get-ChildItem $artifacts -Filter 'MiniMica.Templates.*.nupkg' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $package) { throw 'Template package was not produced.' }

    & dotnet new install $package.FullName --force
    if ($LASTEXITCODE -ne 0) { throw 'Template install failed.' }

    $cases = @(
        @{ Name = 'SmokeDefault'; Args = @() },
        @{ Name = 'SmokeDarkAcrylic'; Args = @('--theme', 'Dark', '--backdrop', 'Acrylic') },
        @{ Name = 'SmokeNoBackdrop'; Args = @('--theme', 'Light', '--backdrop', 'None') }
    )

    foreach ($case in $cases) {
        $out = Join-Path $temp $case.Name
        $newArgs = @('new', 'minimica', '-n', $case.Name, '-o', $out) + $case.Args
        & dotnet @newArgs
        if ($LASTEXITCODE -ne 0) { throw "Template generation failed: $($case.Name)" }

        $staleName = Get-ChildItem $out -Recurse -File |
            Select-String -SimpleMatch 'MiniMicaApp' -ErrorAction SilentlyContinue
        if ($staleName) { throw "Generated project contains stale MiniMicaApp identifier: $($case.Name)" }

        $sentinel = Get-ChildItem $out -Recurse -File |
            Select-String -Pattern 'MINIMICA_(THEME|BACKDROP)' -ErrorAction SilentlyContinue
        if ($sentinel) { throw "Generated project contains an unresolved template sentinel: $($case.Name)" }

        $generatedProject = Join-Path $out "$($case.Name).csproj"
        Build-Net48Project $generatedProject
        Assert-SmallPayload $out $case.Name
    }

    # The base template stays neutral-English and tiny. Separately validate the
    # repository localization tool by generating a 14-language set into a fresh app.
    $localizedOut = Join-Path $temp 'SmokeLocalized'
    & dotnet new minimica -n SmokeLocalized -o $localizedOut
    if ($LASTEXITCODE -ne 0) { throw 'Localized template generation failed.' }

    $localizationTool = Join-Path $repo 'tools\localization\generate-resx.ps1'
    $localizationOut = Join-Path $localizedOut 'Localization'
    & $localizationTool -Tier 14 -OutputDirectory $localizationOut
    if ($LASTEXITCODE -ne 0) { throw 'Localization resource generation failed.' }

    $satelliteSources = @(Get-ChildItem $localizationOut -Filter 'Strings.*.resx' -File)
    if ($satelliteSources.Count -ne 13) {
        throw "Expected 13 non-English resources for tier 14, found $($satelliteSources.Count)."
    }

    Build-Net48Project (Join-Path $localizedOut 'SmokeLocalized.csproj')
    Write-Host 'MiniMica localization smoke test passed.' -ForegroundColor Green

    Write-Host 'MiniMica .NET Framework 4.8 template smoke tests passed.' -ForegroundColor Green
}
finally {
    & dotnet new uninstall $installedPackageId 2>$null | Out-Null
    Remove-Item -Recurse -Force $temp -ErrorAction SilentlyContinue
}
