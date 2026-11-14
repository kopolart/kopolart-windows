using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.UI.Forms;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls.NavBar
{
    /// <summary>
    /// Handles update check orchestration, update notification badges, blinking timer, and menu spacing.
    /// </summary>
    public sealed class NavBarUpdateHandler : IDisposable
    {
        private readonly IMainFormActions _parentForm;
        private readonly IUpdateService _updateService;
        private readonly ISettingsManager _settingsManager;
        private readonly Func<bool> _isDarkModeProvider;
        private readonly IDownloadService _downloadService;
        private readonly IUpdatePackageVerifier _packageVerifier;
        private readonly MenuStrip _menuStrip;
        private readonly ToolStripMenuItem _helpToolStripMenuItem;
        private readonly Func<ToolStripMenuItem?> _updatesMenuItemProvider;

        private Timer? _blinkTimer;
        private bool _isDisposed;

        public NavBarUpdateHandler(
            IMainFormActions parentForm,
            IUpdateService updateService,
            ISettingsManager settingsManager,
            Func<bool> isDarkModeProvider,
            IDownloadService downloadService,
            IUpdatePackageVerifier packageVerifier,
            MenuStrip menuStrip,
            ToolStripMenuItem helpToolStripMenuItem,
            Func<ToolStripMenuItem?> updatesMenuItemProvider)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
            _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
            _packageVerifier = packageVerifier ?? throw new ArgumentNullException(nameof(packageVerifier));
            _menuStrip = menuStrip ?? throw new ArgumentNullException(nameof(menuStrip));
            _helpToolStripMenuItem = helpToolStripMenuItem ?? throw new ArgumentNullException(nameof(helpToolStripMenuItem));
            _updatesMenuItemProvider = updatesMenuItemProvider ?? throw new ArgumentNullException(nameof(updatesMenuItemProvider));
        }

        public async Task CheckForUpdatesManualAsync()
        {
            bool isDarkMode = _isDarkModeProvider();
            var updatesMenuItem = _updatesMenuItemProvider();

            if (updatesMenuItem != null)
            {
                updatesMenuItem.Enabled = false;
            }

            try
            {
                var currentVersion = System.Windows.Forms.Application.ProductVersion;
                var updateInfo = await _updateService.CheckForUpdatesAsync(currentVersion);

                if (updateInfo != null && !string.IsNullOrWhiteSpace(updateInfo.Version))
                {
                    if (updateInfo.IsNewerThan(currentVersion))
                    {
                        SetUpdateAvailable(true);
                        UpdateUIHandler.PromptAndDownloadUpdate(_parentForm.AsForm, updateInfo, isDarkMode, _downloadService, _packageVerifier);
                        return;
                    }

                    string title = TextManager.GetString("UpdateNoNewUpdateTitle");
                    string msg = TextManager.GetString("UpdateNoNewUpdateMessage");
                    CustomMessageBox.Show(_parentForm, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information, isDarkMode);
                }
                else
                {
                    string title = TextManager.GetString("UpdateCheckFailedTitle");
                    string msg = TextManager.GetString("UpdateCheckFailedMessage");
                    CustomMessageBox.Show(_parentForm, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning, isDarkMode);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Manual update check failed: {ex.Message}");
                string title = TextManager.GetString("UpdateCheckFailedTitle");
                string msg = TextManager.GetString("UpdateCheckFailedMessage");
                CustomMessageBox.Show(_parentForm, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error, isDarkMode);
            }
            finally
            {
                if (updatesMenuItem != null)
                {
                    updatesMenuItem.Enabled = true;
                }
            }
        }

        public void SetUpdateAvailable(bool available)
        {
            if (_menuStrip.InvokeRequired)
            {
                _menuStrip.Invoke(new Action(() => SetUpdateAvailable(available)));
                return;
            }

            MenuUpdateNotifier.IsUpdateAvailable = available;
            UpdateMenuTextsWithSpacing(available);
            RefreshBlinkState();
        }

        public void UpdateMenuTextsWithSpacing(bool available)
        {
            var updatesMenuItem = _updatesMenuItemProvider();

            string helpBase = TextManager.GetString("MenuHelp");
            string updatesBase = TextManager.GetString("MenuUpdates");

            if (available)
            {
                _helpToolStripMenuItem.Text = helpBase + "\u00A0\u00A0\u00A0";
                if (updatesMenuItem != null)
                {
                    updatesMenuItem.Text = updatesBase + "\u00A0\u00A0\u00A0";
                }
            }
            else
            {
                _helpToolStripMenuItem.Text = helpBase;
                if (updatesMenuItem != null)
                {
                    updatesMenuItem.Text = updatesBase;
                }
            }
        }

        public void RefreshBlinkState()
        {
            bool hasUpdate = MenuUpdateNotifier.IsUpdateAvailable;

            if (hasUpdate)
            {
                if (_blinkTimer == null)
                {
                    _blinkTimer = new Timer();
                    _blinkTimer.Interval = 500;
                    _blinkTimer.Tick += (s, e) =>
                    {
                        MenuUpdateNotifier.BlinkState = !MenuUpdateNotifier.BlinkState;
                        _menuStrip.Invalidate();
                        if (_helpToolStripMenuItem.DropDown != null && _helpToolStripMenuItem.DropDown.Visible)
                        {
                            _helpToolStripMenuItem.DropDown.Invalidate();
                        }
                    };
                }
                _blinkTimer.Start();
            }
            else
            {
                if (_blinkTimer != null)
                {
                    _blinkTimer.Stop();
                    _blinkTimer.Dispose();
                    _blinkTimer = null;
                }
                MenuUpdateNotifier.BlinkState = false;
                _menuStrip.Invalidate();
                if (_helpToolStripMenuItem.DropDown != null && _helpToolStripMenuItem.DropDown.Visible)
                {
                    _helpToolStripMenuItem.DropDown.Invalidate();
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_blinkTimer != null)
                {
                    _blinkTimer.Stop();
                    _blinkTimer.Dispose();
                    _blinkTimer = null;
                }
                _isDisposed = true;
            }
        }
    }
}

