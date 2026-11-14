using Kopolart.Windows.UI.Helpers;
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
        }

        /// <summary>
        /// Switches the dialog theme at runtime.
        /// </summary>
        public void ApplyDarkMode(bool isDark)
        {
            IsDark = isDark;
            if (!IsHandleCreated) return;

            NativeMethods.SetImmersiveDarkMode(Handle, isDark);
            ApplyThemeColors(isDark);
        }

        protected abstract void ApplyThemeColors(bool isDark);
    }
}
