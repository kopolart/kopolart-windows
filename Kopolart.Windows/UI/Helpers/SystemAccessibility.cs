using Kopolart.Windows.UI.Constants;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Single source of truth for the Windows visual/accessibility settings that this
    /// hand-themed UI has to honor: text scale, high contrast, accent color and keyboard cues.
    /// Raises <see cref="Changed"/> when the user changes any of them in Windows Settings.
    /// </summary>
    public static class SystemAccessibility
    {
        private static SystemVisualSettings? _settings;
        private static bool _subscribed;

        public static event EventHandler<SystemVisualSettingsChangedEventArgs>? Changed;

        public static SystemVisualSettings Settings =>
            _settings ??= System.Windows.Forms.Application.SystemVisualSettings;

        public static bool IsHighContrast => Settings.HighContrastEnabled;

        public static bool AreKeyboardCuesVisible => Settings.KeyboardCuesVisible;

        public static float TextScaleFactor => Settings.TextScaleFactor;

        /// <summary>
        /// Zoom factor for WebView2. Windows text scale starts at 1.0; the content zoom is
        /// capped so the page stays usable on unusually large accessibility settings.
        /// </summary>
        public static float WebViewZoomFactor
        {
            get
            {
                float scale = TextScaleFactor;
                if (float.IsNaN(scale) || scale <= 0f)
                {
                    return 1f;
                }

                return Math.Min(2f, Math.Max(1f, scale));
            }
        }

        /// <summary>
        /// Width of the system focus rectangle, used by custom-painted focus rings.
        /// </summary>
        public static int FocusBorderWidth
        {
            get
            {
                int width = Settings.FocusBorderMetrics.Width;
                return width <= 0 ? 1 : width;
            }
        }

        public static Color GetAccent(Color fallback)
        {
            Color accent = Settings.AccentColor;
            return accent.IsEmpty ? fallback : accent;
        }

        public static Color AccentColor => GetAccent(UIConstants.BrandPrimary);

        /// <summary>
        /// Reads the current settings and starts listening for changes.
        /// Call once, on the UI thread, before the message loop starts.
        /// </summary>
        public static void Initialize()
        {
            _settings = System.Windows.Forms.Application.SystemVisualSettings;

            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            System.Windows.Forms.Application.SystemVisualSettingsChanged += OnSystemVisualSettingsChanged;
        }

        private static void OnSystemVisualSettingsChanged(object? sender, SystemVisualSettingsChangedEventArgs e)
        {
            _settings = e.NewSettings;
            Changed?.Invoke(null, e);
        }
    }
}
