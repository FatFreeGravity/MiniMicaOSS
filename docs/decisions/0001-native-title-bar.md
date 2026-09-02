# ADR 0001, Native title bar

**Status: SUPERSEDED by [0005](0005-custom-chrome.md) (v5.0)**

Kept for history. Do not follow this document.

## What it said

The v1.0 prototype used the system title bar and synchronised its dark-mode
appearance through DWM, on the grounds that Windows would then own Snap Layouts,
the system menu, accessibility and non-client DPI behavior for free.

## Why it was reversed

Visual consistency across derived OEM apps mattered more than the plumbing
Windows provides, and the v4.x custom title bar already reproduced Windows 11
spacing precisely while carrying a Settings gear every derived app needs.

The main argument for a native bar, that Snap Layouts only works there,turned
out to be avoidable: answering `HTMAXBUTTON` to `WM_NCHITTEST` gets the Snap
flyout over a custom maximize button. See [0005](0005-custom-chrome.md).
