# Contributing

MiniMica values small, understandable changes over feature accumulation.

## Non-negotiable baseline

- Target: **.NET Framework 4.8**.
- Minimum OS: **Windows 10 19H1 build 18362**.
- Starter runtime dependencies: **none**.
- Generated sample Release payload: **< 1 MiB**, excluding symbols/XML documentation.

Changing one of those constraints requires an explicit architecture decision, not an incidental package addition.

## Before adding a feature

Ask:

1. Is this needed by a large share of MiniMica-style applications?
2. Is it Windows plumbing developers repeatedly get wrong?
3. Can a downstream application add it cleanly instead?
4. Can users delete it without surgery?
5. What does it add to the shipping payload?

WebView2, telemetry, analytics, update systems, installers, authentication, API clients, and product navigation normally belong downstream.

## Pull requests

- Keep Win32 code under `Platform/Windows`.
- Avoid global mutable state.
- Do not add a runtime `PackageReference` casually.
- Keep C# compatible with the repository's configured language level.
- Preserve high-contrast behavior.
- Test mixed DPI and recent Windows 11 releases when relevant.
- Keep the template source directly runnable.
- Run `scripts/test-template.ps1` for template/runtime changes.
- Update documentation together with behavior.


## Localization changes

When adding or changing user-visible resource keys, update the neutral `src/MiniMicaApp/Localization/Strings.resx` and the translation worksheet when the key belongs in the reusable catalog. Do not hand-copy the same change across culture files. Preserve placeholder tokens exactly, and generate only the culture resources needed for the test/product scenario.
