using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.Services;
using Kopolart.Windows.UI.Forms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Coordinates file downloads from WebView2, prompting user for file destination,
    /// tracking download progress, recording download history, and notifying via tray.
    /// </summary>
    public class WebViewDownloadCoordinator
    {
        private readonly Form _ownerForm;
        private readonly ISettingsManager _settingsManager;
        private readonly UsageTrackerService _usageTracker;
        private readonly Func<TrayIconManager?> _trayIconManagerProvider;
        private readonly Func<bool> _isDarkModeProvider;

        private bool _isShowingSaveDialog;
        private string? _lastDownloadUri;
        private DateTime _lastDownloadTimeUtc;

        public WebViewDownloadCoordinator(
            Form ownerForm,
            ISettingsManager settingsManager,
            UsageTrackerService usageTracker,
            Func<TrayIconManager?> trayIconManagerProvider,
            Func<bool> isDarkModeProvider)
        {
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
            _trayIconManagerProvider = trayIconManagerProvider ?? throw new ArgumentNullException(nameof(trayIconManagerProvider));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
        }

        public void HandleDownloadStarting(CoreWebView2DownloadStartingEventArgs e)
        {
            e.Handled = true;

            string currentUri = e.DownloadOperation?.Uri ?? "";

            // Guard against duplicate rapid download triggers for the same URL
            var now = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(currentUri) &&
                string.Equals(currentUri, _lastDownloadUri, StringComparison.OrdinalIgnoreCase) &&
                (now - _lastDownloadTimeUtc).TotalMilliseconds < 2000)
            {
                Debug.WriteLine($"[WebViewDownloadCoordinator] Suppressed duplicate download event for: {currentUri}");
                e.Cancel = true;
                return;
            }

            // Guard against multiple SaveFileDialogs opening simultaneously
            if (_isShowingSaveDialog)
            {
                Debug.WriteLine($"[WebViewDownloadCoordinator] Suppressed concurrent download dialog for: {currentUri}");
                e.Cancel = true;
                return;
            }

            _lastDownloadUri = currentUri;
            _lastDownloadTimeUtc = now;
            _isShowingSaveDialog = true;

            // Take a deferral instead of blocking inside the WebView2 event with a modal
            // dialog — a nested message pump there stalls the browser's event dispatch
            // and allows reentrant WebView2 events mid-handler.
            var deferral = e.GetDeferral();

            _ownerForm.BeginInvoke(new Action(() =>
            {
                try
                {
                    string defaultFileName = Path.GetFileName(e.ResultFilePath);

                    using var saveFileDialog = new SaveFileDialog
                    {
                        FileName = defaultFileName,
                        Filter = "All Files (*.*)|*.*"
                    };

                    if (saveFileDialog.ShowDialog(_ownerForm) == DialogResult.OK)
                    {
                        e.ResultFilePath = saveFileDialog.FileName;

                        var downloadOp = e.DownloadOperation;
                        if (downloadOp != null)
                        {
                            downloadOp.StateChanged += (s, args) =>
                            {
                                if (downloadOp.State == CoreWebView2DownloadState.Completed)
                                {
                                    try
                                    {
                                        long size = downloadOp.TotalBytesToReceive.HasValue
                                            ? (long)downloadOp.TotalBytesToReceive.Value
                                            : (File.Exists(saveFileDialog.FileName) ? new FileInfo(saveFileDialog.FileName).Length : 0L);
                                        if (size > 0)
                                        {
                                            _usageTracker.TrackInternetBytes(size);
                                        }

                                        var item = new DownloadedItem
                                        {
                                            FileName = Path.GetFileName(saveFileDialog.FileName),
                                            FilePath = saveFileDialog.FileName,
                                            Url = downloadOp.Uri ?? "",
                                            FileSizeBytes = size,
                                            DownloadedAt = DateTime.Now,
                                            Category = DownloadedItem.DetermineCategory(downloadOp.Uri, Path.GetFileName(saveFileDialog.FileName))
                                        };
                                        _settingsManager.AddDownloadedItem(item);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Failed to record completed download: {ex.Message}");
                                    }

                                    var trayIcon = _trayIconManagerProvider();
                                    if (_ownerForm.InvokeRequired)
                                    {
                                        _ownerForm.BeginInvoke(new Action(() => trayIcon?.ShowDownloadComplete(saveFileDialog.FileName)));
                                    }
                                    else
                                    {
                                        trayIcon?.ShowDownloadComplete(saveFileDialog.FileName);
                                    }
                                }
                            };

                            var progressForm = new DownloadProgressDialog(downloadOp, _isDarkModeProvider());
                            progressForm.Show(_ownerForm);
                        }
                    }
                    else
                    {
                        e.Cancel = true;
                        try { e.DownloadOperation?.Cancel(); } catch { /* already gone */ }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to handle download starting: {ex}");
                    e.Cancel = true;
                    try { e.DownloadOperation?.Cancel(); } catch { /* already gone */ }
                }
                finally
                {
                    _isShowingSaveDialog = false;
                    deferral.Complete();
                }
            }));
        }
    }
}
