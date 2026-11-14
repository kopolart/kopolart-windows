using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Helper to enforce consistent dialog styles across the application:
    /// - ShowInTaskbar = false (keeps Taskbar and Task Manager clean, grouped under main app)
    /// - ShowIcon = true (displays Kopolart icon in the dialog title bar)
    /// - Sets the official Kopolart icon on the dialog window
    /// </summary>
    public static class DialogHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private static Icon? _cachedAppIcon;
        private static readonly object _iconLock = new();

        public static void ApplyDialogStyle(Form form)
        {
            if (form == null) return;

            form.ShowInTaskbar = false;
            form.ShowIcon = true;

            ApplyIcon(form);
            form.Load += (s, e) => ApplyIcon(form);
        }

        public static void ApplyIcon(Form form)
        {
            try
            {
                if (form.Owner?.Icon != null)
                {
                    form.Icon = form.Owner.Icon;
                    return;
                }

                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    foreach (Form openForm in System.Windows.Forms.Application.OpenForms)
                    {
                        if (openForm != form && openForm.Icon != null)
                        {
                            form.Icon = openForm.Icon;
                            return;
                        }
                    }
                }

                lock (_iconLock)
                {
                    if (_cachedAppIcon == null)
                    {
                        var bitmap = Properties.Resources.kopolart_icon;
                        if (bitmap != null)
                        {
                            var hIcon = bitmap.GetHicon();
                            try
                            {
                                _cachedAppIcon = (Icon)Icon.FromHandle(hIcon).Clone();
                            }
                            finally
                            {
                                DestroyIcon(hIcon);
                            }
                        }
                    }

                    if (_cachedAppIcon != null)
                    {
                        form.Icon = _cachedAppIcon;
                    }
                }
            }
            catch
            {
                // Fallback silently if icon cannot be set
            }
        }
    }
}
