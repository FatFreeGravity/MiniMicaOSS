# Recipe: fixed utility window

For a small agent, palette, helper, or toast-like product window, keep the same appearance foundation and change normal WPF window properties:

```xaml
<shell:MiniMicaWindow
    Width="420"
    Height="560"
    ResizeMode="NoResize"
    ShowInTaskbar="False"
    WindowStartupLocation="Manual">
```

Then place it after loading:

```csharp
Loaded += delegate
{
    WindowPlacementService.Place(
        this,
        WorkAreaPlacement.BottomLeft,
        new Thickness(12));
};
```

This replaces hand-written primary-screen and DPI math.

Do not make the window topmost unless the product requirement truly needs it. If temporary foreground activation is necessary, isolate that behavior in a separate windowing helper instead of embedding it in every view.
