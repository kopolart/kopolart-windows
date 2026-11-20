using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Infrastructure.Helpers;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    /// <summary>
    /// Registers a per-user Windows Task Scheduler entry that launches the application with
    /// <c>--rss-check</c> every 3 days. That headless run shows the new-post toast even when the
    /// app is closed. No administrator rights are required (current-user task, limited privileges).
    /// </summary>
    public class WindowsBackgroundPostCheckService : IBackgroundPostCheckScheduler
    {
        public const string TaskName = "KopolartPostCheck";

        private const string CheckArgument = "--rss-check";
        private const int RunEveryDays = 3;
        private const int MaxCommandWaitMs = 20000;

        public bool IsRegistered()
        {
            return RunSchtasks("/Query", "/TN", TaskName) == 0;
        }

        public void Sync(bool notificationsEnabled)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    if (notificationsEnabled)
                    {
                        EnsureRegistered();
                    }
                    else
                    {
                        Unregister();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WindowsBackgroundPostCheck] Sync failed: {ex.Message}");
                }
            });
        }

        private void EnsureRegistered()
        {
            string? executablePath = AppPathHelper.GetTargetExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                Debug.WriteLine("[WindowsBackgroundPostCheck] Executable path not resolved. Skipping task registration.");
                return;
            }

            if (IsRegistered())
                return;

            // First run one hour from now, then every N days.
            string startTime = DateTime.Now.AddHours(1).ToString("HH:mm", CultureInfo.InvariantCulture);

            int exitCode = RunSchtasks(
                "/Create",
                "/F",
                "/TN", TaskName,
                "/TR", $"\"{executablePath}\" {CheckArgument}",
                "/SC", "DAILY",
                "/MO", RunEveryDays.ToString(CultureInfo.InvariantCulture),
                "/ST", startTime);

            if (exitCode != 0)
            {
                Debug.WriteLine($"[WindowsBackgroundPostCheck] Task creation failed with exit code {exitCode}.");
            }
        }

        private void Unregister()
        {
            if (!IsRegistered())
                return;

            int exitCode = RunSchtasks("/Delete", "/TN", TaskName, "/F");
            if (exitCode != 0)
            {
                Debug.WriteLine($"[WindowsBackgroundPostCheck] Task deletion failed with exit code {exitCode}.");
            }
        }

        private static int RunSchtasks(params string[] arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                foreach (var argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                using var process = Process.Start(startInfo);
                if (process == null)
                    return -1;

                // Read both streams before waiting so a full pipe buffer can never block the process.
                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(MaxCommandWaitMs))
                {
                    try { process.Kill(entireProcessTree: true); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WindowsBackgroundPostCheckService] Failed to kill timed-out process tree: {ex.Message}"); }
                    return -1;
                }

                if (process.ExitCode != 0)
                {
                    Debug.WriteLine($"[WindowsBackgroundPostCheck] schtasks {string.Join(' ', arguments)} -> {process.ExitCode}: {standardOutput} {standardError}");
                }

                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsBackgroundPostCheck] schtasks failed: {ex.Message}");
                return -1;
            }
        }
    }
}
