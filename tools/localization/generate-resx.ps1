[CmdletBinding()]
param(
    [string]$Worksheet = (Join-Path $PSScriptRoot 'worksheet.csv'),
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,
    [ValidateSet('14', '22', '25', 'All')]
    [string]$Tier = '14',
    [string]$BaseName = 'Strings',
    [switch]$IncludeNeutral
)

$ErrorActionPreference = 'Stop'

# Historical MiniMica deployment tiers. They are convenience sets, not a claim
# that every application has the same audience or localization requirements.
$tiers = @{
    '14' = @('en-US','de-DE','fr-FR','es-MX','es-ES','pt-BR','pt-PT','zh-TW','zh-CN','it-IT','ru-RU','uk-UA','nl-NL','pl-PL')
    '22' = @('en-US','de-DE','fr-FR','es-MX','es-ES','pt-BR','pt-PT','zh-TW','zh-CN','it-IT','ru-RU','uk-UA','nl-NL','pl-PL','sv-SE','da-DK','nb-NO','fi-FI','ja-JP','ko-KR','cs-CZ','tr-TR')
    '25' = @('en-US','de-DE','fr-FR','es-MX','es-ES','pt-BR','pt-PT','zh-TW','zh-CN','it-IT','ru-RU','uk-UA','nl-NL','pl-PL','sv-SE','da-DK','nb-NO','fi-FI','ja-JP','ko-KR','cs-CZ','tr-TR','id-ID','th-TH','vi-VN')
}

function New-ResxDocument {
    $doc = New-Object System.Xml.XmlDocument
    $decl = $doc.CreateXmlDeclaration('1.0', 'utf-8', $null)
    [void]$doc.AppendChild($decl)
    $root = $doc.CreateElement('root')
    [void]$doc.AppendChild($root)

    $headers = [ordered]@{
        'resmimetype' = 'text/microsoft-resx'
        'version' = '2.0'
        'reader' = 'System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
        'writer' = 'System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
    }

    foreach ($pair in $headers.GetEnumerator()) {
        $resheader = $doc.CreateElement('resheader')
        $resheader.SetAttribute('name', $pair.Key)
        $value = $doc.CreateElement('value')
        $value.InnerText = $pair.Value
        [void]$resheader.AppendChild($value)
        [void]$root.AppendChild($resheader)
    }

    return $doc
}

function Get-Placeholders([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return @() }
    return @([regex]::Matches($text, '\{[^{}]+\}') | ForEach-Object { $_.Value } | Sort-Object -Unique)
}

if (-not (Test-Path $Worksheet)) { throw "Worksheet not found: $Worksheet" }
$rows = @(Import-Csv -LiteralPath $Worksheet)
if ($rows.Count -eq 0) { throw 'Worksheet contains no resource rows.' }

$headers = @($rows[0].PSObject.Properties.Name)
if ('ResourceID' -notin $headers -or 'en-US' -notin $headers) {
    throw 'Worksheet must contain ResourceID and en-US columns.'
}

$cultures = if ($Tier -eq 'All') {
    @($headers | Where-Object { $_ -ne 'ResourceID' })
} else {
    @($tiers[$Tier])
}

foreach ($culture in $cultures) {
    if ($culture -notin $headers) { throw "Worksheet is missing culture column: $culture" }
}

$duplicates = $rows | Group-Object ResourceID | Where-Object { $_.Count -gt 1 }
if ($duplicates) { throw "Duplicate ResourceID(s): $($duplicates.Name -join ', ')" }

foreach ($row in $rows) {
    if ([string]::IsNullOrWhiteSpace($row.ResourceID)) { throw 'ResourceID cannot be empty.' }
    if ($row.ResourceID -notmatch '^[A-Za-z_][A-Za-z0-9_.-]*$') { throw "Invalid ResourceID: $($row.ResourceID)" }
    if ([string]::IsNullOrWhiteSpace($row.'en-US')) { throw "English value is required: $($row.ResourceID)" }

    $expected = @(Get-Placeholders $row.'en-US')
    foreach ($culture in $cultures) {
        $translated = [string]$row.$culture
        if ([string]::IsNullOrWhiteSpace($translated)) { continue }
        $actual = @(Get-Placeholders $translated)
        if (($expected -join '|') -ne ($actual -join '|')) {
            throw "Placeholder mismatch for '$($row.ResourceID)' in $culture. Expected [$($expected -join ', ')], found [$($actual -join ', ')]."
        }
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

foreach ($culture in $cultures) {
    if ($culture -eq 'en-US' -and -not $IncludeNeutral) { continue }

    $fileName = if ($culture -eq 'en-US') { "$BaseName.resx" } else { "$BaseName.$culture.resx" }
    $path = Join-Path $OutputDirectory $fileName
    $doc = New-ResxDocument
    $root = $doc.DocumentElement

    foreach ($row in $rows) {
        $translated = [string]$row.$culture

        # Omit missing translations from satellite resources. ResourceManager then
        # falls back to a parent culture / neutral English instead of returning an
        # intentionally empty string.
        if ([string]::IsNullOrWhiteSpace($translated)) { continue }

        $data = $doc.CreateElement('data')
        $data.SetAttribute('name', [string]$row.ResourceID)
        $data.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
        $value = $doc.CreateElement('value')
        $value.InnerText = $translated
        [void]$data.AppendChild($value)
        [void]$root.AppendChild($data)
    }

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $writer = [System.Xml.XmlWriter]::Create($path, $settings)
    try { $doc.Save($writer) } finally { $writer.Dispose() }
    Write-Host "Generated $path"
}

Write-Host "Localization generation complete: tier $Tier" -ForegroundColor Green
