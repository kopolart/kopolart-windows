using System;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Monitors network availability changes and safely dispatches reconnection callbacks
    /// to the WinForms UI thread, handling dispose/handle destruction edge cases.
    /// </summary>
    public class NetworkAvailabilityWatcher : IDisposable
    {
        private readonly Control _targetControl;
        private readonly Action _onNetworkRestored;
        private bool _isDisposed;

        public NetworkAvailabilityWatcher(Control targetControl, Action onNetworkRestored)
        {
            _targetControl = targetControl ?? throw new ArgumentNullException(nameof(targetControl));
            _onNetworkRestored = onNetworkRestored ?? throw new ArgumentNullException(nameof(onNetworkRestored));

            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            if (!e.IsAvailable || _isDisposed)
            {
                return;
            }

            // This event fires on a threadpool thread. InvokeRequired is false before the
            // handle exists, so without these guards WebView2/timer work could run off the
            // UI thread, and Invoke during dispose could crash the process.
            try
            {
                if (_targetControl.IsDisposed || !_targetControl.IsHandleCreated)
                {
                    return;
                }

                _targetControl.BeginInvoke(new Action(() =>
                {
                    if (!_targetControl.IsDisposed && !_isDisposed)
                    {
                        _onNetworkRestored();
                    }
                }));
            }
            catch (ObjectDisposedException)
            {
                // Form disposed between the check and the call — ignore.
            }
            catch (InvalidOperationException)
            {
                // Handle destroyed concurrently — ignore.
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        }
    }
}
