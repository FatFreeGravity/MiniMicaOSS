# ADR 0003: Target .NET Framework 4.8, designed for Windows 11

Status: Accepted. Revised 2026-09 to lower the minimum OS from build 22621 to 18362.

## Decision

MiniMica applications target **.NET Framework 4.8 (`net48`)**, are **designed for Windows 11 22H2 / build 22621 or newer**, and **run on Windows 10 1903 / build 18362 or newer**.

The template does not target modern .NET and does not carry a private .NET runtime.

Two tiers, one binary:

| Tier | Builds | What you get |
|---|---:|---|
| Designed for | 22621+ | Everything: Mica, rounded corners, Snap Layouts flyout, Segoe Fluent Icons |
| Runs on | 18362-22620 | Every function works. Solid window background, square corners, no Snap Layouts flyout, Segoe MDL2 Assets glyphs |
| Refused | below 18362 | A version message, then exit |

Three constants in `WindowsVersion` carry this: `MinimumBuild` (18362), `RoundedCornersBuild` (22000) and `BackdropBuild` (22621). `SupportsBackdrop` and `SupportsRoundedCorners` gate the DWM calls; nothing else in the tree needs a capability check.

## Why .NET Framework 4.8

MiniMica is intended for distribution scenarios where payload size matters. Windows 11 already carries a compatible .NET Framework 4.x runtime, so a simple MiniMica application can ship as essentially its own executable, configuration and resources rather than a bundled runtime.

Windows 10 1903 was the first release with .NET Framework 4.8 in the box, which is what sets the floor. Windows 11 22H2 and newer include 4.8.1. .NET Framework 4.x releases are in-place updates, so an application compiled for 4.8 runs on the newer 4.8.1 runtime.

This gives MiniMica a practical target of **about 1 MB or less for a small application payload**, before product-specific media or third-party libraries are added.

## Why build 22621 is the design target

Windows 11 22H2 (build 22621) was released in September 2022 and is the first release where `DWMWA_SYSTEMBACKDROP_TYPE` is documented. It is what a Mica-oriented template should be built against in 2026.

## Why build 18362 is the floor

The original decision refused to start below 22621. Testing on Windows 10 20H1 (build 19041) showed the app runs there and every function works; only the Windows 11 finishes are absent. Refusing to start was therefore costing OEM reach for nothing, since the capability checks that make the degradation safe are three lines of code.

Windows 10 is an edge case for OEM desktop apps in 2026, so it gets a run-on guarantee rather than a designed-for one: it is not where the pixel-level work goes, but it is not blocked either.

At the time of this decision, the recent Windows 11 generation includes:

| Release | Base build | Availability |
|---|---:|---|
| 22H2 | 22621 | September 2022 |
| 23H2 | 22631 | October 2023 |
| 24H2 | 26100 | October 2024 |
| 25H2 | 26200 | September 2025 |
| 26H1 | 28000 | February 2026; selected new hardware |

The table defines a technical compatibility family, not Microsoft servicing status. Some older releases in it may already be out of support for particular Windows editions.

## Consequences

### Benefits

- No bundled .NET runtime.
- Very small shipping payload.
- No runtime NuGet dependencies in the starter.
- Mature WPF and CLR behavior.
- Smaller compatibility matrix.
- The documented DWM system-backdrop API is available on every designed-for OS.
- A single binary covers Windows 10 and Windows 11 without conditional compilation.

### Costs

- No modern .NET-only APIs such as WPF `ThemeMode`.
- MiniMica maintains its own small semantic WPF resource palette.
- Developers needing newer runtime APIs must add or port them themselves.
- Windows 10 and Windows 11 21H2 render a reduced look, so screenshots and pixel comparisons are only meaningful on 22621 or newer.
- Every use of Segoe Fluent Icons has to name Segoe MDL2 Assets as a fallback, because Fluent Icons ships only with Windows 11.

## Guardrails

CI builds generated applications as `net48`, rejects runtime `PackageReference` entries in the starter, and enforces a **1.25 MiB shipping-payload budget** for the generated sample (excluding symbols and XML documentation).
