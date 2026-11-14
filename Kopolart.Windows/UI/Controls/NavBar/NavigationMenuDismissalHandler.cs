using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls.NavBar
{
    /// <summary>
    /// Handles outside-click detection via low-level mouse hooks and closes open dropdown menus,
    /// returning focus to the web view when a click occurs outside active menus.
    /// </summary>
    public sealed class NavigationMenuDismissalHandler : IDisposable
    {
        private readonly MenuStrip _menuStrip;
        private readonly Form _parentForm;
        private readonly Action _focusWebViewAction;

        private IntPtr _mouseHookHandle = IntPtr.Zero;
        private LowLevelMouseProc? _mouseHookProc;
        private bool _isDisposed;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_NCRBUTTONDOWN = 0x00A4;
        private const int WM_NCMBUTTONDOWN = 0x00A7;

        public NavigationMenuDismissalHandler(
            MenuStrip menuStrip,
            Form parentForm,
            Action focusWebViewAction)
        {
            _menuStrip = menuStrip ?? throw new ArgumentNullException(nameof(menuStrip));
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _focusWebViewAction = focusWebViewAction ?? throw new ArgumentNullException(nameof(focusWebViewAction));

            _menuStrip.MenuActivate += (s, e) => InstallMouseHook();
            _menuStrip.MenuDeactivate += (s, e) =>
            {
                if (!HasAnyOpenMenu())
                {
                    UninstallMouseHook();
                }
            };

            WireDropDownEvents(_menuStrip.Items);

            _menuStrip.ItemAdded += (s, e) =>
            {
                if (e.Item is ToolStripDropDownItem dropDownItem)
                {
                    WireDropDownItemEvents(dropDownItem);
                }
            };

            _parentForm.Deactivate += ParentForm_Deactivate;
        }

        public void WireDropDownEvents(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripDropDownItem dropDownItem)
                {
                    WireDropDownItemEvents(dropDownItem);
                }
            }
        }

        public void WireDropDownItemEvents(ToolStripDropDownItem item)
        {
            item.DropDownOpened += (s, e) => InstallMouseHook();
            item.DropDownClosed += (s, e) =>
            {
                if (!HasAnyOpenMenu())
                {
                    UninstallMouseHook();
                }
            };

            if (item.HasDropDownItems)
            {
                WireDropDownEvents(item.DropDownItems);
            }
        }

        public void CloseAllMenus()
        {
            if (_isDisposed || _menuStrip.IsDisposed)
                return;

            if (_menuStrip.InvokeRequired)
            {
                try
                {
                    _menuStrip.BeginInvoke(new Action(CloseAllMenus));
                }
                catch (ObjectDisposedException)
                {
                }
                return;
            }

            try
            {
                bool hadOpenMenu = false;
                foreach (ToolStripItem item in _menuStrip.Items)
                {
                    if (item is ToolStripDropDownItem dropDownItem && dropDownItem.DropDown.Visible)
                    {
                        CloseDropDownRecursive(dropDownItem);
                        hadOpenMenu = true;
                    }
                }

                if (hadOpenMenu)
                {
                    _focusWebViewAction();
                }
            }
            finally
            {
                UninstallMouseHook();
            }
        }

        private static void CloseDropDownRecursive(ToolStripDropDownItem item)
        {
            if (item.HasDropDownItems)
            {
                foreach (ToolStripItem subItem in item.DropDownItems)
                {
                    if (subItem is ToolStripDropDownItem subDropDownItem && subDropDownItem.DropDown.Visible)
                    {
                        CloseDropDownRecursive(subDropDownItem);
                    }
                }
            }

            if (item.DropDown.Visible)
            {
                item.DropDown.Close(ToolStripDropDownCloseReason.AppClicked);
            }
        }

        private void ParentForm_Deactivate(object? sender, EventArgs e)
        {
            CloseAllMenus();
        }

        private void InstallMouseHook()
        {
            if (_isDisposed || _menuStrip.IsDisposed)
                return;

            if (_mouseHookHandle != IntPtr.Zero)
                return;

            try
            {
                _mouseHookProc ??= MouseHookCallback;
                IntPtr hMod = GetModuleHandle(null);
                _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, hMod, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to install mouse hook: {ex.Message}");
            }
        }

        private void UninstallMouseHook()
        {
            if (_mouseHookHandle != IntPtr.Zero)
            {
                try
                {
                    UnhookWindowsHookEx(_mouseHookHandle);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to uninstall mouse hook: {ex.Message}");
                }
                _mouseHookHandle = IntPtr.Zero;
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    int msg = wParam.ToInt32();
                    if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN ||
                        msg == WM_NCLBUTTONDOWN || msg == WM_NCRBUTTONDOWN || msg == WM_NCMBUTTONDOWN)
                    {
                        if (HasAnyOpenMenu())
                        {
                            var pt = Cursor.Position;
                            if (!IsPointOverActiveMenu(pt))
                            {
                                CloseAllMenus();
                            }
                        }
                        else
                        {
                            UninstallMouseHook();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in mouse hook callback: {ex.Message}");
                }
            }

            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        public bool HasAnyOpenMenu()
        {
            if (_menuStrip == null || _menuStrip.IsDisposed)
                return false;

            foreach (ToolStripItem item in _menuStrip.Items)
            {
                if (item is ToolStripDropDownItem dropDownItem && dropDownItem.DropDown.Visible)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPointOverActiveMenu(Point screenPoint)
        {
            if (_menuStrip != null && !_menuStrip.IsDisposed)
            {
                if (_menuStrip.Visible && _menuStrip.RectangleToScreen(_menuStrip.ClientRectangle).Contains(screenPoint))
                {
                    return true;
                }

                foreach (ToolStripItem item in _menuStrip.Items)
                {
                    if (item is ToolStripDropDownItem dropDownItem && dropDownItem.DropDown.Visible)
                    {
                        if (IsPointOverDropDownRecursive(dropDownItem.DropDown, screenPoint))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsPointOverDropDownRecursive(ToolStripDropDown dropDown, Point screenPoint)
        {
            if (dropDown != null && !dropDown.IsDisposed && dropDown.Visible)
            {
                if (dropDown.Bounds.Contains(screenPoint))
                {
                    return true;
                }

                foreach (ToolStripItem item in dropDown.Items)
                {
                    if (item is ToolStripDropDownItem subDropDownItem && subDropDownItem.DropDown.Visible)
                    {
                        if (IsPointOverDropDownRecursive(subDropDownItem.DropDown, screenPoint))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                UninstallMouseHook();
                if (_parentForm != null && !_parentForm.IsDisposed)
                {
                    _parentForm.Deactivate -= ParentForm_Deactivate;
                }
                _isDisposed = true;
            }
        }
    }
}
