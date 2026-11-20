using Kopolart.Windows.Application.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Kopolart.Windows.UI.Helpers
{
    public static class DesktopShortcutManager
    {
        private const string ShortcutFileName = "kopolart.ir.lnk";
        private const string ShortcutDescription = "kopolart.ir";

        /// <summary>
        /// Unified entry point to ensure the desktop shortcut policy is enforced on first run or app update.
        /// </summary>
        public static void EnsureDesktopShortcut(ISettingsManager settingsManager)
        {
            try
            {
                var currentVersion = System.Windows.Forms.Application.ProductVersion ?? "";
                var lastVersion = settingsManager.GetLastAppVersion();
                var args = Environment.GetCommandLineArgs();
                bool isUpdatedArg = args is { Length: > 1 } && Array.Exists(args, arg => arg.Equals("--updated", StringComparison.OrdinalIgnoreCase));
                bool isVersionChanged = !string.IsNullOrWhiteSpace(lastVersion) && !string.Equals(lastVersion, currentVersion, StringComparison.OrdinalIgnoreCase);

                if (isUpdatedArg || isVersionChanged)
                {
                    EnsureDesktopShortcutOnUpdate(settingsManager);
                }
                else
                {
                    EnsureDesktopShortcutCreatedOnFirstRun(settingsManager);
                }

                if (!string.Equals(lastVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
                {
                    settingsManager.SetLastAppVersion(currentVersion);
                    settingsManager.Flush();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnsureDesktopShortcut failed: {ex}");
            }
        }

        /// <summary>
        /// Ensures that the desktop shortcut exists after an application update.
        /// If the shortcut was removed or is missing, it is recreated immediately.
        /// </summary>
        public static void EnsureDesktopShortcutOnUpdate(ISettingsManager settingsManager)
        {
            try
            {
                if (!settingsManager.GetKeepDesktopShortcut())
                {
                    Debug.WriteLine("Desktop shortcut recreation skipped as KeepDesktopShortcut is disabled.");
                    return;
                }

                Debug.WriteLine("Checking desktop shortcut upon application update...");
                if (!ShortcutExists())
                {
                    Debug.WriteLine("Desktop shortcut was missing after update. Recreating shortcut...");
                    if (TryCreateDesktopShortcut())
                    {
                        settingsManager.SetDesktopShortcutCreated(true);
                        settingsManager.Flush();
                        Debug.WriteLine("Desktop shortcut successfully recreated after update.");
                    }
                }
                else
                {
                    // Shortcut exists; ensure its target and icon are up-to-date with current executable
                    TryCreateDesktopShortcut();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Desktop shortcut check on update failed: {ex}");
            }
        }

        /// <summary>
        /// Ensures that the desktop shortcut exists on client desktop on first run.
        /// If the shortcut is missing or has never been created, it creates it and persists the state.
        /// </summary>
        public static void EnsureDesktopShortcutCreatedOnFirstRun(ISettingsManager settingsManager)
        {
            try
            {
                if (!settingsManager.GetKeepDesktopShortcut())
                {
                    return;
                }

                // If shortcut already exists on the desktop, ensure setting is synchronized and return
                if (ShortcutExists())
                {
                    if (!settingsManager.GetDesktopShortcutCreated())
                    {
                        settingsManager.SetDesktopShortcutCreated(true);
                        settingsManager.Flush();
                    }
                    return;
                }

                // If shortcut does not exist on desktop, create it
                if (TryCreateDesktopShortcut())
                {
                    try
                    {
                        settingsManager.SetDesktopShortcutCreated(true);
                        settingsManager.Flush();
                        Debug.WriteLine("Desktop shortcut created and recorded successfully.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to persist desktop shortcut creation state: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Desktop shortcut creation failed: {ex}");
            }
        }

        /// <summary>
        /// Checks whether a valid shortcut for Kopolart already exists on the desktop.
        /// </summary>
        public static bool ShortcutExists()
        {
            try
            {
                var desktopPath = GetDesktopDirectory();
                if (string.IsNullOrWhiteSpace(desktopPath) || !Directory.Exists(desktopPath))
                {
                    return false;
                }

                var candidateNames = new[] { ShortcutFileName, "Kopolart.lnk", "kopolart.lnk", "KopolArt.lnk" };
                foreach (var name in candidateNames)
                {
                    var fullPath = Path.Combine(desktopPath, name);
                    if (File.Exists(fullPath))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking if shortcut exists: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Attempts to create the desktop shortcut using a multi-tiered strategy:
        /// 1. Native Windows COM IShellLinkW / IPersistFile (official Win32 API)
        /// 2. Windows Script Host (WScript.Shell COM)
        /// 3. PowerShell command fallback
        /// </summary>
        public static bool TryCreateDesktopShortcut()
        {
            try
            {
                var desktopPath = GetDesktopDirectory();
                if (string.IsNullOrWhiteSpace(desktopPath) || !Directory.Exists(desktopPath))
                {
                    Debug.WriteLine("Could not resolve a valid Desktop directory.");
                    return false;
                }

                var exePath = GetTargetExecutablePath();
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    Debug.WriteLine($"Target executable path does not exist: {exePath}");
                    return false;
                }

                var shortcutPath = Path.Combine(desktopPath, ShortcutFileName);
                var workingDirectory = Path.GetDirectoryName(exePath) ?? desktopPath;

                // Tier 1: Native Win32 COM IShellLinkW (Fastest, standard, bypasses WSH restrictions)
                if (CreateShortcutViaNativeShellLink(shortcutPath, exePath, workingDirectory, ShortcutDescription, exePath))
                {
                    Debug.WriteLine($"Desktop shortcut successfully created via IShellLinkW: {shortcutPath}");
                    return true;
                }

                // Tier 2: WScript.Shell COM
                if (CreateShortcutViaWScript(shortcutPath, exePath, workingDirectory, ShortcutDescription, exePath))
                {
                    Debug.WriteLine($"Desktop shortcut successfully created via WScript.Shell: {shortcutPath}");
                    return true;
                }

                // Tier 3: PowerShell fallback
                if (CreateShortcutViaPowerShell(shortcutPath, exePath, workingDirectory, ShortcutDescription, exePath))
                {
                    Debug.WriteLine($"Desktop shortcut successfully created via PowerShell: {shortcutPath}");
                    return true;
                }

                return File.Exists(shortcutPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during TryCreateDesktopShortcut: {ex}");
                return false;
            }
        }

        private static bool CreateShortcutViaNativeShellLink(string shortcutPath, string targetPath, string workingDirectory, string description, string iconPath)
        {
            try
            {
                var shellLink = (IShellLinkW)new ShellLink();
                shellLink.SetPath(targetPath);
                shellLink.SetWorkingDirectory(workingDirectory);
                shellLink.SetDescription(description);

                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                {
                    shellLink.SetIconLocation(iconPath, 0);
                }
                else
                {
                    shellLink.SetIconLocation(targetPath, 0);
                }

                var persistFile = (IPersistFile)shellLink;
                persistFile.Save(shortcutPath, true);

                if (Marshal.IsComObject(shellLink))
                {
                    Marshal.ReleaseComObject(shellLink);
                }

                return File.Exists(shortcutPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IShellLinkW failed: {ex}");
                return false;
            }
        }

        private static bool CreateShortcutViaWScript(string shortcutPath, string targetPath, string workingDirectory, string description, string iconPath)
        {
            dynamic? shell = null;
            dynamic? shortcut = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return false;
                }

                shell = Activator.CreateInstance(shellType);
                if (shell == null)
                {
                    return false;
                }

                shortcut = shell.CreateShortcut(shortcutPath);
                if (shortcut == null)
                {
                    return false;
                }

                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDirectory;
                shortcut.Description = description;
                shortcut.IconLocation = !string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath)
                    ? $"{iconPath},0"
                    : $"{targetPath},0";

                shortcut.Save();
                return File.Exists(shortcutPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WScript.Shell failed: {ex}");
                return false;
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                {
                    Marshal.ReleaseComObject(shortcut);
                }
                if (shell != null && Marshal.IsComObject(shell))
                {
                    Marshal.ReleaseComObject(shell);
                }
            }
        }

        private static bool CreateShortcutViaPowerShell(string shortcutPath, string targetPath, string workingDirectory, string description, string iconPath)
        {
            try
            {
                var escapedShortcutPath = shortcutPath.Replace("'", "''");
                var escapedTargetPath = targetPath.Replace("'", "''");
                var escapedWorkingDir = workingDirectory.Replace("'", "''");
                var escapedDescription = description.Replace("'", "''");
                var escapedIconPath = iconPath.Replace("'", "''");

                var psScript = $"$s=(New-Object -ComObject WScript.Shell).CreateShortcut('{escapedShortcutPath}');" +
                               $"$s.TargetPath='{escapedTargetPath}';" +
                               $"$s.WorkingDirectory='{escapedWorkingDir}';" +
                               $"$s.Description='{escapedDescription}';" +
                               $"$s.IconLocation='{escapedIconPath},0';" +
                               "$s.Save();";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    return File.Exists(shortcutPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PowerShell shortcut creation fallback failed: {ex}");
            }

            return false;
        }

        public static string? GetDesktopDirectory()
        {
            // 1. Try SpecialFolder.DesktopDirectory
            var path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolderOption.Create);
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                return path;
            }

            // 2. Try SpecialFolder.Desktop
            path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop, Environment.SpecialFolderOption.Create);
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                return path;
            }

            // 3. Try Registry User Shell Folders (handles OneDrive / redirected desktop folder)
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
                var val = key?.GetValue("Desktop") as string;
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var expanded = Environment.ExpandEnvironmentVariables(val);
                    if (Directory.Exists(expanded))
                    {
                        return expanded;
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }

            return null;
        }

        public static string? GetTargetExecutablePath()
        {
            return Infrastructure.Helpers.AppPathHelper.GetTargetExecutablePath();
        }

        #region Native COM Interop Definitions

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder ppszFileName);
        }

        #endregion
    }
}
