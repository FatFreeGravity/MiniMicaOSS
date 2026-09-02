# Offline verification suite

`verify.py` runs the MiniMica static checks that need neither Windows, MSBuild nor
PowerShell. It exists because a large class of defects in this repository —
missing resource keys, XAML wired to methods that do not exist, template-rename
drift — are fully determined by the source text, and catching them offline keeps
the first Windows compile focused on genuine platform problems.

```bash
python3 tools/verify/verify.py             # all checks
python3 tools/verify/verify.py loc         # localization only
python3 tools/verify/verify.py precompile  # XAML/C# wiring only
python3 tools/verify/verify.py chrome      # v4.1 pixel contracts
python3 tools/verify/verify.py styles      # centralized styling, brush roles, assets
python3 tools/verify/verify.py rename      # template generation only
python3 tools/verify/verify.py docs        # doc links, paths, stale identifiers
```

Exit code `0` means every check passed. Python 3.6+ only, no packages required.

## What each check covers

### `localization`
- committed satellites match the worksheet (drift happens when someone edits a
  `.resx` directly instead of the catalog)
- `metric_*` rows parse as numbers
- every resource ID referenced from XAML (`{i18n:Loc Key=...}`) or C#
  (`Strings.Get` / `Strings.Expand`, including the runtime-selected
  `titlebar_maximize` / `titlebar_restore` pair) exists in `Strings.resx`
- resource IDs defined but never referenced (reported as a note, not a failure)
- `worksheet.csv` — duplicate IDs, malformed IDs, empty English values
- **placeholder safety**: a translated cell must contain exactly the same
  `{Token}` set as its English source. A mismatch silently breaks
  `Strings.Expand`, so it is treated as an error rather than a warning
- `worksheet.csv` and the neutral `Strings.resx` describe the same key set

### `precompile`
Approximates what the C#/XAML compilers reject:
- `{StaticResource}` / `{DynamicResource}` / `SetResourceReference` /
  `FindResource` keys resolve to an `x:Key` defined somewhere in the tree
- every `x:Class` resolves to a real C# class in the declared namespace
- every `xmlns:` `clr-namespace:` declaration names a namespace that exists,
  and every type used through that alias exists in it
- XAML event handler attributes resolve to a method in the code-behind
- every `[TemplatePart(Name = ...)]` a control declares is actually present as
  an `x:Name` in some template
- merged `ResourceDictionary` `Source=` paths point at files that exist

### `chrome`
Pixel contracts inherited from v4.1, each added after a visible regression:
- caption glyphs use **Segoe MDL2 Assets @ 10**, pan chevrons MDL2 @ 12, and the
  Settings gear overrides to Segoe Fluent Icons @ 12 (E713 is a Fluent glyph)
- the Settings dialog stays **480 x 300 DIP**
- the dialog uses stock radio/checkbox controls and no accent brush
- no glyph codepoint outside the v4.1 set

### `styles`
- no `FontFamily` / `FontSize` / `FontWeight` / `Foreground` attribute under `Views/`
- every `/Resources/...` asset a view references exists **and** is declared as a
  `<Resource>` in the csproj (a missing entry compiles, then throws at runtime)
- **brush roles**: a fill-role brush used as `Foreground`, or a text-role brush
  used as `Background`/`Fill`, is an error. Contrast themes collapse many colors
  onto one value, so a fill brush used as text becomes invisible even though it
  looked right in light and dark

### `docs`
- broken internal links
- code paths mentioned in prose that no longer exist
- stale identifiers (`MicaWindow`, `MiniMica.sln`)

### `rename`
Simulates `dotnet new minimica -n Contoso` offline — file/directory renames,
`sourceName` substitution and template symbol replacement — then audits the
result for stale `MiniMicaApp` identifiers, unreplaced sentinels
(`MINIMICA_THEME`, `MINIMICA_BACKDROP`) and a set of generated-app invariants
(root namespace, assembly name, `ResourceManager` base name, manifest identity,
explicit version block, `.template.config` exclusion).

## What it deliberately does NOT cover

This is a static text analysis. It is **not** a compiler and passing it is not a
substitute for building on Windows. In particular it does not check:

- C# type checking, overload resolution, or anything semantic
- XAML property/attached-property validity, or whether a value parses into its
  target type
- binding paths, converters, or anything resolved at runtime
- WPF layout, DPI behavior, DWM/Mica, window chrome or any runtime behavior
- the real `dotnet new` template engine, which also rewrites case variants of
  `sourceName`; the simulation replaces the literal form only, and reports
  case-variant residue as a note so the two can be compared

Runtime behavior is verified separately — see `docs/v5-test-protocol.md`.

## Why Python, and what a C# port would take

The project convention is C# in the solution, and the original v4.1 workflow used a
C# `i18n` helper project. These tools are Python for one reason: they were authored
and validated on a machine that cannot compile or run .NET, so Python is the only
version that has actually been *tested*. Shipping untested C# into your solution
would risk breaking your build, which has already happened twice from smaller
guesses.

If you want them in-solution, the port is small and self-contained:

| Tool | Port effort | Notes |
|---|---|---|
| `generate_resx.py` | low | ~200 lines; a `dotnet run` console project, same CSV in, same `.resx` out. `generate-resx.ps1` already mirrors it |
| `verify.py` | medium | ~700 lines of regex over source text; no .NET API needed, so it translates directly |

Neither is on the critical path: the localization generator has a PowerShell twin,
and the verification suite is a developer and CI convenience rather than part of
the build. Say the word and they become a `tools/` C# project in the solution.

## Why Python rather than PowerShell

The rest of the repository's tooling is PowerShell, which is the natural choice
on Windows. These checks are Python because they were authored and validated on
a non-Windows machine, and because CI can then run them on any agent before the
Windows build job starts. Each check has been verified by fault injection — a
deliberately broken tree is used to confirm every rule actually fires, so a
passing run means the rules ran rather than silently matched nothing.

Porting them to PowerShell later is reasonable; keep the fault-injection habit
if you do.
