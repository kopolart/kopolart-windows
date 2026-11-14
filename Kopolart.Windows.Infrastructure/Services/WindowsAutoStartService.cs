using Kopolart.Windows.Application.Interfaces;
using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class WindowsAutoStartService : IAutoStartService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppKeyName = "Kopolart";

        public bool IsAutoStartConfigured()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(AppKeyName) != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read registry for auto-start: {ex.Message}");
                return false;
            }
        }

        public void SetAutoStart(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key == null)
                    return;

                if (enabled)
                {
                    string? appPath = Helpers.AppPathHelper.GetTargetExecutablePath();
                    if (!string.IsNullOrEmpty(appPath))
                    {
                        key.SetValue(AppKeyName, $"\"{appPath}\" --autostart");
                    }
                }
                else
                {
                    key.DeleteValue(AppKeyName, throwOnMissingValue: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set registry for auto-start: {ex}");
            }
        }
    }
}
