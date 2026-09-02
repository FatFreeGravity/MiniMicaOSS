# ADR 0004: Use RESX localization with optional satellite assemblies

## Status

Accepted.

## Context

MiniMica targets .NET Framework 4.8 partly to keep downstream deployment payloads extremely small. Localization must therefore avoid bringing a runtime localization framework into every generated application.

The original MiniMica implementation already used `.resx` files, `CurrentUICulture`, a spreadsheet translation matrix, and optional embedded satellite DLLs. It also depended on a machine-specific CsvHelper utility and generated strongly-typed resource code through Visual Studio.

## Decision

MiniMica v5.0 keeps the standard `.resx`/`ResourceManager` model but simplifies it:

- neutral English `Strings.resx` is in the starter;
- runtime localization code has no NuGet dependency;
- XAML uses a tiny `LocExtension`;
- no generated `Strings.Designer.cs` is required;
- a user-selected culture is applied at startup;
- language changes are restart-to-apply by default;
- exact regional BCP-47 culture names are preserved;
- translated resources are generated only for languages a product chooses to ship;
- 14/22/25 historical language tiers exist only as tooling shortcuts;
- standard satellite assemblies are preferred over embedding them into the EXE.

## Consequences

The neutral MiniMica deployment remains tiny. International products pay approximately only for the resources they actually ship. Developers can edit translations in a spreadsheet and regenerate resources without changing application code.

Dynamic in-process language switching is intentionally not provided by the starter. Applications that require it can replace the `LocExtension` with a binding-based localization service.
