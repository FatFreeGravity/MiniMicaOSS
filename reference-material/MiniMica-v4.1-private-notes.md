# MiniMica

*August 2025*



This application is an excellent example of a custom-themed WPF window that mimics the modern **Mica effect** and behaviors found in Windows 11 applications. It achieves this by forgoing the standard Windows title bar and creating its own custom window chrome, complete with custom-styled buttons and theme-awareness.



### ## File Breakdown

The project consists of three main parts, each defined in separate files:

1. **`MiniMicaWindow.xaml` / `MiniMicaWindow.xaml.cs`**: This is the main window of the application. The XAML file defines the structure, including the custom title bar, buttons, and content area1111111. The C# code-behind handles all the logic, such as button clicks, window dragging, pseudo-maximization, and theme changes.
2. **`MiniMicaControl.xaml` / `MiniMicaControl.xaml.cs`**: This is a `UserControl` that represents the main content area of the application2. Placing the UI in a separate control keeps the main window's code cleaner and more organized.
3. **`MiniMicaTheme.xaml`**: This is a `ResourceDictionary` containing all the styles, colors, and brushes used throughout the application3. It defines colors for light and dark modes, button hover effects, and base styles for the window controls4444.



### ## Core Features and Implementation

Here’s how the key features are implemented:

#### ### Custom Window Chrome

- **No Standard Title Bar**: The window is configured with `WindowStyle="None"` to remove the default operating system title bar and border5.
- **Custom Controls**: The title bar area is a `Grid` containing an icon, title `TextBlock`, and three custom-styled `Button` elements for minimize, maximize, and close actions6.
- **WindowChrome Class**: The `<WindowChrome.WindowChrome>` object is used to re-enable standard window behaviors like resizing from the edges (`ResizeBorderThickness="8"`) and displaying a drop shadow (`GlassFrameThickness="1"`) that are lost when `WindowStyle` is set to `None`7.

#### ### Mica Effect & Theming

- **Simulated Mica**: A true Mica effect isn't available directly in WPF. This app simulates it by using solid background colors defined in `MiniMicaTheme.xaml` (`MicaBackground_L` for light theme, `MicaBackground_D` for dark)8.
- **Theme Detection**: The `UpdateTheme()` method in `MiniMicaWindow.xaml.cs` reads a Windows registry key to detect if the user's system is set to light or dark mode. It then applies the appropriate styles and colors from the resource dictionary.
- **Dynamic Styling**: When the app is activated or deactivated, the `OnActivated` and `OnDeactivated` event handlers are triggered9. They update the foreground color of the title and the style of the caption buttons to reflect the window's state (e.g., using dimmer colors when inactive).

#### ### Custom Maximization Logic

Standard maximization doesn't work well with custom title bars. This app implements a "pseudo-maximization" feature:

- **`TogglePseudoMaximize()` Method**: Instead of changing the `WindowState`, this method manually calculates the screen's working area (excluding the taskbar) and resizes/repositions the window to fill it.
- **State Management**: It saves the window's original size and position in a `_restoreBounds` variable before maximizing, so it can restore it accurately.
- **Drag to Restore**: The `OnMouseMove` event handler contains logic to detect when a user clicks and drags the title bar from a maximized state. After the mouse moves a certain distance, it restores the window to its previous size and initiates a standard window drag operation.

This project is a well-structured demonstration of how to create a modern, visually appealing WPF application that integrates nicely with the Windows 11 design language.



## Language Breakdown

Top languages as of August 2025:

| Language                                                 | # of Languages | Cumulative Coverage |
| -------------------------------------------------------- | -------------- | ------------------- |
| en                                                       | Top 1          | 48%                 |
| de, fr, es/es-ES, pt/pt-PT, zh/zh-CN, it, ru, uk, nl, pl | Middle 13      | 95%                 |
| sv, da, nb, fi, ja, ko, cs, tr (high per-capita)         | Niche 8        | 99%                 |
| th, id, vi (large population, little usage)              | ASEAN 3        |                     |
| ar (right-to-left)                                       |                |                     |

==**To reach 95-99% of the global audience, apps should generally be localized into at least 14 languages and no more than 22.  This may be extended to 25 if the ASEAN market is a priority.**==



==**IMPORTANT: Do NOT use Visual Studio RESX editor.  Do NOT use the UNDO function specifically.  Always use a spreadsheet and copy into the RESX editor, or edit the .resx/XML files directly.  Recommend VS extension: ResX Resource Manager by Tom Englert**==



## Future Consideration: Merging Localization DLLs into Main .EXE

Let's switch to a robust, manual method that gives you complete control. This approach involves embedding the language DLLs directly into your project as resources and then adding a small piece of code to tell your application how to find them at runtime. This method has no external tool dependencies.



### Manual Method: Embedding and Resolving Assemblies

This is a two-part process: first, we'll embed the compiled language files into your project, and second, we'll add the code to load them.



### 1. Embed the Language DLLs

1. **Get the DLLs**: **Rebuild** your solution. This will generate the localization folders (e.g., `bin\Debug\es`, `bin\Debug\zh-CN`, etc.).
2. **Create a Project Folder**: In Visual Studio's **Solution Explorer**, create a new folder in your project named `EmbeddedAssemblies`.
3. **Copy and Rename DLLs**: For each language folder (e.g., `es`), find the `MiniMica.resources.dll` file inside it.
   - Copy that file into the `EmbeddedAssemblies` folder you just created.
   - **Important**: Rename the file to include its language code to avoid name conflicts. For example:
     - `es\MiniMica.resources.dll` → `es.resources.dll`
     - `zh-CN\MiniMica.resources.dll` → `zh-CN.resources.dll`
4. **Include and Embed in Project**:
   - In Solution Explorer, right-click the `EmbeddedAssemblies` folder and select **Add > Existing Item...**. Select all the renamed `.dll` files.
   - Select all the newly added DLLs in the Solution Explorer.
   - In the **Properties** window, change their **Build Action** to **Embedded Resource**.



### 2. Add the Assembly-Loading Code

Now, we'll add the code that finds and loads these embedded DLLs when the application needs them.

- Open your **`App.xaml.cs`** file and add the following code. The new code is in the `App()` constructor and the `Resolver` method.

C#

```
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;

namespace MiniMica
{
    public partial class App : Application
    {
        public App()
        {
            // This event is triggered when the runtime fails to find an assembly.
            // We're hooking into it to load our embedded language DLLs.
            AppDomain.CurrentDomain.AssemblyResolve += Resolver;
        }

        // This method handles the assembly resolution.
        private static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            // Get the name of the assembly that failed to load.
            var requestedAssembly = new AssemblyName(args.Name);

            // We're only interested in our satellite assemblies.
            // Their names end with ".resources".
            if (!requestedAssembly.Name.EndsWith(".resources"))
            {
                return null;
            }

            // Get the culture code (e.g., "es", "zh-CN") from the assembly name.
            string cultureName = requestedAssembly.CultureName;

            // Build the name of the embedded resource.
            // Format: YourProjectName.FolderName.CultureCode.resources.dll
            string resourceName = $"MiniMica.EmbeddedAssemblies.{cultureName}.resources.dll";

            // Get the current assembly (your main .exe).
            var currentAssembly = Assembly.GetExecutingAssembly();

            // Load the embedded resource as a stream.
            using (var stream = currentAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                // Read the stream into a byte array.
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);

                // Load the byte array as an assembly and return it.
                return Assembly.Load(assemblyData);
            }
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            // Your existing startup code...
            Global.appName = "Contoso";
            // ... etc.
        }
    }
}
```

After completing these steps, **Clean and Rebuild** your solution. The language folders will no longer be created, and your application will load all translations from within the main `.exe` file.



### About Rounded Corners and Contrast Themes

* Geometry in Windows https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/geometry
* Contrast themes https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes
* Apply rounded corners in desktop apps for Windows 11 https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners

