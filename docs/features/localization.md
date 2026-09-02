# Localization (feature overview)

The full procedure lives in **[../localization.md](../localization.md)**, that is
the document to follow.

Quick orientation:

| Piece | File |
|---|---|
| Catalog (single source of truth) | `tools/localization/worksheet.csv` |
| Generators | `tools/localization/generate_resx.py`, `generate-resx.ps1` |
| Neutral English resources | `src/MiniMicaApp/Localization/Strings.resx` |
| Committed satellites | `Strings.<culture>.resx`, tier 14 by default |
| Lookup | `Strings.Get` / `Strings.Expand` |
| XAML markup | `{i18n:Loc Key=…}` |
| Culture + metrics at startup | `LocalizationManager` |

Design notes:

- **Neutral English always ships; cultures are opt-in**, so a single-language app
  pays nothing. ([ADR 0004](../decisions/0004-localization-resx-satellites.md))
- **Blank cells are omitted, not written empty**, so `ResourceManager` falls back
  to the parent culture and finally to English.
- **`{i18n:Loc}` resolves when XAML loads**, so a language change needs a restart.
  That is a deliberate trade against adding a binding framework to the template.
- **Per-culture layout metrics** (`metric_*` rows) let a fixed canvas absorb
  languages whose text is longer, without editing XAML.
  ([ADR 0007](../decisions/0007-branding-and-metrics.md))
