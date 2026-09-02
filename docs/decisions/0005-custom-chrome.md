# ADR 0005, Custom chrome only

**Status: Accepted (v5.0).** Supersedes [0001](0001-native-title-bar.md).

## Decision

MiniMica draws its own title bar. There is no system-title-bar mode and no
option to switch, one path, tested.

## Why

- **Consistency.** Derived OEM apps must look identical across Windows builds.
- **The Settings gear.** Every derived app needs it in the caption; a system bar
  cannot host it.
- **The trade is smaller than it looks.** Custom chrome usually costs Snap
  Layouts. It does not have to (below).

## How

| Concern | Owner |
|---|---|
| Caption visuals, icon, title, buttons | MiniMica (`Resources/WindowChrome.xaml`) |
| Window state, resize, snap, drag | Windows / WPF `WindowChrome` |
| Minimize / maximize / restore / close | `SystemCommands` |
| Maximized geometry, min track size | `WM_GETMINMAXINFO` |
| Snap Layouts flyout | `WM_NCHITTEST` → `HTMAXBUTTON` |

We deliberately do **not** recreate v4.x pseudo-maximize state. Real
`WindowState` is used, so Windows keeps restore bounds and snap correct.

### Snap Layouts

Answering `HTMAXBUTTON` makes Windows offer the flyout, and makes that rectangle
non-client, so the WPF button stops receiving mouse input. Hover, press and click
are therefore driven from `WM_NCMOUSEMOVE` / `WM_NCMOUSELEAVE` /
`WM_NCLBUTTONDOWN` / `WM_NCLBUTTONUP` and surfaced as
`IsMaximizeButtonHovered` / `IsMaximizeButtonPressed`, which the template binds.
Remove any one of those handlers and the button goes visually dead while the
flyout still works.

`IsSnapLayoutEnabled="False"` opts out.

## Costs accepted

- MiniMica owns caption accessibility: UI Automation names, `Alt+Space`,
  contrast-theme colors.
- The caption palette must be maintained by hand. Values are ported from v4.1 and
  locked by `verify.py chrome`, because re-deriving them produced visible
  regressions more than once.
