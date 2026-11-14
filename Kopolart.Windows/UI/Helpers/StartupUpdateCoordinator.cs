using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Services;
using Kopolart.Windows.UI.Forms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Coordinates post-update verification, desktop shortcut maintenance on update,
    /// notification toasts/dialogs, and scheduler startup.
    /// </summary>
    public class StartupUpdateCoordinator
    {
        private readonly ISettingsManager _settingsManager;
        private readonly UpdateSchedulerService _updateSchedulerService;
        private readonly Func<bool> _isDarkModeProvider;

        private string? _pendingUpdateSuccessMessage;
        private string? _pendingUpdateVerifyFailedMessage;

        public StartupUpdateCoordinator(
            ISettingsManager settingsManager,
            UpdateSchedulerService updateSchedulerService,
            Func<bool> isDarkModeProvider)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _updateSchedulerService = updateSchedulerService ?? throw new ArgumentNullException(nameof(updateSchedulerService));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
        }

        public void ProcessStartupArguments(string[] args)
        {
            if (args is { Length: > 1 } && Array.Exists(args, arg => arg.Equals("--updated", StringComparison.OrdinalIgnoreCase)))
            {
                // Ensure desktop shortcut exists and is up-to-date after application update
                DesktopShortcutManager.EnsureDesktopShortcutOnUpdate(_settingsManager);

                // Verify the swap actually installed the expected version instead of
                // blindly trusting the --updated flag (the old exe can relaunch if the swap failed).
                var expectedVersionText = UpdateMarker.ReadAndClear();
                bool updateVerified = true;
                if (Version.TryParse(expectedVersionText, out var expectedVersion) &&
                    Version.TryParse(System.Windows.Forms.Application.ProductVersion, out var actualVersion))
                {
                    updateVerified = actualVersion >= expectedVersion;
                }

                if (updateVerified)
                {
                    _pendingUpdateSuccessMessage = string.Format(
                        TextManager.GetString("UpdateSuccessMessage"),
                        System.Windows.Forms.Application.ProductVersion);
                }
                else
                {
                    _pendingUpdateVerifyFailedMessage = TextManager.GetString("UpdateVerifyFailedMessage");
                }
            }
        }

        public void TryShowPendingUpdateNotification(IWin32Window owner, Action? focusWebView = null)
        {
            if (_pendingUpdateSuccessMessage != null)
            {
                var msg = _pendingUpdateSuccessMessage;
                _pendingUpdateSuccessMessage = null;

                var isDark = _isDarkModeProvider();

                try
                {
                    new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                        .AddText(TextManager.GetString("UpdateSuccessTitle"))
                        .AddText(msg)
                        .Show();
                }
                catch
                {
                    using (var alert = new MilestoneAlert(isDark, msg, "UpdateSuccessTitle"))
                    {
                        alert.ShowDialog(owner);
                    }
                }
                focusWebView?.Invoke();
            }
            else if (_pendingUpdateVerifyFailedMessage != null)
            {
                var msg = _pendingUpdateVerifyFailedMessage;
                _pendingUpdateVerifyFailedMessage = null;

                var isDark = _isDarkModeProvider();

                CustomMessageBox.Show(owner,
                    msg,
                    TextManager.GetString("UpdateVerifyFailedTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning, isDark);
                focusWebView?.Invoke();
            }
        }

        public void StartUpdateScheduler(
            Form form,
            Action<bool> setBadge,
            Func<TrayIconManager?> trayIconManagerProvider,
            Action<IReadOnlyList<Domain.Models.RssFeedItem>>? onNewContent = null)
        {
            _updateSchedulerService.Start(
                form,
                setBadge,
                updateInfo => trayIconManagerProvider()?.ShowUpdateNotification(
                    updateInfo,
                    () => _updateSchedulerService.PromptUserForUpdate(updateInfo)),
                _isDarkModeProvider,
                posts => onNewContent?.Invoke(posts));
        }
    }
}

