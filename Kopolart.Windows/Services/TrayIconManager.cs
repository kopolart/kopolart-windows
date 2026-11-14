using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.Services
{
    public class TrayIconManager : IDisposable
    {
        private readonly Form1 _parentForm;
        private readonly ISettingsManager _settingsManager;
        private readonly Icon? _appIcon;
        private NotifyIcon? _trayIcon;
        private FormWindowState _stateBeforeHide = FormWindowState.Normal;

        private ContextMenuStrip? _trayContextMenu;
        private ToolStripMenuItem? _menuRestore;
        private ToolStripMenuItem? _menuCheckUpdates;
        private ToolStripMenuItem? _menuSettings;
        private ToolStripMenuItem? _menuExit;
        private string? _lastDownloadedFilePath;
        private Action? _onBalloonTipClicked;

        public TrayIconManager(Form1 parentForm, ISettingsManager settingsManager, Icon? appIcon)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _appIcon = appIcon;
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            _trayContextMenu = new ContextMenuStrip
            {
                RightToLeft = RightToLeft.Yes
            };

            _menuRestore = new ToolStripMenuItem(TextManager.GetString("TrayMenuRestore"), null, (s, e) => RestoreFromTray())
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _menuCheckUpdates = new ToolStripMenuItem(TextManager.GetString("TrayMenuCheckUpdates"), null, (s, e) =>
            {
                RestoreFromTray();
                _parentForm.CheckForUpdatesManually();
            });
            _menuSettings = new ToolStripMenuItem(TextManager.GetString("TrayMenuSettings"), null, (s, e) =>
            {
                RestoreFromTray();
                _parentForm.OpenSettingsDialog();
            });
            _menuExit = new ToolStripMenuItem(TextManager.GetString("TrayMenuExit"), null, (s, e) => _parentForm.ForceExit());

            _trayContextMenu.Items.Add(_menuRestore);
            _trayContextMenu.Items.Add(new ToolStripSeparator());
            _trayContextMenu.Items.Add(_menuCheckUpdates);
            _trayContextMenu.Items.Add(_menuSettings);
            _trayContextMenu.Items.Add(new ToolStripSeparator());
            _trayContextMenu.Items.Add(_menuExit);

            FontManager.ApplyCustomFont(_trayContextMenu);

            _trayIcon = new NotifyIcon
            {
                Icon = _appIcon,
                Visible = false,
                ContextMenuStrip = _trayContextMenu
            };
            _trayIcon.DoubleClick += (s, e) => RestoreFromTray();
            _trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;
        }

        private void TrayIcon_BalloonTipClicked(object? sender, EventArgs e)
        {
            if (_onBalloonTipClicked != null)
            {
                var action = _onBalloonTipClicked;
                _onBalloonTipClicked = null;
                RestoreFromTray();
                action();
                return;
            }

            if (!string.IsNullOrEmpty(_lastDownloadedFilePath) && System.IO.File.Exists(_lastDownloadedFilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_lastDownloadedFilePath}\"");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open explorer for downloaded file: {ex}");
                }
            }
            else
            {
                RestoreFromTray();
            }
        }

        public void ShowUpdateNotification(UpdateInfo updateInfo, Action onNotificationClicked)
        {
            _lastDownloadedFilePath = null;
            _onBalloonTipClicked = onNotificationClicked;

            if (_trayIcon != null)
            {
                bool wasVisible = _trayIcon.Visible;
                _trayIcon.Icon = _appIcon;
                _trayIcon.Visible = true;

                string title = TextManager.GetString("UpdateNotificationTitle");
                string msg = string.Format(TextManager.GetString("UpdateNotificationMessage"), updateInfo.Version);
                _trayIcon.ShowBalloonTip(6000, title, msg, ToolTipIcon.Info);

                if (!wasVisible && _parentForm.Visible)
                {
                    Timer timer = new Timer { Interval = 10000 };
                    timer.Tick += (s, e) =>
                    {
                        if (!_parentForm.Visible) return;
                        _trayIcon.Visible = false;
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
        }

        public void ShowNewContentNotification(string title, string message, Action onNotificationClicked)
        {
            _lastDownloadedFilePath = null;
            _onBalloonTipClicked = onNotificationClicked;

            if (_trayIcon != null)
            {
                bool wasVisible = _trayIcon.Visible;
                _trayIcon.Icon = _appIcon;
                _trayIcon.Visible = true;

                _trayIcon.ShowBalloonTip(7000, title, message, ToolTipIcon.Info);

                if (!wasVisible && _parentForm.Visible)
                {
                    Timer timer = new Timer { Interval = 12000 };
                    timer.Tick += (s, e) =>
                    {
                        if (!_parentForm.Visible) return;
                        _trayIcon.Visible = false;
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
        }

        public void ApplyTexts()
        {
            if (_trayContextMenu != null)
            {
                _trayContextMenu.RightToLeft = RightToLeft.Yes;
            }
            if (_menuRestore != null) _menuRestore.Text = TextManager.GetString("TrayMenuRestore");
            if (_menuCheckUpdates != null) _menuCheckUpdates.Text = TextManager.GetString("TrayMenuCheckUpdates");
            if (_menuSettings != null) _menuSettings.Text = TextManager.GetString("TrayMenuSettings");
            if (_menuExit != null) _menuExit.Text = TextManager.GetString("TrayMenuExit");
        }

        public void HideToTray()
        {
            // Remember the current state so restore doesn't force a maximized window back to Normal.
            _stateBeforeHide = _parentForm.WindowState == FormWindowState.Minimized
                ? FormWindowState.Normal
                : _parentForm.WindowState;

            _parentForm.Hide();
            _parentForm.UsageTracker.PauseTracking();
            if (_trayIcon != null)
            {
                _trayIcon.Icon = _appIcon;
                _trayIcon.Visible = true;
                string title = TextManager.GetString("AppRunningBackgroundTitle");
                string msg = TextManager.GetString("AppRunningBackground");
                _trayIcon.ShowBalloonTip(3000, title, msg, ToolTipIcon.Info);
            }
        }

        public void ShowDownloadComplete(string filePath)
        {
            _lastDownloadedFilePath = filePath;
            string fileName = System.IO.Path.GetFileName(filePath);

            if (_trayIcon != null)
            {
                bool wasVisible = _trayIcon.Visible;
                _trayIcon.Icon = _appIcon;
                _trayIcon.Visible = true;
                string title = TextManager.GetString("DownloadTitle");
                string msg = string.Format(TextManager.GetString("DownloadComplete"), fileName);
                _trayIcon.ShowBalloonTip(4000, title, msg, ToolTipIcon.Info);

                if (!wasVisible && _parentForm.Visible)
                {
                    // If the tray icon was temporarily shown only for balloon tip, hide it after delay
                    Timer timer = new Timer { Interval = 5000 };
                    timer.Tick += (s, e) =>
                    {
                        if (!_parentForm.Visible) return;
                        _trayIcon.Visible = false;
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
        }

        public void RestoreFromTray()
        {
            _parentForm.Show();
            _parentForm.WindowState = _stateBeforeHide;
            _parentForm.Activate();
            if (!_parentForm.IsDashboardVisible && _parentForm.WindowState != FormWindowState.Minimized)
            {
                _parentForm.UsageTracker.ResumeTracking();
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
            }
        }

        public void Dispose()
        {
            if (_trayIcon != null)
            {
                _trayIcon.BalloonTipClicked -= TrayIcon_BalloonTipClicked;
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            if (_trayContextMenu != null)
            {
                _trayContextMenu.Dispose();
                _trayContextMenu = null;
            }
        }
    }
}

