# Localization tooling

MiniMica uses ordinary .NET Framework `.resx` resources. The runtime starter has no localization package dependency.

The translation worksheet in this folder is derived from the original MiniMica settings-localization workflow. It contains 25 culture columns and reusable translations for common settings terms. `{ProductName}` and `{M.m.build}` are placeholders and must remain unchanged in every language.

## Generate satellite resources

From the repository root:

```powershell
.\tools\localization\generate-resx.ps1 `
    -Tier 14 `
    -OutputDirectory .\src\MiniMicaApp\Localization
```

Available historical convenience tiers are `14`, `22`, and `25`. `All` emits every culture column present in the worksheet.

The generator intentionally does **not** overwrite neutral `Strings.resx` unless `-IncludeNeutral` is specified. Missing translated cells are omitted from satellite resources so .NET ResourceManager can fall back to neutral English.

## Editing translations

Recommended workflow:

1. Add the neutral English key/value to `src/MiniMicaApp/Localization/Strings.resx`.
2. Add the same `ResourceID` and English text to `worksheet.csv`.
3. Translate in a spreadsheet or use ResX Resource Manager to export/import Excel.
4. Run the generator for only the cultures your product ships.
5. Build and test at least one exact regional culture (for example `es-ES`) and one fallback scenario.

The original private workflow used a custom CsvHelper console utility. This public version uses PowerShell's built-in CSV/XML support instead: no hard-coded machine paths and no developer-tool NuGet dependency.
