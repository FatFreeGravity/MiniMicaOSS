# DPI and monitor placement

WPF uses device-independent pixels (DIPs); Win32 monitor APIs report physical pixels. Mixing them without conversion is a common source of incorrect placement at 125%, 150%, or mixed-monitor scaling.

## Manifest

`app.manifest` requests `PerMonitorV2,PerMonitor` DPI awareness.

## Placement algorithm

`WindowPlacementService.Place`:

1. obtains the window handle;
2. calls `MonitorFromWindow(..., MONITOR_DEFAULTTONEAREST)`;
3. calls `GetMonitorInfo` to get that monitor's **work area**;
4. calls `GetDpiForWindow`;
5. converts Win32 pixel coordinates to WPF DIPs using `dpi / 96.0`;
6. places relative to the selected work-area edge.

Because the work area excludes the taskbar, it works when the taskbar is on the top, bottom, left, or right.

## Example: bottom-right utility window

```csharp
Loaded += delegate
{
    WindowPlacementService.Place(
        this,
        WorkAreaPlacement.BottomRight,
        new Thickness(12));
};
```

## Do not assume the primary monitor

A user can launch or move an application on any monitor. Utility-window positioning should be relative to the window's actual monitor unless the product has an explicit reason to target a different one.

## Testing matrix

At minimum test:

- 100% / 100% dual monitor;
- 100% / 150% mixed DPI;
- primary monitor on either side;
- taskbar on bottom and left/right;
- move window between monitors before opening a child utility window.
