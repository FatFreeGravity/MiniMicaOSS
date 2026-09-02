<#
.SYNOPSIS
    First-build triage script for MiniMica v5.0.

.DESCRIPTION
    Builds the solution Debug + Release, captures full MSBuild output to log files,
    and prints a compact summary (errors, warnings, Release payload size).

    The MiniMica v5 source has never been compiled on Windows. Run this first and
    send back build-report.txt - it contains everything needed to triage failures
    without pasting a full MSBuild log.

.EXAMPLE
    .\scripts\build-and-report.ps1
    .\scripts\build-and-report.ps1 -Configuration Release
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release','Both')]
    [string]$Configuration = 'Both',
    [double]$SizeBudgetMiB = 1.25
)

$ErrorActionPreference = 'Continue'
$repo    = Split-Path -Parent $PSScriptRoot
$logDir  = Join-Path $repo 'artifacts'
$report  = Join-Path $logDir 'build-report.txt'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Find-MSBuild {
    $cmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $p = & $vswhere -latest -requires Microsoft.Component.MSBuild `
                        -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($p) { return $p }
    }
    return $null
}

$msbuild = Find-MSBuild
$lines = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { Write-Host $s; $lines.Add($s) | Out-Null }

Emit "MiniMica v5.0 build report"
Emit ("Generated : {0}" -f (Get-Date -Format 'u'))
Emit ("Machine   : {0}  |  OS build {1}" -f $env:COMPUTERNAME, [Environment]::OSVersion.Version)
Emit ("MSBuild   : {0}" -f ($(if ($msbuild) { $msbuild } else { 'NOT FOUND' })))
Emit ('-' * 70)

if (-not $msbuild) {
    Emit 'ERROR: MSBuild not found. Open "Developer PowerShell for VS 2022" and re-run,'
    Emit '       or install the .NET Framework 4.8 targeting pack + MSBuild.'
    $lines -join "`r`n" | Set-Content -Path $report -Encoding UTF8
    exit 1
}

$configs = if ($Configuration -eq 'Both') { @('Debug','Release') } else { @($Configuration) }
$failed = $false

foreach ($cfg in $configs) {
    $log = Join-Path $logDir "msbuild-$cfg.log"
    Emit ''
    Emit "### $cfg"
    & $msbuild (Join-Path $repo 'MiniMica.slnx') /restore /p:Configuration=$cfg `
        /v:normal /nologo /fl "/flp:LogFile=$log;Verbosity=normal" 2>&1 | Out-Null
    $code = $LASTEXITCODE

    $errs = @(); $warns = @()
    if (Test-Path $log) {
        $content = Get-Content $log
        $errs  = @($content | Select-String -Pattern '\berror\s+[A-Z]{2,}\d+' | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
        $warns = @($content | Select-String -Pattern '\bwarning\s+[A-Z]{2,}\d+' | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
    }

    Emit ("exit code : {0}" -f $code)
    Emit ("errors    : {0}" -f $errs.Count)
    Emit ("warnings  : {0}" -f $warns.Count)
    Emit ("full log  : {0}" -f $log)

    if ($errs.Count) {
        $failed = $true
        Emit ''
        Emit 'FIRST 40 ERRORS'
        $errs | Select-Object -First 40 | ForEach-Object { Emit "  $_" }
    }
    if ($warns.Count) {
        Emit ''
        Emit 'FIRST 20 WARNINGS'
        $warns | Select-Object -First 20 | ForEach-Object { Emit "  $_" }
    }

    if ($cfg -eq 'Release' -and $code -eq 0) {
        $bin = Join-Path $repo 'src\MiniMicaApp\bin\Release'
        if (Test-Path $bin) {
            $payload = Get-ChildItem $bin -Recurse -File |
                       Where-Object { $_.Extension -notin '.pdb','.xml' }
            $mib = [math]::Round((($payload | Measure-Object Length -Sum).Sum / 1MB), 3)
            Emit ''
            Emit ("Release payload : {0} MiB across {1} files (budget {2} MiB)" -f $mib, $payload.Count, $SizeBudgetMiB)
            if ($mib -gt $SizeBudgetMiB) { Emit '  OVER BUDGET'; $failed = $true } else { Emit '  within budget' }
            $payload | Sort-Object Length -Descending | Select-Object -First 10 |
                ForEach-Object { Emit ("   {0,10:N0}  {1}" -f $_.Length, $_.Name) }
        }
    }
}

Emit ''
Emit ('-' * 70)
Emit ($(if ($failed) { 'RESULT: FAILED - send artifacts\build-report.txt back for triage' }
        else         { 'RESULT: BUILD OK - proceed to the runtime checklist (docs/v5-test-protocol.md)' }))

$lines -join "`r`n" | Set-Content -Path $report -Encoding UTF8
Write-Host ''
Write-Host "Report written to $report" -ForegroundColor Cyan
exit $(if ($failed) { 1 } else { 0 })
