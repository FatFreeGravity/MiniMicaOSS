# Platform references

MiniMica intentionally stays close to platform APIs.

## .NET Framework

- .NET Framework system requirements: https://learn.microsoft.com/dotnet/framework/get-started/system-requirements
- .NET Framework versions and dependencies: https://learn.microsoft.com/dotnet/framework/install/versions-and-dependencies

Windows 11 22H2 and later include .NET Framework 4.8.1; MiniMica targets 4.8 and relies on .NET Framework 4.x in-place runtime compatibility.

## Windows release baseline

- Windows release health: https://learn.microsoft.com/windows/release-health/
- Windows 11 release information: https://learn.microsoft.com/windows/release-health/windows11-release-information

MiniMica's minimum build is 22621 (Windows 11 22H2).

## DWM

- `DwmSetWindowAttribute`: https://learn.microsoft.com/windows/win32/api/dwmapi/nf-dwmapi-dwmsetwindowattribute
- `DWM_WINDOW_ATTRIBUTE`: https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
- `DWM_SYSTEMBACKDROP_TYPE`: https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type
- Mica material guidance: https://learn.microsoft.com/windows/apps/design/style/mica

## DPI/windowing

- High-DPI desktop guidance: https://learn.microsoft.com/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows
- `GetDpiForWindow`: https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getdpiforwindow
- `MonitorFromWindow`: https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-monitorfromwindow
- `GetMonitorInfo`: https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getmonitorinfow

These links explain the APIs wrapped by `Platform/Windows`; application views should not need them for normal use.
