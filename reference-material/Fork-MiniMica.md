# Fork MiniMica



### Step by Step

* Copy or fork the entire `MiniMica` project folder into your solution.
  * You may change the project name and folder.
  * ~~The default namespace is `MiniMica`.  You may keep it is as is or rename it if desired.~~
  * The new project is ready to compile and run immediately.
* Locate all the placeholders `Contoso` in the project, and update to your program name.
  * Also update the app icon and images
* Port the existing OnePager window to MainControl.xaml.  The canvas sizes are 960x640.
* To support light/dark themes, update the `UpdateTheme()` method behind each window.
* Must change `[assembly: AssemblyTitle("MiniMica")]`  to desired product name; otherwise `MiniMica` is seen at the right-click menu on taskbar.



### Localization

* Open the utility `CUA\i18n`.
* Make a copy of `C:\V4N\CUA\i18n\i18n-resx` for the new project.
* Edit `worksheet.csv` and add all ResourceID and English content.
  * The spreadsheet already contains translations for the Settings dialog.
  * Do not delete any columns if not translated.
* Add translated strings from partner.
  * For translations with Gemini, go to `C:\V4N\CUA\doc` and use `i18n prompt template.md` to create the prompts.  Fill `i18n worksheet.csv` and feed them into Gemini 2.5 Pro.  The output is `worksheet.csv`.
* Run `i18n` utility and generate all the `.resx` files.
* Copy and add the `.resx` files into the working project.  Only include the languages actually translated.
* If new strings were added, make sure forcing Visual Studio to run the code generator and create `Strings.Designer.cs`.
  * In the Solution Explorer, find your `Strings.resx` file and right click.
  * From the context menu, select "Run Custom Tool".
* If the localized the languages are less than 22, keep the translated DLLs only.

```
Updated:
For ResX Manager, the most reliable path is:
* Export from ResX to Excel
* Translate in Excel
* Import back into ResX
```



