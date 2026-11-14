using System;
using System.IO;
using System.Reflection;

namespace Kopolart.Windows.Infrastructure.Helpers
{
    public static class AppPathHelper
    {
        /// <summary>
        /// Resolves the actual application executable path, defensively avoiding 'dotnet.exe'
        /// when running in development/debug environments.
        /// </summary>
        public static string? GetTargetExecutablePath()
        {
            // 1. Process Path (Native executable running the process)
            string? path = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                if (!Path.GetFileName(path).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(path);
                }
            }

            // 2. Application.ExecutablePath
            path = System.Windows.Forms.Application.ExecutablePath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                {
                    if (!Path.GetFileName(path).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        return Path.GetFullPath(path);
                    }
                }
                else if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    var exeCandidate = Path.ChangeExtension(path, ".exe");
                    if (File.Exists(exeCandidate))
                    {
                        return Path.GetFullPath(exeCandidate);
                    }
                }
            }

            // 3. Entry Assembly Location
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null && !string.IsNullOrWhiteSpace(entryAssembly.Location))
            {
                var loc = entryAssembly.Location;
                if (loc.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(loc))
                {
                    return Path.GetFullPath(loc);
                }
                var exeCandidate = Path.ChangeExtension(loc, ".exe");
                if (File.Exists(exeCandidate))
                {
                    return Path.GetFullPath(exeCandidate);
                }
            }

            // 4. AppContext.BaseDirectory with Assembly Name
            var baseDir = AppContext.BaseDirectory;
            var appName = entryAssembly?.GetName().Name ?? "kopolart-win-x64";
            var defaultExe = Path.Combine(baseDir, $"{appName}.exe");
            if (File.Exists(defaultExe))
            {
                return Path.GetFullPath(defaultExe);
            }

            return null;
        }
    }
}
