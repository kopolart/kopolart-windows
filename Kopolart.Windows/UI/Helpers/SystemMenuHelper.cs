using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    public static class SystemMenuHelper
    {
        // P/Invoke Constants
        public const int WM_SYSCOMMAND = 0x0112;
        public const int StartMaximizedSysMenuId = 0x1FF0;
        public const int ShowNavLoadingSysMenuId = 0x1FE0;

        private const int MF_STRING = 0x0000;
        private const int MF_SEPARATOR = 0x0800;
        private const int MF_CHECKED = 0x0008;
        private const int MF_UNCHECKED = 0x0000;
        private const int MF_BYCOMMAND = 0x0000;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIdNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern int CheckMenuItem(IntPtr hMenu, int uIdCheckItem, int uCheck);


        public static void InitializeSystemMenu(Form? form, bool isStartMaximized, bool showNavLoading)
        {
            if (form == null || form.IsDisposed || !form.IsHandleCreated)
            {
                return;
            }

            // Revert system menu to default state to clear previous additions
            GetSystemMenu(form.Handle, true);

            var systemMenuHandle = GetSystemMenu(form.Handle, false);
            if (systemMenuHandle == IntPtr.Zero)
            {
                return;
            }

            string startMaximizedText = TextManager.GetString("SysMenuStartMaximized");
            string showNavLoadingText = TextManager.GetString("SysMenuShowNavLoading");

            AppendMenu(systemMenuHandle, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(systemMenuHandle, MF_STRING, StartMaximizedSysMenuId, startMaximizedText);
            AppendMenu(systemMenuHandle, MF_STRING, ShowNavLoadingSysMenuId, showNavLoadingText);

            UpdateStartMaximizedMenuCheck(form, isStartMaximized);
            UpdateNavLoadingMenuCheck(form, showNavLoading);
        }

        public static void UpdateStartMaximizedMenuCheck(Form form, bool enabled)
        {
            if (form is not { IsHandleCreated: true }) return;

            var systemMenuHandle = GetSystemMenu(form.Handle, false);
            if (systemMenuHandle == IntPtr.Zero)
            {
                return;
            }

            CheckMenuItem(
                systemMenuHandle,
                StartMaximizedSysMenuId,
                MF_BYCOMMAND | (enabled ? MF_CHECKED : MF_UNCHECKED));
        }

        /// <summary>
        /// Toggles the Start Maximized state visually and returns the new value.
        /// Caller is responsible for persisting the value.
        /// </summary>
        public static bool ToggleStartMaximized(Form form, bool currentValue)
        {
            var enabled = !currentValue;
            UpdateStartMaximizedMenuCheck(form, enabled);

            if (enabled)
            {
                if (form.WindowState == FormWindowState.Minimized)
                {
                    form.WindowState = FormWindowState.Normal;
                }

                form.WindowState = FormWindowState.Maximized;
                form.Activate();
            }

            return enabled;
        }

        public static void UpdateNavLoadingMenuCheck(Form form, bool enabled)
        {
            if (form is not { IsHandleCreated: true }) return;

            var systemMenuHandle = GetSystemMenu(form.Handle, false);
            if (systemMenuHandle == IntPtr.Zero) return;

            CheckMenuItem(
                systemMenuHandle,
                ShowNavLoadingSysMenuId,
                MF_BYCOMMAND | (enabled ? MF_CHECKED : MF_UNCHECKED));
        }

        /// <summary>
        /// Toggles the Show Nav Loading state visually and returns the new value.
        /// Caller is responsible for persisting the value.
        /// </summary>
        public static bool ToggleShowNavLoading(Form form, bool currentValue)
        {
            var enabled = !currentValue;
            UpdateNavLoadingMenuCheck(form, enabled);
            return enabled;
        }
    }
}


