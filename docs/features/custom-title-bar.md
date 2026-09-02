# Custom title bar

MiniMica draws the caption itself. Windows keeps window state. See
[ADR 0005](../decisions/0005-custom-chrome.md).

## Anatomy

```
┌──────────────────────────────────────────────────────────────┐
│ [icon]  Title                        [gear][—][□][×]         │  30 DIP
└──────────────────────────────────────────────────────────────┘
```

| Element | Metric |
|---|---|
| Title bar height | 30 DIP |
| Caption button width | 45 DIP |
| App icon | 16 × 16 at x ≈ 18 |
| Title text | starts x ≈ 48, Segoe UI 12 |
| Settings gear | immediately before Minimize |

These are matched pixel-wise against the Windows 11 Calculator title bar.
`verify.py chrome` locks them.

## Fonts, do not change casually

| Element | Font | Size |
|---|---|---|
| Minimize / Maximize / Restore / Close | **Segoe MDL2 Assets** | 10 |
| Settings gear (E713) | **Segoe Fluent Icons** | 12 |
| Pan chevrons (E0E2 / E0E3) | **Segoe MDL2 Assets** | 12 |

Both fonts contain E921/E922/E923/E8BB, but Fluent Icons redraws them with
different metrics: at 10 pt the minimize bar loses a pixel and the maximize
square stops landing on pixel boundaries, so its 1 px outline antialiases across
two rows and reads as a thicker, softer edge. The gear is the deliberate
exception, E713 is a Fluent Icons glyph.

## Colors

| State | Light | Dark | Contrast theme |
|---|---|---|---|
| Glyph, active | Black | White | `ControlText` |
| Glyph, inactive | Silver | Gray | `ControlText` (not dimmed) |
| Hover fill | `#DBDBDB` | `#363636` | `Highlight` |
| Close hover | `#C42B1C` / white | same | `Highlight` |
| Title text | Black | White | `ActiveCaptionText` |
| Caption fill | transparent | transparent | `ActiveCaption` |

Hover and pressed live in `Style.Triggers` and set the **button's** Background and
Foreground. They must not be `ControlTemplate.Triggers` targeting the inner
border. A template trigger outranks the `TemplateBinding`, so a derived style
(the close button) could never repaint it.

Trigger order matters: the inactive `DataTrigger` is declared **first** and
hover/pressed after, so hovering an inactive window restores full contrast.

**Glyphs and title text use different brushes.** The caption fill covers only the
icon/title column, never the buttons, so glyphs sit on the window background and
need `ControlText` rather than `ActiveCaptionText`.

## Behavior

| Input | Result |
|---|---|
| Caption buttons | `SystemCommands` minimize / maximize / restore / close |
| Hover maximize | Windows 11 Snap Layouts flyout |
| `Alt+Space` | system menu |
| Right-click caption | system menu |
| Double-click caption | maximize / restore |
| Drag | move; drag down from maximized restores |
| Edges and corners | resize (8 DIP border) |

Maximized state swaps the glyph to E923, retitles the tooltip to
`titlebar_restore`, and drops the corner radius and contrast frame.

## Contrast themes

The caption band is filled with `ActiveCaption` (inactive: `InactiveCaption`) —
without it, `ActiveCaptionText` renders dark-on-dark. The window outline is 4 DIP;
thinner values disappear into the rounded corner. It is drawn as an **overlay**
so it costs no client area.

## Hiding buttons

```xml
<shell:MiniMicaWindow ShowSettingsButton="False"
                      ShowMinimizeButton="False"
                      ShowMaximizeButton="False">
```

Close cannot be hidden. The Settings dialog uses exactly this.
