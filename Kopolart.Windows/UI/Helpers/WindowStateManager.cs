using Kopolart.Windows.Application.Interfaces;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    public static class WindowStateManager
    {
        private static Timer? _debounceTimer;
        private static Form? _pendingForm;
        private static ISettingsManager? _settingsManager;

        public static void SaveWindowState(Form form, ISettingsManager settingsManager)
        {
            _pendingForm = form;
            _settingsManager = settingsManager;

            if (_debounceTimer == null)
            {
                _debounceTimer = new Timer();
                _debounceTimer.Interval = 300;
                _debounceTimer.Tick += (s, e) =>
                {
                    _debounceTimer.Stop();
                    if (_pendingForm is { IsDisposed: false } && _settingsManager != null)
                    {
                        SaveWindowStateNow(_pendingForm, _settingsManager);
                    }
                };
            }

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        public static void Cleanup()
        {
            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();

                // Flush any pending save before disposing
                if (_pendingForm is { IsDisposed: false } && _settingsManager != null)
                {
                    SaveWindowStateNow(_pendingForm, _settingsManager);
                    _settingsManager.Flush();
                }

                _debounceTimer.Dispose();
                _debounceTimer = null;
            }
            _pendingForm = null;
            _settingsManager = null;
        }

        private static void SaveWindowStateNow(Form form, ISettingsManager settingsManager)
        {
            try
            {
                if (form.WindowState != FormWindowState.Minimized)
                {
                    settingsManager.SetWindowState((int)form.WindowState);
                }

                if (form.WindowState == FormWindowState.Normal)
                {
                    settingsManager.SetWindowLocation($"{form.Location.X},{form.Location.Y}");
                    settingsManager.SetWindowSize($"{form.Size.Width},{form.Size.Height}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save window state: {ex.Message}");
            }
        }

        public static void LoadWindowState(Form form, ISettingsManager settingsManager)
        {
            try
            {
                var locationValue = settingsManager.GetWindowLocation();
                var sizeValue = settingsManager.GetWindowSize();

                if (!string.IsNullOrEmpty(locationValue) && !string.IsNullOrEmpty(sizeValue))
                {
                    var locParts = locationValue.Split(',');
                    var sizeParts = sizeValue.Split(',');

                    if (locParts.Length == 2 && int.TryParse(locParts[0], out int x) && int.TryParse(locParts[1], out int y) &&
                        sizeParts.Length == 2 && int.TryParse(sizeParts[0], out int width) && int.TryParse(sizeParts[1], out int height))
                    {
                        var bounds = new Rectangle(x, y, width, height);
                        
                        // Check if the saved bounds interact with any screen
                        bool isOnScreen = false;
                        foreach (var screen in Screen.AllScreens)
                        {
                            if (screen.WorkingArea.IntersectsWith(bounds))
                            {
                                isOnScreen = true;
                                break;
                            }
                        }

                        if (isOnScreen)
                        {
                            form.StartPosition = FormStartPosition.Manual;
                            form.Location = bounds.Location;
                            form.Size = bounds.Size;
                        }
                    }
                }

                var state = (FormWindowState)settingsManager.GetWindowState();
                if (state != FormWindowState.Minimized)
                {
                    form.WindowState = state;
                }
                else
                {
                    form.WindowState = FormWindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load window state: {ex.Message}");
            }
        }

        public static void RestoreAndFocus(Form form, ISettingsManager settingsManager)
        {
            if (form == null || form.IsDisposed || !form.IsHandleCreated) return;

            if (form.WindowState == FormWindowState.Minimized)
            {
                var startMaximized = settingsManager.GetStartMaximized() ||
                                     settingsManager.GetWindowState() == (int)FormWindowState.Maximized;
                form.WindowState = startMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
            }

            form.Show();
            form.BringToFront();
            form.Activate();

            try
            {
                int showCmd = form.WindowState == FormWindowState.Maximized
                    ? NativeMethods.SW_SHOWMAXIMIZED
                    : NativeMethods.SW_RESTORE;

                NativeMethods.ShowWindow(form.Handle, showCmd);
                NativeMethods.SetForegroundWindow(form.Handle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restore and focus window: {ex}");
            }
        }
    }
}
