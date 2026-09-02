using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace MiniMicaApp.Localization
{
    /// <summary>
    /// Initializes the process UI culture before WPF creates application windows.
    /// No persistence policy is imposed on downstream applications.
    /// </summary>
    public static class LocalizationManager
    {
        /// <summary>Sentinel meaning "follow Windows", stored in the "language" setting.</summary>
        public const string SystemDefault = "00";

        /// <summary>
        /// Cultures offered by the hidden developer language picker, in the same order as
        /// the columns of tools/localization/worksheet.csv. A culture with no satellite
        /// assembly still selects correctly and falls back to neutral English - which is
        /// exactly what a localization tester needs to see.
        /// </summary>
        public static readonly string[] TestCultures =
        {
            "en-US", "zh-TW", "zh-CN", "nl-NL", "fr-FR", "de-DE", "it-IT", "pl-PL",
            "pt-BR", "pt-PT", "ru-RU", "es-MX", "es-ES", "uk-UA", "da-DK", "cs-CZ",
            "fi-FI", "ja-JP", "ko-KR", "nb-NO", "sv-SE", "tr-TR", "id-ID", "th-TH",
            "vi-VN"
        };

        public static CultureInfo CurrentUICulture
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
        }

        /// <summary>
        /// Layout numbers that may differ per culture, paired with the resource key each
        /// one feeds. A fixed canvas cannot reflow, and the same sentence is materially
        /// longer in some languages, so these absorb the difference.
        ///
        /// The values live in the metric_* rows of tools/localization/worksheet.csv
        /// alongside the translations: whoever notices that German overflows edits one
        /// spreadsheet cell instead of hunting through XAML. Blank cells inherit en-US
        /// through normal ResourceManager fallback.
        /// </summary>
        private static readonly string[][] Metrics = new string[][]
        {
            new[] { "metric_page_title_size",      "MiniMica.PageTitleFontSize" },
            new[] { "metric_page_subtitle_size",   "MiniMica.PageSubtitleFontSize" },
            new[] { "metric_feature_heading_size", "MiniMica.FeatureHeadingFontSize" },
            new[] { "metric_feature_body_size",    "MiniMica.FeatureBodyFontSize" },
            new[] { "metric_feature_body_width",   "MiniMica.FeatureBodyWidth" },
            new[] { "metric_action_button_size",   "MiniMica.ActionButtonFontSize" },
        };

        /// <summary>
        /// Pushes the current culture's layout metrics into a resource dictionary. Call
        /// after the UI culture is set and before windows are created. Values that are
        /// missing or unparseable are skipped, leaving the XAML baseline in place - a bad
        /// spreadsheet cell degrades one measurement, it does not break startup.
        /// </summary>
        public static void ApplyMetrics(ResourceDictionary resources)
        {
            if (resources == null)
            {
                return;
            }

            foreach (string[] metric in Metrics)
            {
                string raw = Strings.Get(metric[0]);
                double value;
                if (!string.IsNullOrWhiteSpace(raw)
                    && !string.Equals(raw, metric[0], StringComparison.Ordinal)
                    && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    && value > 0)
                {
                    resources[metric[1]] = value;
                }
            }
        }

        /// <summary>
        /// Applies a stored language setting: either <see cref="SystemDefault"/> or a
        /// culture name. An unknown culture falls back to Windows rather than throwing,
        /// so a hand-edited config cannot break startup.
        /// </summary>
        public static void ApplyStoredLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language)
                || string.Equals(language, SystemDefault, StringComparison.Ordinal))
            {
                Initialize();
                return;
            }

            try
            {
                Initialize(language.Trim(), true);
            }
            catch (CultureNotFoundException)
            {
                Initialize();
            }
        }

        /// <param name="cultureName">
        /// Null/empty means use Windows' current UI language. Otherwise pass a BCP-47
        /// culture such as "fr-FR", "ja-JP", or "zh-CN".
        /// </param>
        /// <param name="useForFormatting">
        /// When true, number/date formatting follows the selected UI culture too.
        /// Leave false when UI language and Windows regional format should remain separate.
        /// </param>
        public static void Initialize(string cultureName = null, bool useForFormatting = false)
        {
            CultureInfo uiCulture = string.IsNullOrWhiteSpace(cultureName)
                ? CultureInfo.CurrentUICulture
                : CultureInfo.GetCultureInfo(cultureName);

            Thread.CurrentThread.CurrentUICulture = uiCulture;

            if (useForFormatting)
            {
                Thread.CurrentThread.CurrentCulture = uiCulture;
                ApplyWpfLanguage(uiCulture);
            }
        }

        private static void ApplyWpfLanguage(CultureInfo culture)
        {
            // This metadata must be set before FrameworkElement instances are created.
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
        }
    }
}
