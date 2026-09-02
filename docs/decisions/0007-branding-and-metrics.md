# ADR 0007, Branding as a layer; per-culture metrics as data

**Status: Accepted (v5.0)**

## Decision

Two related rules:

1. **Views carry no typography.** No `FontFamily`, `FontSize`, `FontWeight` or
   `Foreground` under `Views/`. Views position elements and name a style; every
   visual value lives in `Resources/Styles.xaml`.
2. **Per-culture layout numbers are data.** Font sizes and the body wrap width
   resolve `MiniMica.*FontSize` resources, whose values come from `metric_*` rows
   in `tools/localization/worksheet.csv`.

## Why (1)

Restyling a fork becomes a bounded edit to one dictionary instead of a search
across the tree. It also means a coding assistant can be pointed at a single file
to restyle an app.

Enforced by `verify.py styles`, because this erodes one attribute at a time.

## Why (2)

A fixed canvas cannot reflow, and the same sentence is materially longer in
German or Finnish. The previous practice was to hardcode a size and hand-tune it
after looking at a build.

Putting the numbers in the translation worksheet means:

- the person who notices the overflow is already in that file
- adding a language and tuning it are one task, not two
- a fix is a spreadsheet cell, not a code change and rebuild
- blank inherits en-US, exactly like a translation

`LocalizationManager.ApplyMetrics` reads them at startup, after the culture is
set and before any window measures text. A missing or non-numeric cell is
skipped, leaving the XAML baseline. A bad cell costs one measurement, not
startup.

## Also: brush roles are separate keys

`BrandAccentBrush` (fill) and `BrandAccentTextBrush` (text) are distinct even
though they hold the same color in light and dark. Under a contrast theme the
fill role becomes button-face, which equals the window background in most
contrast themes. So a text element bound to the fill brush becomes invisible.

This shipped as a real bug: the sample subtitle vanished under every contrast
theme. `verify.py styles` now fails on fill-role brushes used as `Foreground`.

## Alternative considered

Per-culture XAML dictionaries (`Metrics.de-DE.xaml`) merged at startup. More
idiomatic WPF, but it splits per-language tuning across two file types and two
workflows, and translators cannot touch it.
