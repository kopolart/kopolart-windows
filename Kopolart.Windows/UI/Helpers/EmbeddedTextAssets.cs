using System;
using System.Collections.Concurrent;
using System.IO;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Cached access to embedded text assets (JS/CSS), so scripts and styles live in
    /// real .js/.css files instead of inline strings and are read from the assembly once.
    /// </summary>
    public static class EmbeddedTextAssets
    {
        public const string FindBarScript = "Kopolart.Windows.assets.js.findbar.js";
        public const string FindBarStyles = "Kopolart.Windows.assets.css.findbar.css";
        public const string ShortcutForwarderScript = "Kopolart.Windows.assets.js.shortcuts.js";

        private static readonly ConcurrentDictionary<string, string> Cache = new();

        public static string Get(string resourceName)
        {
            return Cache.GetOrAdd(resourceName, static name =>
            {
                using var stream = typeof(EmbeddedTextAssets).Assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException($"Embedded resource '{name}' was not found.");
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            });
        }
    }
}
