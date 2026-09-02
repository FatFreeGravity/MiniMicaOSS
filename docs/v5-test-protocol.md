# MiniMica v5.0 Windows verification protocol

The manual test matrix for MiniMica v5.0. Run it on Windows 11 22H2 or later, which is
the designed-for target; the OS tiers in Part B5 cover the run-on range.

**Validated so far:** the solution compiles; Snap Layouts works over the custom
maximize button; caption hover, press and inactive states match v4.1; the Settings
dialog and its persistence behave; contrast themes render correctly including the
subtitle and window size; and Tab order plus Narrator announcements work.

**Not yet validated:** the DPI matrix, multi-monitor behavior, snap edge cases, and
the localization pass in Part B3. Those are the parts worth spending time on.

Part B is front-loaded with specific hypotheses drawn from reading the code, each
with a failure signature. Work top to bottom and stop at the first part that fails
badly, because a broken shell makes later results meaningless.

---

## Part A: build and smoke (5 minutes)

```powershell
# From "Developer PowerShell for VS 2022"
.\scripts\build-and-report.ps1
```

This writes `artifacts\build-report.txt` with errors, warnings and the Release
payload size. **If the build fails, send that file and stop here.** Everything
below depends on a running binary.

| # | Check | Expected |
|---|-------|----------|
| A1 | Debug build | succeeds |
| A2 | Release build | succeeds |
| A3 | Release payload | ≤ 1.25 MiB excluding `.pdb` / `.xml` |
| A4 | App launches | window appears, no first-chance crash |
| A5 | Window size | 960 × 670, content 960 × 640 under a 30 DIP title bar |

---

## Part B: the five suspected defects

Each has a **failure signature**: if you see it, record it verbatim. These are
hypotheses from code review rather than confirmed bugs, and several may be fine.

### B1 · Mica backdrop may not render
`MiniMicaWindow` sets `Background = Brushes.Transparent` with
`AllowsTransparency=false` and `GlassFrameThickness(1)`. Mica normally requires
the frame extended into the client area.

- **Pass:** window background shows the desktop-tinted Mica material; changing
  the desktop wallpaper changes the tint.
- **Fail signature:** flat gray/white/black background with no tint, or a black
  rectangle behind the content.
- Also try: Settings → Dark, and confirm the backdrop follows.

### B2 · System menu may appear twice or not at all
Right-click is handled *both* by WPF (`OnMouseRightButtonUp`) and natively
(`WM_NCRBUTTONUP`). With `WindowChrome.CaptionHeight`, the caption is non-client,
so WPF may never see the click, or both may fire.

- **Pass:** exactly one system menu, at the pointer.
- **Fail signature:** two menus (one flashes and is replaced), or none.
- Test at: title bar empty area, directly on the app icon, on the title text.

### B3 · System menu position may be wrong at non-100% DPI
`ShowSystemMenuAt` calls `PointToScreen` (device pixels) then applies
`TransformFromDevice`.

- **Pass:** menu appears at the pointer at 100 %, 150 % and 200 %.
- **Fail signature:** menu offset from the pointer, scaling with DPI, or opening
  on the wrong monitor. Note the DPI and monitor arrangement.

### B4 · Fixed page may not center when the window is wide  ← most likely to fail
`FixedPageHost` uses a `ScrollViewer` with `HorizontalContentAlignment="Center"`.
In WPF, centering and scrollable extent interact badly.

- **Pass (wide, > 960):** page centered with equal padding, matching
  `reference-material/screenshots/S2-wide-centered-canvas.png`.
- **Pass (narrow, < 960):** page clipped, circular chevron RepeatButtons appear
  vertically centered at both edges, matching `S1-narrow-horizontal-pan.png`.
- **Fail signatures:** page left-aligned when wide; page starts mid-scroll rather
  than at the left edge when narrowed; pan buttons visible with no overflow;
  right button still enabled at the right limit.
- Resize slowly through 960 and note the behavior exactly at the boundary.

### B5 · Rounded corners may clip incorrectly when maximized
A WPF `Border` with `CornerRadius=8` sits inside a window that also asks DWM for
rounded corners.

- **Pass:** rounded when restored, square and full-bleed when maximized.
- **Fail signature:** rounded corners persist when maximized leaving desktop
  showing through, or a 1px border artifact along any edge.

---

## Part B2: caption states and Snap Layouts (reworked round 2)

The caption palette is now ported verbatim from v4.1 and hover/pressed live in
`Style.Triggers` rather than `ControlTemplate.Triggers`. Snap Layouts is new.

| # | Check | Expected |
|---|-------|----------|
| B2-1 | Inactive window, buttons | glyphs `Silver` (light) or `Gray` (dark), the v4.1 values |
| B2-2 | **Hover a button while the window is inactive** | background fills, glyph goes to full `Black`/`White`, not gray |
| B2-3 | Hover Minimize / Maximize / Settings | background `#DBDBDB` light, `#363636` dark |
| B2-4 | **Hover Close** | background `#C42B1C`, glyph white, when active *and* inactive |
| B2-5 | Press Close | `#A4262C` |
| B2-6 | Title text when inactive | grays with the buttons |
| B2-7 | **Toggle Windows light/dark while the app is visible but NOT focused** | chrome recolors immediately, no click needed |
| B2-8 | Toggle theme via the Settings dialog | same, immediate |
| B2-9 | High contrast | caption uses system colors, remains legible |

### Snap Layouts: the part most likely to need a second pass

| # | Check | Expected |
|---|-------|----------|
| B2-10 | **Hover the maximize button ~0.5 s** | Windows 11 Snap Layout flyout appears below it |
| B2-11 | Pick a layout from the flyout | window snaps to that zone |
| B2-12 | Hover visual during all this | button still highlights, driven from `WM_NCMOUSEMOVE` rather than WPF |
| B2-13 | Click maximize normally | toggles maximize/restore as before |
| B2-14 | Move pointer off the button | highlight clears (no stuck hover) |
| B2-15 | Press and drag off the button, release | no state change, highlight clears |
| B2-16 | Restored window: drag the **top edge** over the button | still resizes, because the top 8px stays `HTTOP` |
| B2-17 | Maximized: hover the restore button | flyout still offered |
| B2-18 | `IsSnapLayoutEnabled="False"` on the window | plain WPF button, no flyout, everything else normal |

**Known risk. Read this if B2-10 fails.** Enabling the flyout requires our
`WM_NCHITTEST` hook to answer `HTMAXBUTTON` *before* `WindowChrome`'s own hook
answers `HTCLIENT`. `HwndSource` invokes hooks most-recently-added-first, and ours
is added in `OnSourceInitialized` (after `WindowChrome`'s), so it should win, but
that ordering is an implementation detail I could not verify without Windows.

If the flyout never appears but the button otherwise behaves normally, that
ordering is the first thing to suspect, not the hit-test math. Say so and I will
switch to an explicit subclass that does not depend on hook order.

If the button goes visually dead (no hover) **and** the flyout appears, the hit
test is working and only the `WM_NCMOUSEMOVE` visual path needs attention.

---

## Part B3: settings persistence and the developer language override (round 5)

Settings now live in v4.1's location and format:

```
%LOCALAPPDATA%\OEM\MiniMica\<AppName>\app.config
```

```xml
<configuration>
  <appSettings>
    <add key="appearance"   value="2" />   <!-- 0=Dark 1=Light 2=Automatic -->
    <add key="notification" value="1" />   <!-- 0=Off  1=On -->
    <add key="diagnostics"  value="0" />   <!-- 0=Off  1=On -->
    <add key="language"     value="00" />  <!-- 00=follow Windows -->
  </appSettings>
</configuration>
```

| # | Check | Expected |
|---|-------|----------|
| B3-1 | Open Settings, change Appearance, close | file appears at the path above |
| B3-2 | Inspect the file | four keys, exactly the encoding above |
| B3-3 | Dark / Light / Automatic | writes `0` / `1` / `2`. Note that **this is not the enum order** |
| B3-4 | Toggle each checkbox | `notification` / `diagnostics` flip between `0` and `1` |
| B3-5 | Restart the app | all three choices restored |
| B3-6 | Delete the file, restart | clean defaults, no error |
| B3-7 | Corrupt the file (truncate mid-tag), restart | falls back to defaults, no crash |
| B3-8 | Make the file read-only, change a setting | app stays alive; choice applies for the session |
| B3-9 | Generated `Contoso` app | writes to `...\OEM\MiniMica\Contoso\app.config` |

### Developer language override

| # | Check | Expected |
|---|-------|----------|
| B3-10 | Open Settings **normally** | no Language row: three options only |
| B3-11 | Open Settings holding **Ctrl+Shift** | fourth "Language (developer)" row appears |
| B3-12 | Row position | sits below Telemetry; dialog stays 480×300, nothing reflows |
| B3-13 | Pick e.g. `fr-FR` | prompt offers restart; `language` key becomes `fr-FR` |
| B3-14 | Accept the restart | app relaunches in French: caption tooltips and Settings translated |
| B3-15 | Decline the restart | setting still saved; applies on next manual start |
| B3-16 | Set back to *System default (00)* | returns to the Windows language |
| B3-17 | Pick a culture with **no** satellite built | falls back to English rather than showing raw keys |
| B3-18 | Hand-edit `language` to nonsense, restart | falls back to Windows language, no crash |

Generate satellites first, or B3-14 shows English for everything:

```powershell
pwsh tools\localization\generate-resx.ps1 -OutputDirectory src\MiniMicaApp\Localization -Tier 14
```

---

## Part B4: Windows contrast themes (round 8)

Set **Settings → Accessibility → Contrast themes** and try at least **Night Sky** (dark)
and **Aquatic** or **Desert** (light), so both branches of the black/white detection get
exercised.

Reference values measured from v4.1 under Night Sky: caption band and window frame are both
`#868CFF` (`ActiveCaption`), body `#000000`, content text `#FFFFFF`.

| # | Check | Expected |
|---|-------|----------|
| B4-1 | **Title bar fill** | fills the icon/title area only, in the system caption color |
| B4-1b | **Fill stops at the buttons** | the caption band must NOT extend behind gear/min/max/close: they keep the window background, as v4.1 does (measured: band ends exactly where the buttons begin) |
| B4-2 | **Title text** | legible against that band: this was black-on-black before |
| B4-3 | **Caption glyphs** | gear / minimize / maximize / close all visible, in window/control text color at full contrast |
| B4-4 | Hover a caption button | system highlight fill, matching glyph color |
| B4-5 | Hover Close | uses the system highlight, **not** the #C42B1C brand red |
| B4-6 | **Window frame** | 4 DIP outline in the caption color on all four sides |
| B4-7 | Deactivate the window | band and frame switch to the *inactive* caption color |
| B4-8 | Maximize | frame and rounded corners drop away, no desktop bleed |
| B4-9 | Page drop shadow | gone (it is noise against a flat palette) |
| B4-10 | Body text and headings | system window text: the brand teal is **not** used |
| B4-10b | **Subtitle line** | ✅ validated. Visible in system window text; it shared a brush with the action button fill and was invisible in every contrast theme |
| B4-11 | Bullet icons | the light artwork on a light contrast theme, dark artwork on a dark one |
| B4-12 | "Get Started" | button face + button text, with a visible outline; hover uses highlight |
| B4-13 | **No pan buttons appear** | the frame is an overlay and costs no client area, so 960 always fits |
| B4-13b | **Window size unchanged** | ✅ validated. Still 960 × 670 under a contrast theme |
| B4-14 | Toggle the contrast theme **while running** | everything above updates without a restart |
| B4-15 | Toggle back to a normal theme | frame gone, Mica returns, brand colors return, shadow returns |
| B4-16 | Settings dialog under a contrast theme | title band filled, stock controls legible |

**B4-13 / B4-13b.** A `Border`'s thickness insets its child, so drawing the frame on the
window border would cost 8 DIP of client area and overflow the 960 canvas. Compensating by
resizing the window was tried and double-counted against `MinHeight` (observed 686 instead
of 678). The frame is now `PART_ContrastFrame`, an overlay with `IsHitTestVisible=False`
layered over the content: zero layout impact, so the window stays exactly 960 × 670 in
every theme. The 4 DIP overlap lands in the page's own margins.

If the frame still does not appear, report whether the *caption band* filled correctly:
band-but-no-frame points at the border being painted under the DWM glass frame, which is a
different fix from the resource plumbing.

---

## Part B5: keyboard and Narrator (round 12) — ✅ validated

| # | Check | Expected |
|---|-------|----------|
| B5-1 | Press Tab repeatedly from a fresh launch | focus moves through Settings, Minimize, Maximize, Close, then the page content, then the action button |
| B5-2 | Focus ring | solid 2px ring, clearly visible in light, dark and contrast themes |
| B5-3 | Space or Enter on a focused caption button | performs the action |
| B5-4 | Start Narrator (`Ctrl+Win+Enter`), then Tab | each caption button is announced by its localized name |
| B5-5 | Tab onto the page text | Narrator reads the title, subtitle, headings and body |
| B5-6 | Tab onto the hero image | announced using `app_hero_alt` |
| B5-7 | Bullet icons | not tab stops, not announced |
| B5-8 | Narrow the window so the pan chevrons appear, then Tab | they are reachable and announced |
| B5-9 | Settings dialog with Narrator | every control announced; Ctrl+Shift language row too |
| B5-10 | Switch language, repeat B5-4 | announcements follow the language |

### OS tiers

| # | Check | Expected |
|---|-------|----------|
| B5-11 | **Windows 10 1903-22H2** | ✅ validated on build 19041. App runs, all functions work. No Mica, square corners, no Snap Layouts flyout. Icon glyphs fall back from Segoe Fluent Icons to Segoe MDL2 Assets |
| B5-12 | Windows 11 21H2 | runs; rounded corners, no Mica |
| B5-13 | Windows 11 22H2+ | the full look |
| B5-14 | Below build 18362 | version message, then exits |

Windows 10 is a supported-to-run tier, not a designed-for tier. Segoe Fluent Icons
ships only with Windows 11, so every use of it names Segoe MDL2 Assets as a fallback;
WPF resolves a font list per glyph, which leaves Windows 11 unchanged.

---

## Part C: behavior matrix

Mark each ✅ / ❌ / n-a. Anything ❌ needs a one-line note.

### C1 Custom title bar
| Check | Result |
|---|---|
| Icon 16×16 at x≈18, title at x≈48, matches `v41.png` | |
| Settings gear sits immediately before Minimize | |
| Caption buttons 45 DIP wide, title bar 30 DIP tall | |
| Minimize / Maximize / Restore / Close all work | |
| Maximize glyph swaps to restore glyph when maximized | |
| `Alt+Space` opens the system menu | |
| Double-click title bar toggles maximize/restore | |
| Double-click app icon closes the window | |
| Drag to move; drag down from maximized restores sensibly | |
| Resize from all four edges and corners | |
| Active vs inactive title foreground differ | |

### C2 DPI and multi-monitor
Test at **100 / 125 / 150 / 200 %**, and drag between mixed-DPI monitors.
| Check | Result |
|---|---|
| Title bar and caption buttons scale cleanly | |
| No blurred text after a monitor change | |
| Min width/height still enforced | |
| Correct behavior with the taskbar on each screen edge | |

### C3 Snap and window state
| Check | Result |
|---|---|
| `Win`+`Left` / `Win`+`Right` | |
| `Win`+`Z` snap layouts | |
| Snap to a narrow region: window stays usable | |
| Restore from snap returns to previous bounds | |
| Maximize does not cover the taskbar | |

*Mouse-hover Snap Layout flyout over the maximize button is explicitly out of scope.*

### C4 Fixed page host
| Check | Result |
|---|---|
| 960×670 exactly fits the 960×640 page | |
| Wider → centered (S2) | |
| Narrower → clipped + pan buttons (S1) | |
| Pan buttons: click, and press-and-hold repeat | |
| Left disabled at left limit, right at right limit | |
| Buttons disappear when there is no overflow | |
| No whole-page vertical scrolling anywhere | |
| Mouse wheel does not pan horizontally unexpectedly | |

### C5 Settings dialog
| Check | Result |
|---|---|
| Opens from the gear; only Close is visible in its chrome | |
| Dialog is 600×375 and resembles `Settings-v4.1.png` | |
| Automatic / Light / Dark apply immediately to all open windows | |
| Choice persists across restart | |
| Notifications checkbox persists | |
| Telemetry checkbox persists | |
| Delete `%LOCALAPPDATA%\MiniMicaApp\app.config` → clean defaults | |
| Corrupt that file with junk → falls back, does not crash | |
| Version reads 5.0.x | |

### C6 Accessibility
| Check | Result |
|---|---|
| High contrast themes: chrome remains readable | |
| Narrator announces Settings/Minimize/Maximize/Close correctly | |
| Keyboard reaches every Settings control | |
| Focus visuals are visible | |

### C7 Localization
```powershell
# generate satellites and rerun
pwsh tools\localization\generate-resx.ps1 -OutputDirectory src\MiniMicaApp\Localization -Tier 14
```
| Check | Result |
|---|---|
| Caption tooltips are translated, not raw keys like `titlebar_close` | |
| Settings dialog is translated | |
| Sample page stays English (intentional: falls back to neutral) | |
| Version string renders a number, not `{M.m.build}` | |
| Title renders the product name, not `{ProductName}` | |

---

## Reporting back

Send `artifacts\build-report.txt` plus this table filled in. For each ❌:

```
ID   : B4
What : page stayed left-aligned at 1400px wide
DPI  : 150%, single monitor, taskbar bottom
Note : pan buttons still visible with no overflow
```

A screenshot is worth attaching for anything visual (B1, B4, B5, C1).

That is enough to fix most defects without a second round-trip.
