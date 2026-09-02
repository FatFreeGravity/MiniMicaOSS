using System;
using System.Windows.Markup;

namespace MiniMicaApp.Localization
{
    /// <summary>
    /// XAML shorthand: Text="{i18n:Loc Key=settings_appearance}".
    /// Resources are resolved when XAML is loaded. MiniMica therefore recommends
    /// applying a user-selected UI language at startup and restarting after a
    /// language change rather than introducing a binding framework into the starter.
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class LocExtension : MarkupExtension
    {
        public LocExtension()
        {
        }

        public LocExtension(string key)
        {
            Key = key;
        }

        [ConstructorArgument("key")]
        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Strings.Get(Key);
        }
    }
}
