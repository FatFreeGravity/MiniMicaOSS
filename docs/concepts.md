# Concepts

Five ideas explain almost every design decision in MiniMica. Read this once and
the rest of the codebase is predictable.

---

## 1. A template, not a framework

You **fork** MiniMica; you do not reference it. There is no NuGet package to
track, no base-library upgrade path, no plugin model. `dotnet new minimica -n Contoso`
gives you a project you own outright and can cut apart freely.

**Benefit:** no dependency you did not choose, and nothing between you and the
code when something needs to change.

---

## 2. A fixed design canvas inside a resizable window

Derived apps are one-pagers with a designed layout, not responsive web pages.
`FixedPageHost` hosts a canvas of a known size, 960 × 640 by default.

| Window is… | Behavior |
|---|---|
| wider than the canvas | canvas centered, empty padding around it |
| narrower | canvas clipped, left/right pan buttons appear |
| any size | canvas is **never scaled**, text never shrinks |

There is no whole-page vertical scrolling. A page that needs an internal
scrolling region adds its own `ScrollViewer`.

**Benefit:** you lay out once at a real size and it stays exact. Resize and
Snap still work, so the app remains a well-behaved Windows citizen.

```xml
<controls:FixedPageHost DesignWidth="960" DesignHeight="640" MinimumViewportWidth="500">
```

---

## 3. Custom chrome, Windows-owned state

MiniMica draws the title bar (icon, title, Settings gear, caption buttons) so
it looks identical on every supported build. Everything *stateful* stays with
Windows: real `WindowState`, `SystemCommands`, resize borders, snap.

Notably the maximize button answers `HTMAXBUTTON` to `WM_NCHITTEST`, so
**Windows 11 Snap Layouts flyout works over custom chrome**. That is usually
the reason people give up and go back to a system title bar.

**Benefit:** full visual control without reimplementing window management.

---

## 4. One semantic palette, three themes

No control names a color. Everything resolves a **semantic key** through
`DynamicResource`, and `ThemeManager` fills those keys for light, dark and
Windows contrast themes.

```
MiniMica.BrandInkBrush        →  #001D2F  |  White      |  SystemColors.WindowText
MiniMica.CaptionHoverBrush    →  #DBDBDB  |  #363636    |  SystemColors.Highlight
```

Two rules keep this honest:

- **Every key is set in all three branches.** A key set only in the light branch
  keeps a stale value elsewhere.
- **Fill-role and text-role brushes never share a key.** Contrast themes collapse
  many colors onto one value, so a fill brush used as text becomes invisible
  even though it looked fine in light and dark.

Both rules are enforced by `tools/verify/verify.py`, not left to reviewers.

**Benefit:** switching themes, including a live contrast-theme switch,needs no
code, because the resources re-resolve themselves.

---

## 5. Branding is a layer, not a search-and-replace

Making the app *yours* means editing a small, bounded set of things:

| Layer | Where |
|---|---|
| Identity (name, version) | `AppOptions`, `.csproj` |
| Assets (icon, hero, bullet icons) | `Resources/Branding/` |
| Colors | brand keys in `ThemeManager` + `Styles.xaml` |
| Typography | named styles in `Styles.xaml` |
| Copy | `tools/localization/worksheet.csv` |

Views contain **no** fonts, sizes or colors, only layout and a style name.
A verification rule fails the build if that erodes.

See **[branding.md](branding.md)** for the step-by-step.

**Benefit:** restyling is a predictable edit to a handful of files, not an
archaeology exercise.

---

## Size discipline

Every choice above is constrained by one number: the app should stay around
**1 MB**. That is why it targets .NET Framework 4.8 (in the box since Windows 10
1903, so no private runtime ships), carries **no runtime NuGet dependencies**,
and ships translations as opt-in satellites.

CI enforces a 1.25 MiB payload budget.

---

## What MiniMica deliberately is not

- not a DI, MVVM or navigation framework
- not responsive, no auto-scaling or reflow to arbitrary sizes
- not a control library
- not cross-platform

Each of these is a real cost paid to keep the template small and readable.

---

## Where things live

| Concern | Path |
|---|---|
| Window shell, chrome, snap | `Shell/` |
| Fixed canvas + panning | `Controls/FixedPageHost.cs` |
| Theme palette | `Platform/Windows/Theme/` |
| DWM backdrop, corners | `Platform/Windows/Dwm/` |
| OEM settings + persistence | `Settings/` |
| Strings and cultures | `Localization/`, `tools/localization/` |
| Styles, brushes, metrics | `Resources/Styles.xaml` |
| Chrome template | `Resources/WindowChrome.xaml` |
| Your page | `Views/` |
