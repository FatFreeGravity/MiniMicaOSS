# ADR 0006, Fixed design canvas with horizontal panning

**Status: Accepted (v5.0)**

## Decision

The app content is a **fixed-size canvas** inside a resizable window.

| Window vs canvas | Behavior |
|---|---|
| wider | canvas centered, padding around it |
| narrower | canvas clipped, left/right `RepeatButton`s pan |
| either | canvas never scaled |

No whole-page vertical scrolling. No `Viewbox`, no `ScaleTransform`.

## Why

Derived apps are designed one-pagers, not responsive layouts. Resize and Snap
support is an OS-citizenship requirement, not a design goal, so the cheapest
honest answer is to keep the design exact and make overflow *navigable* rather
than reflow it.

## Rejected: auto-scaling to fit

Shrinking the page to fit a narrow window makes text smaller, which fails
accessibility expectations, blurs raster art, and quietly breaks the fixed-page
model that makes these apps easy to design. Panning keeps type at its intended
size.

## Rejected: full responsiveness

Would push every derived app into reflow work for a window size almost no user
picks, for a product that is one page.

## Contract

```xml
<controls:FixedPageHost DesignWidth="960" DesignHeight="640" MinimumViewportWidth="500" />
```

`MinimumViewportWidth` is 500 because Windows 11 Snap guidance wants ≤ 500 epx.
Minimum height is derived: `DesignHeight` + title bar.

An app needing an internal scrolling region adds its own `ScrollViewer` inside
the page. That is app-specific and fine.

## Note

The contrast-theme window outline is drawn as an **overlay**, not as a border on
the frame. A `Border`'s thickness insets its child, which would cost client area
and make the canvas overflow on a window nobody resized.
