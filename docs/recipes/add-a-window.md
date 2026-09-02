# Recipe: add a window

Create `Views/SettingsWindow.xaml` with `shell:MiniMicaWindow` as the root element and derive the code-behind from `MiniMicaWindow`.

```csharp
public partial class SettingsWindow : MiniMicaWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        ThemePreference = AppOptions.DefaultTheme;
        Backdrop = BackdropKind.Mica;
    }
}
```

Use a transparent root panel if you want the system backdrop visible. Put readable content on semantic surfaces such as `MiniMica.CardStyle`.

Avoid copying `OnSourceInitialized`, DWM P/Invoke, or theme-message hooks into the new window.
