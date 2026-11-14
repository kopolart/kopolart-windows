using System.Drawing;

namespace Kopolart.Windows.UI.Constants
{
    public static class UIConstants
    {
        public const string AppName = "Kopolart";

        // KopolArt Brand Backgrounds (matching mobile app BrandBackground #E2CDB4)
        public const string BrandBackgroundHex = "#e2cdb4";
        public static readonly Color BrandBackground = ColorTranslator.FromHtml(BrandBackgroundHex);

        public const string ThemeColorHex = "#e2cdb4";
        public static readonly Color ThemeColor = ColorTranslator.FromHtml(ThemeColorHex);
        public static readonly Color TextThemeColor = Color.FromArgb(74, 59, 42);

        // Brand Primary Brown & Light/Dark variants (matching main.css & dark-mode.css)
        public const string BrandPrimaryHex = "#865231";
        public static readonly Color BrandPrimary = ColorTranslator.FromHtml(BrandPrimaryHex);
        public const string BrandPrimaryDarkHex = "#c8a582";
        public static readonly Color BrandPrimaryDark = ColorTranslator.FromHtml(BrandPrimaryDarkHex);
        public const string BrandPrimaryLightHex = "#fcd8ae";
        public static readonly Color BrandPrimaryLight = ColorTranslator.FromHtml(BrandPrimaryLightHex);

        // Navigation bar & window light background (matching mobile app BrandBackground #E2CDB4)
        public const string NavBarLightBgHex = "#e2cdb4";
        public static readonly Color NavBarLightBg = ColorTranslator.FromHtml(NavBarLightBgHex);

        // Card & Items Background and Border (matching mobile app CardBackgroundLight #FFFDF9 & CardBorderLight #E4D5C3)
        public const string CardBackgroundLightHex = "#FFFDF9";
        public static readonly Color CardBackgroundLight = ColorTranslator.FromHtml(CardBackgroundLightHex);
        public const string CardBorderLightHex = "#E4D5C3";
        public static readonly Color CardBorderLight = ColorTranslator.FromHtml(CardBorderLightHex);
        public const string CardBackgroundHoverLightHex = "#FFFCFA";
        public static readonly Color CardBackgroundHoverLight = ColorTranslator.FromHtml(CardBackgroundHoverLightHex);

        // Project Order & Alerts / Danger
        public static readonly Color ProjectOrderColor = ColorTranslator.FromHtml("#c42525");
        public const string DangerColorHex = "#de4215";
        public static readonly Color DangerColor = ColorTranslator.FromHtml(DangerColorHex);
        public static readonly Color AlertDotColor = ColorTranslator.FromHtml(DangerColorHex);

        // Dark Red (consistent red color for logged-in user card border, matching mobile app)
        public const string DarkRedHex = "#C62828";
        public static readonly Color DarkRed = ColorTranslator.FromHtml(DarkRedHex);
        public const string DarkRedDarkHex = "#EF5350";
        public static readonly Color DarkRedDark = ColorTranslator.FromHtml(DarkRedDarkHex);

        // Brand Slate Blue (original and 7% lighter in dark mode)
        public static readonly Color BrandSlateBlue = Color.FromArgb(83, 94, 123);
        public static readonly Color BrandSlateBlueDark = Color.FromArgb(98, 111, 143);

        // Luxury Gold Brand Color for Bookmarks
        public const string BrandGoldHex = "#D4AF37";
        public static readonly Color BrandGold = ColorTranslator.FromHtml(BrandGoldHex);

        // Online Users Live Green (matching mobile app LiveGreen)
        public static readonly Color LiveGreenLight = Color.FromArgb(22, 163, 74); // #16A34A
        public static readonly Color LiveGreenDark = Color.FromArgb(34, 197, 94); // #22C55E
        public static readonly Color LiveGreenFadedLight = Color.FromArgb(90, 22, 163, 74);
        public static readonly Color LiveGreenFadedDark = Color.FromArgb(90, 34, 197, 94);

        // Light mode colors
        public static class LightMode
        {
            public static readonly Color MenuHighlight = Color.FromArgb(218, 195, 168);
            public static readonly Color MenuBorder = Color.FromArgb(228, 213, 195);
            public static readonly Color MenuButtonPressed = Color.FromArgb(210, 185, 155);
            public static readonly Color TextDisabledColor = Color.Gray;
        }

        // Loader Colors
        public static readonly Color LoaderWarningColor = ColorTranslator.FromHtml("#444444");
        public static readonly Color LoaderLabelColor = ColorTranslator.FromHtml("#555555");
        public static readonly Color LoaderButtonBackColor = Color.FromArgb(60, 60, 60);
        public static readonly Color LoaderButtonForeColor = Color.White;

        // Default window dimensions (designer defaults)
        public const int DefaultWindowWidth = 984;
        public const int DefaultWindowHeight = 561;

        // Animated dot colors (warm palette)
        public static readonly Color DotColor1 = ColorTranslator.FromHtml("#c49a6c");
        public static readonly Color DotColor2 = ColorTranslator.FromHtml("#b07d4f");
        public static readonly Color DotColor3 = ColorTranslator.FromHtml("#d4a574");

        // Offline page colors (derived from theme)
        public const string OfflinePageBgHex = "#e2cdb4";
        public const string OfflinePageTextHex = "#555";
        public const string OfflinePageHeadingHex = "#333";
        public const string OfflinePageSubtextHex = "#777";
        public const string OfflinePageButtonTextHex = "#4a3b2a";
        public static string OfflinePageShadowRgba(double alpha) =>
            $"rgba({ThemeColor.R}, {ThemeColor.G}, {ThemeColor.B}, {alpha})";

        // Dark mode
        public static class DarkMode
        {
            public const string CookieName = "darkmode-kopolart";
            public const string CookieValue = "dark";

            // KopolArt Brand Dark Background (matching mobile app BrandBackgroundDark #403E3B)
            public const string BrandBackgroundDarkHex = "#403E3B";
            public static readonly Color BrandBackground = ColorTranslator.FromHtml(BrandBackgroundDarkHex);

            public const string ThemeColorHex = "#a3795f";
            public static readonly Color ThemeColor = ColorTranslator.FromHtml(ThemeColorHex);

            // Navigation overlay: dark background
            public static readonly Color OverlayBackColor = ColorTranslator.FromHtml("#312f2d");

            // Menu strip & App window dark background (matching mobile app BrandBackgroundDark #403E3B)
            public const string MenuStripBackColorHex = "#403E3B";
            public static readonly Color MenuStripBackColor = ColorTranslator.FromHtml(MenuStripBackColorHex);
            public static readonly Color MenuStripForeColor = ColorTranslator.FromHtml("#e0e0e0");
            public static readonly Color MenuHighlight = Color.FromArgb(77, 74, 71);
            public static readonly Color MenuBorder = Color.FromArgb(77, 74, 71);
            public static readonly Color MenuButtonPressed = Color.FromArgb(52, 50, 48);
            public static readonly Color TextDisabledColor = Color.FromArgb(120, 120, 120);

            // Card & Items Background and Border (matching mobile app CardBackgroundDark #2C2A28 & CardBorderDark #4D4A47)
            public const string CardBackgroundDarkHex = "#2C2A28";
            public static readonly Color CardBackgroundDark = ColorTranslator.FromHtml(CardBackgroundDarkHex);
            public const string CardBorderDarkHex = "#4D4A47";
            public static readonly Color CardBorderDark = ColorTranslator.FromHtml(CardBorderDarkHex);
            public const string CardBackgroundHoverDarkHex = "#3A3835";
            public static readonly Color CardBackgroundHoverDark = ColorTranslator.FromHtml(CardBackgroundHoverDarkHex);

            // Dark mode brand colors
            public static readonly Color BrandPrimary = BrandPrimaryDark;
            public static readonly Color BrandPrimaryLight = UIConstants.BrandPrimaryLight;
            public const string DangerColorHex = "#ff5c38";
            public static readonly Color DangerColor = ColorTranslator.FromHtml(DangerColorHex);

            // Animated dot colors (warm dark palette)
            public static readonly Color DotColor1 = ColorTranslator.FromHtml("#a3795f");
            public static readonly Color DotColor2 = ColorTranslator.FromHtml("#8b6550");
            public static readonly Color DotColor3 = ColorTranslator.FromHtml("#b8907a");
        }
    }
}
