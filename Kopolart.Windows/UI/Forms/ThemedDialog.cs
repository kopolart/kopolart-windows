using Kopolart.Windows.UI.Helpers;
using System;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    /// <summary>
    /// Base form for application dialogs. Owns the light/dark state, centralizes the
    /// Windows immersive dark title bar, and delegates color assignments to
    /// <see cref="ApplyThemeColors"/>.
    /// </summary>
    public abstract class ThemedDialog : Form
    {
        protected ThemedDialog(bool isDark)
        {
            IsDark = isDark;
            DialogHelper.ApplyDialogStyle(this);
        }

        protected bool IsDark { get; private set; }

        /// <summary>
        /// Applies the initial theme. Call once after the controls have been created.
        /// </summary>
        protected void InitializeTheme()
        {
            NativeMethods.SetImmersiveDarkMode(Handle, IsDark);
            ApplyThemeColors(IsDark);
            HighContrastTheme.Apply(this);
        }

        /// <summary>
        /// Switches the dialog theme at runtime.
        /// </summary>
        public void ApplyDarkMode(bool isDark)
        {
            IsDark = isDark;
            if (!IsHandleCreated) return;

            NativeMethods.SetImmersiveDarkMode(Handle, isDark);
            HighContrastTheme.Restore(this);
            ApplyThemeColors(isDark);
            HighContrastTheme.Apply(this);
            Invalidate(true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SystemAccessibility.Changed += OnSystemAccessibilityChanged;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            SystemAccessibility.Changed -= OnSystemAccessibilityChanged;
            base.OnHandleDestroyed(e);
        }

        private void OnSystemAccessibilityChanged(object? sender, SystemVisualSettingsChangedEventArgs e)
        {
            ApplyDarkMode(IsDark);
        }

        protected abstract void ApplyThemeColors(bool isDark);
    }
}
