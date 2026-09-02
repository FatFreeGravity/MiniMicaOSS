# Localization

MiniMica localizes from **one spreadsheet**. Translators fill cells, a generator
emits `.resx`, the build produces satellite assemblies. No translator ever opens
XAML or Visual Studio.

```
worksheet.csv  ──generate──▶  Strings.<culture>.resx  ──build──▶  <culture>/App.resources.dll
```

`Strings.resx` (neutral English) always ships. Cultures are opt-in, so a
single-language app pays nothing.

---

## The catalog

**`tools/localization/worksheet.csv`**, 30 rows × 25 cultures.

| Column | Meaning |
|---|---|
| `ResourceID` | key used by code and XAML |
| `en-US` | source text; **required** |
| everything else | translation, or **blank to inherit** |

Row groups:

| Prefix | Purpose | Localized? |
|---|---|---|
| `app_*` | sample page copy | English only in the sample |
| `titlebar_*`, `pan_*` | chrome tooltips + narrator names | all 25 cultures |
| `settings_*` | Settings dialog | all 25 cultures |
| `metric_*` | per-culture layout numbers | see [step 6](#step-6--fix-overflow-without-touching-xaml) |

Two rules the generator enforces:

1. **A blank cell is omitted, never written empty.** `ResourceManager` then falls
   back to the parent culture and finally to English. An empty string would render
   as blank UI.
2. **Placeholders must match the English source.** `{ProductName}` and
   `{M.m.build}` are substituted by `Strings.Expand`. A dropped or renamed
   placeholder is an error, not a warning.

---

## Step 1: Add a string

Append a row. `ResourceID` is lowercase with underscores; fill `en-US`; leave the
rest blank for now.

```csv
app_footer_note,Sold separately,,,,,…
```

---

## Step 2: Use it

XAML:

```xml
<TextBlock Style="{StaticResource MiniMica.PageNoteStyle}"
           Text="{i18n:Loc Key=app_footer_note}" />
```

C#:

```csharp
string s = Strings.Get("app_footer_note");
string t = Strings.Expand("settings_title", AppOptions.DisplayName, null);
```

`{i18n:Loc}` resolves **when XAML loads**, so a language change needs a restart.
The Settings dialog offers one. This is deliberate: the alternative is a binding
framework in a template meant to stay small.

---

## Step 3: Translate

Pick whichever fits your team; all three produce the same worksheet.

**A. Spreadsheet (default).** Open the CSV in Excel or Sheets, hand out columns,
save as CSV, UTF-8. Best for bulk work and outside vendors.

**B. Machine translation + review.** The 25-culture catalog in this repo was
produced this way, then reviewed. Give the model the English column, the
`ResourceID` (it carries intent), and the constraint that placeholders must be
preserved verbatim. Review before shipping, because the tooltips use *standard Windows
terminology*, which a general translator often gets wrong.

**C. ResXManager.** The VS extension / standalone tool edits all `.resx` in a
grid, side by side, with a spell checker. Best for small ongoing fixes.

> ResXManager edits the **generated** `.resx`, not the worksheet. Copy changes
> back into the CSV or the next generate overwrites them. `verify.py localization`
> detects the drift.

---

## Step 4: Generate

```bash
# any OS, this is the tested one
python3 tools/localization/generate_resx.py --tier 14 --clean
```

```powershell
# Windows-native equivalent
pwsh tools\localization\generate-resx.ps1 -OutputDirectory src\MiniMicaApp\Localization -Tier 14
```

Tiers are convenience sets, not a claim about your audience:

| Tier | Cultures | Rough reach |
|---|---|---|
| `14` | en, de, fr, es×2, pt×2, zh×2, it, ru, uk, nl, pl | ~95 % |
| `22` | + sv, da, nb, fi, ja, ko, cs, tr | ~99 % |
| `25` | + id, th, vi | adds ASEAN |
| `All` | every column present | testing |

**Tier 14 is committed by default.** Trim or extend to taste; every culture adds
a small satellite assembly.

---

## Step 5: Build and test

The SDK picks up `Strings.<culture>.resx` automatically; no `.csproj` edit.

To see another language without changing Windows:

1. Open **Settings** while holding Ctrl+Shift; a developer **Language** row appears
2. Pick a culture, accept the restart

Stored as `language` in `%LOCALAPPDATA%\OEM\MiniMica\<App>\app.config`; `00`
means follow Windows. An unknown value falls back to Windows rather than failing.

A culture with no satellite still selects and falls back to English, which is
exactly what you want to see while testing coverage.

---

## Step 6: Fix overflow without touching XAML

A fixed canvas cannot reflow, and the same sentence is materially longer in
German or Finnish. Rather than hardcoding sizes per language, the numbers that
absorb that difference live in the **same worksheet**:

| ResourceID | Controls | en-US |
|---|---|---|
| `metric_page_title_size` | page title size | 33 |
| `metric_page_subtitle_size` | subtitle size | 12 |
| `metric_feature_heading_size` | feature heading size | 22 |
| `metric_feature_body_size` | feature body size | 14 |
| `metric_feature_body_width` | body wrap width | 375 |
| `metric_action_button_size` | button text size | 22 |

German body text overflowing? Put `13` in the `de-DE` cell of
`metric_feature_body_size`, regenerate, done. Blank cells inherit en-US.

At startup `LocalizationManager.ApplyMetrics` reads these, parses them as
doubles, and writes them into `MiniMica.*FontSize` resources that the styles
already consume. A missing or non-numeric cell is skipped, leaving the XAML
baseline, so a bad cell costs one measurement rather than startup.

**Why here:** whoever notices the overflow is the person already editing the
spreadsheet. Adding a language and tuning it for that language become one task in
one file, and a fix is a cell rather than a code change and rebuild.

To add a metric: append a `metric_*` row, add a `MiniMica.*` fallback in
`Styles.xaml`, add the pair to `Metrics` in `LocalizationManager`, and point the
style's setter at the new resource.

---

## Step 7: Verify

```bash
python3 tools/verify/verify.py localization
```

Checks that every referenced key exists, placeholders match across all cultures,
`metric_*` values parse as numbers, the worksheet and neutral `.resx` agree, and
**committed satellites match the worksheet**, which is the drift that happens
when someone edits a `.resx` directly.

---

## Reference

| Item | Value |
|---|---|
| Catalog | `tools/localization/worksheet.csv` |
| Generators | `generate_resx.py` (any OS), `generate-resx.ps1` (Windows) |
| Neutral resources | `src/MiniMicaApp/Localization/Strings.resx` |
| Lookup API | `Strings.Get`, `Strings.Expand` |
| XAML markup | `{i18n:Loc Key=…}` |
| Culture control | `LocalizationManager.ApplyStoredLanguage` |
| Metrics | `LocalizationManager.ApplyMetrics` |
| Dev picker | Settings, opened with Ctrl+Shift |

**Culture naming.** Specific names throughout (`zh-TW`, `zh-CN`, `pt-BR`,
`pt-PT`, `es-MX`, `es-ES`). `zh-TW` is Traditional, `zh-CN` Simplified. The v4.1
catalog used neutral `zh` for Traditional, so check this when importing older
translations.

**RTL is not supported.** The chrome and fixed canvas assume left-to-right.
Arabic and Hebrew need layout work beyond translation.
