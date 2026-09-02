# DWM integration

All Desktop Window Manager interop lives under `Platform/Windows/Dwm`.

## Attributes used

```text
20  DWMWA_USE_IMMERSIVE_DARK_MODE
33  DWMWA_WINDOW_CORNER_PREFERENCE
38  DWMWA_SYSTEMBACKDROP_TYPE
```

`DwmNative` owns `IntPtr`-based P/Invoke signatures that work naturally with .NET Framework 4.8. `DwmWindow` exposes small managed operations.

The supported OS floor is Windows 11 build 22621, so the code does not contain an older-Mica implementation or Windows 10 branch.

## Frame extension

When a system backdrop is active, MiniMica calls `DwmExtendFrameIntoClientArea` with `-1` margins so DWM can paint behind the WPF client area. The WPF window background becomes transparent only after the backdrop call succeeds.

## Failure behavior

DWM HRESULT failures do not throw during window construction. MiniMica uses a solid theme-aware background instead.

This fallback is for robustness on supported Windows 11 builds—not a statement of support for older operating systems.
