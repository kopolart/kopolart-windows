using System;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Helper to deduplicate keyboard shortcut processing between WinForms cmd key events
    /// and WebView2 injected accelerator messages within a short time threshold.
    /// </summary>
    public class KeyDeduplicator
    {
        private const int ThresholdMilliseconds = 100;
        private Keys _lastProcessedKey;
        private long _lastProcessedKeyTicks;

        public bool DeduplicateKey(Keys keyData, Func<bool> handler)
        {
            var now = Environment.TickCount64;
            if (keyData == _lastProcessedKey && (now - _lastProcessedKeyTicks) < ThresholdMilliseconds)
            {
                return true;
            }

            var result = handler();
            if (result)
            {
                _lastProcessedKey = keyData;
                _lastProcessedKeyTicks = now;
            }
            return result;
        }
    }
}
