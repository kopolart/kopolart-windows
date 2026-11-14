using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.UI.Controllers;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class SettingsDialog : ThemedDialog
    {
        private ISettingsManager _settingsManager;
        private IWebViewService _webViewService;
        private WebViewController _webViewController;
        private bool _saveRequested;
        private bool _revertRequested;

        private Label lblTitle = null!;
        private CheckBox chkStartMaximized = null!;
        private CheckBox chkOpenDashboardAtStartup = null!;
        private CheckBox chkShowNavLoading = null!;
        private CheckBox chkShowExitPrompt = null!;
        private CheckBox chkAutoStart = null!;
        private CheckBox chkMinimizeToTray = null!;
        private CheckBox chkShowShortcutClearCachePrompt = null!;
        private CheckBox chkClearCacheOnExit = null!;
        private Label lblCacheSize = null!;
        private CheckBox chkRestoreLastVisitedPage = null!;
        private CheckBox chkKeepDesktopShortcut = null!;
        private CheckBox chkNotifyNewContent = null!;
        private CheckBox chkDisableGpu = null!;
        private Label lblLayoutMode = null!;
        private ComboBox cmbLayoutMode = null!;
        private Button btnClearCache = null!;
        private Button btnClearCookies = null!;
        private Button btnClearAll = null!;
        private Button btnRevert = null!;
        private Button btnClose = null!;
        private Button btnExportData = null!;
        private Button btnImportData = null!;
        private GroupBox grpGeneral = null!;
        private GroupBox grpAppearance = null!;
        private GroupBox grpCache = null!;
        private readonly DataBackupCoordinator? _backupCoordinator;
        private readonly Action? _testNotificationAction;
        private int _titleClickCount;

        private const int TestNotificationClickCount = 5;

        public SettingsDialog(
            ISettingsManager settingsManager,
            IWebViewService webViewService,
            WebViewController webViewController,
            bool isDark = false,
            DataBackupCoordinator? backupCoordinator = null,
            Action? testNotificationAction = null)
            : base(isDark)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
            _webViewController = webViewController ?? throw new ArgumentNullException(nameof(webViewController));
            _backupCoordinator = backupCoordinator;
            _testNotificationAction = testNotificationAction;

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            LoadSettings();
            InitializeTheme();

            // Hidden diagnostics: 5 clicks on the dialog title shows a sample new-post notification.
            lblTitle.Cursor = Cursors.Default;
            lblTitle.Click += OnTitleClicked;
        }

        private void OnTitleClicked(object? sender, EventArgs e)
        {
            if (_testNotificationAction == null || ++_titleClickCount < TestNotificationClickCount)
                return;

            _titleClickCount = 0;
            _testNotificationAction();
        }



        private void InitializeComponent()
        {
            lblTitle = new Label();
            chkStartMaximized = new CheckBox();
            chkOpenDashboardAtStartup = new CheckBox();
            chkShowNavLoading = new CheckBox();
            chkShowExitPrompt = new CheckBox();
            chkAutoStart = new CheckBox();
            chkMinimizeToTray = new CheckBox();
            chkShowShortcutClearCachePrompt = new CheckBox();
            chkClearCacheOnExit = new CheckBox();
            lblCacheSize = new Label();
            chkRestoreLastVisitedPage = new CheckBox();
            chkKeepDesktopShortcut = new CheckBox();
            chkNotifyNewContent = new CheckBox();
            chkDisableGpu = new CheckBox();
            lblLayoutMode = new Label();
            cmbLayoutMode = new ComboBox();
            btnClearCache = new Button();
            btnClearCookies = new Button();
            btnClearAll = new Button();
            btnExportData = new Button();
            btnImportData = new Button();
            btnRevert = new Button();
            btnClose = new Button();
            grpGeneral = new GroupBox();
            grpAppearance = new GroupBox();
            grpCache = new GroupBox();
            
            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(710, 620);
            MinimumSize = new Size(710, 620);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("MenuSettings");
            BackColor = Color.White;

            lblTitle.Text = TextManager.GetString("SettingsTitle");
            lblTitle.Font = FontManager.CreateBlackFont(14F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(710 - 20 - lblTitle.PreferredWidth, 15);

            grpGeneral.Text = TextManager.GetString("SettingsTabGeneral");
            grpGeneral.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            grpGeneral.Location = new Point(20, 50);
            grpGeneral.Size = new Size(670, 155);

            grpAppearance.Text = TextManager.GetString("SettingsTabAppearance");
            grpAppearance.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            grpAppearance.Location = new Point(20, 215);
            grpAppearance.Size = new Size(670, 95);

            grpCache.Text = TextManager.GetString("SettingsDataCache");
            grpCache.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            grpCache.Location = new Point(20, 320);
            grpCache.Size = new Size(670, 215);

            // Group 1: General (Right Column: X=345, Width=310 | Left Column: X=15, Width=315)
            chkStartMaximized.Text = TextManager.GetString("SettingsStartMaximized");
            chkStartMaximized.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkStartMaximized.Location = new Point(345, 25);
            chkStartMaximized.Size = new Size(310, 26);
            chkStartMaximized.CheckAlign = ContentAlignment.MiddleRight;
            chkStartMaximized.TextAlign = ContentAlignment.MiddleRight;
            chkStartMaximized.AutoSize = false;

            chkOpenDashboardAtStartup.Text = TextManager.GetString("SettingsOpenDashboardAtStartup");
            chkOpenDashboardAtStartup.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkOpenDashboardAtStartup.Location = new Point(345, 55);
            chkOpenDashboardAtStartup.Size = new Size(310, 26);
            chkOpenDashboardAtStartup.CheckAlign = ContentAlignment.MiddleRight;
            chkOpenDashboardAtStartup.TextAlign = ContentAlignment.MiddleRight;
            chkOpenDashboardAtStartup.AutoSize = false;

            chkAutoStart.Text = TextManager.GetString("SettingsAutoStart");
            chkAutoStart.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkAutoStart.Location = new Point(345, 85);
            chkAutoStart.Size = new Size(310, 26);
            chkAutoStart.CheckAlign = ContentAlignment.MiddleRight;
            chkAutoStart.TextAlign = ContentAlignment.MiddleRight;
            chkAutoStart.AutoSize = false;

            chkMinimizeToTray.Text = TextManager.GetString("SettingsMinimizeToTray");
            chkMinimizeToTray.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkMinimizeToTray.Location = new Point(345, 115);
            chkMinimizeToTray.Size = new Size(310, 26);
            chkMinimizeToTray.CheckAlign = ContentAlignment.MiddleRight;
            chkMinimizeToTray.TextAlign = ContentAlignment.MiddleRight;
            chkMinimizeToTray.AutoSize = false;

            chkShowExitPrompt.Text = TextManager.GetString("SettingsShowExitPrompt");
            chkShowExitPrompt.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkShowExitPrompt.Location = new Point(15, 25);
            chkShowExitPrompt.Size = new Size(315, 26);
            chkShowExitPrompt.CheckAlign = ContentAlignment.MiddleRight;
            chkShowExitPrompt.TextAlign = ContentAlignment.MiddleRight;
            chkShowExitPrompt.AutoSize = false;

            chkRestoreLastVisitedPage.Text = TextManager.GetString("SettingsRestoreLastPage");
            chkRestoreLastVisitedPage.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkRestoreLastVisitedPage.Location = new Point(15, 55);
            chkRestoreLastVisitedPage.Size = new Size(315, 26);
            chkRestoreLastVisitedPage.CheckAlign = ContentAlignment.MiddleRight;
            chkRestoreLastVisitedPage.TextAlign = ContentAlignment.MiddleRight;
            chkRestoreLastVisitedPage.AutoSize = false;

            chkKeepDesktopShortcut.Text = TextManager.GetString("SettingsKeepDesktopShortcut");
            chkKeepDesktopShortcut.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkKeepDesktopShortcut.Location = new Point(15, 85);
            chkKeepDesktopShortcut.Size = new Size(315, 26);
            chkKeepDesktopShortcut.CheckAlign = ContentAlignment.MiddleRight;
            chkKeepDesktopShortcut.TextAlign = ContentAlignment.MiddleRight;
            chkKeepDesktopShortcut.AutoSize = false;

            chkNotifyNewContent.Text = TextManager.GetString("SettingsNotifyNewContent");
            chkNotifyNewContent.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkNotifyNewContent.Location = new Point(15, 115);
            chkNotifyNewContent.Size = new Size(315, 26);
            chkNotifyNewContent.CheckAlign = ContentAlignment.MiddleRight;
            chkNotifyNewContent.TextAlign = ContentAlignment.MiddleRight;
            chkNotifyNewContent.AutoSize = false;

            grpGeneral.Controls.Add(chkStartMaximized);
            grpGeneral.Controls.Add(chkOpenDashboardAtStartup);
            grpGeneral.Controls.Add(chkAutoStart);
            grpGeneral.Controls.Add(chkMinimizeToTray);
            grpGeneral.Controls.Add(chkShowExitPrompt);
            grpGeneral.Controls.Add(chkRestoreLastVisitedPage);
            grpGeneral.Controls.Add(chkKeepDesktopShortcut);
            grpGeneral.Controls.Add(chkNotifyNewContent);

            // Group 2: Appearance
            chkShowNavLoading.Text = TextManager.GetString("SettingsShowNavLoading");
            chkShowNavLoading.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkShowNavLoading.Location = new Point(345, 25);
            chkShowNavLoading.Size = new Size(310, 26);
            chkShowNavLoading.CheckAlign = ContentAlignment.MiddleRight;
            chkShowNavLoading.TextAlign = ContentAlignment.MiddleRight;
            chkShowNavLoading.AutoSize = false;

            chkDisableGpu.Text = TextManager.GetString("SettingsDisableGpu");
            chkDisableGpu.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkDisableGpu.Location = new Point(345, 55);
            chkDisableGpu.Size = new Size(310, 26);
            chkDisableGpu.CheckAlign = ContentAlignment.MiddleRight;
            chkDisableGpu.TextAlign = ContentAlignment.MiddleRight;
            chkDisableGpu.AutoSize = false;

            lblLayoutMode.Text = TextManager.GetString("SettingsLayoutMode") + ":";
            lblLayoutMode.Font = FontManager.CreateFont(9F, FontStyle.Bold);
            lblLayoutMode.Location = new Point(195, 28);
            lblLayoutMode.Size = new Size(125, 25);
            lblLayoutMode.RightToLeft = RightToLeft.No;
            lblLayoutMode.TextAlign = ContentAlignment.MiddleRight;
            lblLayoutMode.AutoSize = false;

            cmbLayoutMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLayoutMode.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            cmbLayoutMode.Location = new Point(15, 26);
            cmbLayoutMode.Size = new Size(175, 26);
            cmbLayoutMode.Items.AddRange(new object[]
            {
                TextManager.GetString("LayoutDesktop"),
                TextManager.GetString("LayoutTablet"),
                TextManager.GetString("LayoutMobile")
            });

            grpAppearance.Controls.Add(chkShowNavLoading);
            grpAppearance.Controls.Add(chkDisableGpu);
            grpAppearance.Controls.Add(lblLayoutMode);
            grpAppearance.Controls.Add(cmbLayoutMode);

            // Group 3: Data, Cache & Backup
            chkShowShortcutClearCachePrompt.Text = TextManager.GetString("SettingsShowShortcutClearCachePrompt");
            chkShowShortcutClearCachePrompt.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkShowShortcutClearCachePrompt.Location = new Point(345, 25);
            chkShowShortcutClearCachePrompt.Size = new Size(310, 26);
            chkShowShortcutClearCachePrompt.CheckAlign = ContentAlignment.MiddleRight;
            chkShowShortcutClearCachePrompt.TextAlign = ContentAlignment.MiddleRight;
            chkShowShortcutClearCachePrompt.AutoSize = false;

            chkClearCacheOnExit.Text = TextManager.GetString("SettingsClearCacheOnExit");
            chkClearCacheOnExit.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            chkClearCacheOnExit.Location = new Point(15, 25);
            chkClearCacheOnExit.Size = new Size(315, 26);
            chkClearCacheOnExit.CheckAlign = ContentAlignment.MiddleRight;
            chkClearCacheOnExit.TextAlign = ContentAlignment.MiddleRight;
            chkClearCacheOnExit.AutoSize = false;

            lblCacheSize.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lblCacheSize.Location = new Point(15, 58);
            lblCacheSize.Size = new Size(640, 22);
            lblCacheSize.RightToLeft = RightToLeft.No;
            lblCacheSize.TextAlign = ContentAlignment.MiddleRight;
            lblCacheSize.AutoSize = false;
            lblCacheSize.Text = string.Format(TextManager.GetString("SettingsCacheSize"), "...");

            btnClearCache.Text = TextManager.GetString("SettingsClearCache");
            btnClearCache.Size = new Size(205, 34);
            btnClearCache.Location = new Point(450, 88);
            btnClearCache.Cursor = Cursors.Hand;
            btnClearCache.FlatStyle = FlatStyle.Flat;
            btnClearCache.FlatAppearance.BorderSize = 1;
            btnClearCache.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClearCache.Click += BtnClearCache_Click;

            btnClearCookies.Text = TextManager.GetString("SettingsClearCookies");
            btnClearCookies.Size = new Size(205, 34);
            btnClearCookies.Location = new Point(233, 88);
            btnClearCookies.Cursor = Cursors.Hand;
            btnClearCookies.FlatStyle = FlatStyle.Flat;
            btnClearCookies.FlatAppearance.BorderSize = 1;
            btnClearCookies.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClearCookies.Click += BtnClearCookies_Click;

            btnClearAll.Text = TextManager.GetString("SettingsClearAll");
            btnClearAll.Size = new Size(205, 34);
            btnClearAll.Location = new Point(15, 88);
            btnClearAll.Cursor = Cursors.Hand;
            btnClearAll.FlatStyle = FlatStyle.Flat;
            btnClearAll.FlatAppearance.BorderSize = 1;
            btnClearAll.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClearAll.Click += BtnClearAll_Click;

            btnExportData.Text = TextManager.GetString("ExportButtonText");
            btnExportData.Size = new Size(312, 36);
            btnExportData.Location = new Point(343, 145);
            btnExportData.Cursor = Cursors.Hand;
            btnExportData.FlatStyle = FlatStyle.Flat;
            btnExportData.FlatAppearance.BorderSize = 1;
            btnExportData.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnExportData.Click += async (s, e) =>
            {
                if (_backupCoordinator != null)
                {
                    await _backupCoordinator.ExportDataAsync();
                }
            };

            btnImportData.Text = TextManager.GetString("ImportButtonText");
            btnImportData.Size = new Size(312, 36);
            btnImportData.Location = new Point(15, 145);
            btnImportData.Cursor = Cursors.Hand;
            btnImportData.FlatStyle = FlatStyle.Flat;
            btnImportData.FlatAppearance.BorderSize = 1;
            btnImportData.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnImportData.Click += async (s, e) =>
            {
                if (_backupCoordinator != null)
                {
                    await _backupCoordinator.ImportDataAsync();
                    LoadSettings();
                }
            };

            grpCache.Controls.Add(chkShowShortcutClearCachePrompt);
            grpCache.Controls.Add(chkClearCacheOnExit);
            grpCache.Controls.Add(lblCacheSize);
            grpCache.Controls.Add(btnClearCache);
            grpCache.Controls.Add(btnClearCookies);
            grpCache.Controls.Add(btnClearAll);
            grpCache.Controls.Add(btnExportData);
            grpCache.Controls.Add(btnImportData);

            btnRevert.Text = TextManager.GetString("SettingsRevert");
            btnRevert.Size = new Size(325, 38);
            btnRevert.Location = new Point(365, 555);
            btnRevert.Cursor = Cursors.Hand;
            btnRevert.FlatStyle = FlatStyle.Flat;
            btnRevert.FlatAppearance.BorderSize = 1;
            btnRevert.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            btnRevert.Click += BtnRevert_Click;

            btnClose.Text = TextManager.GetString("SaveAndClose");
            btnClose.Size = new Size(325, 38);
            btnClose.Location = new Point(20, 555);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            btnClose.Click += BtnClose_Click;

            Controls.Add(lblTitle);
            Controls.Add(grpGeneral);
            Controls.Add(grpAppearance);
            Controls.Add(grpCache);
            Controls.Add(btnRevert);
            Controls.Add(btnClose);

            AcceptButton = btnClose;

            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadSettings()
        {
            chkStartMaximized.Checked = _settingsManager.GetStartMaximized();
            chkOpenDashboardAtStartup.Checked = _settingsManager.GetOpenDashboardAtStartup();
            chkShowNavLoading.Checked = _settingsManager.GetShowNavigationLoading();
            chkShowExitPrompt.Checked = _settingsManager.GetShowExitPrompt();
            chkAutoStart.Checked = _settingsManager.GetAutoStartEnabled();
            chkMinimizeToTray.Checked = _settingsManager.GetMinimizeToTrayEnabled();
            chkShowShortcutClearCachePrompt.Checked = _settingsManager.GetShowShortcutClearCachePrompt();
            chkClearCacheOnExit.Checked = _settingsManager.GetClearCacheOnExit();
            chkRestoreLastVisitedPage.Checked = _settingsManager.GetRestoreLastVisitedPage();
            chkKeepDesktopShortcut.Checked = _settingsManager.GetKeepDesktopShortcut();
            chkNotifyNewContent.Checked = _settingsManager.GetNotifyNewContent();
            chkDisableGpu.Checked = _settingsManager.GetDisableGpuAcceleration();

            var mode = _settingsManager.GetLayoutMode();
            cmbLayoutMode.SelectedIndex = mode switch
            {
                "tablet" => 1,
                "mobile" => 2,
                _ => 0
            };

            UpdateCacheSizeDisplay();
        }

        private void UpdateCacheSizeDisplay()
        {
            Task.Run(() =>
            {
                long bytes = _webViewService.GetCacheSizeBytes();
                double mb = bytes / (1024.0 * 1024.0);
                string sizeStr = mb < 0.1 ? "< 0.1 MB" : $"{mb:F1} MB";
                string text = string.Format(TextManager.GetString("SettingsCacheSize"), sizeStr);
                if (!IsDisposed && IsHandleCreated)
                {
                    try
                    {
                        BeginInvoke(() =>
                        {
                            if (!IsDisposed)
                            {
                                lblCacheSize.Text = text;
                            }
                        });
                    }
                    catch
                    {
                        // Form might be disposing
                    }
                }
            });
        }

        private void ApplyCommonButtonTheme()
        {
            btnClose.BackColor = UIConstants.ThemeColor;
            btnClose.ForeColor = Color.FromArgb(74, 59, 42);

            btnClearCache.BackColor = Color.Transparent;
            btnClearCache.ForeColor = UIConstants.DangerColor;
            btnClearCache.FlatAppearance.BorderColor = UIConstants.DangerColor;

            btnClearCookies.BackColor = Color.Transparent;
            btnClearCookies.ForeColor = UIConstants.DangerColor;
            btnClearCookies.FlatAppearance.BorderColor = UIConstants.DangerColor;

            btnClearAll.BackColor = Color.Transparent;
            btnClearAll.ForeColor = UIConstants.DangerColor;
            btnClearAll.FlatAppearance.BorderColor = UIConstants.DangerColor;

            btnRevert.BackColor = Color.Transparent;
            btnRevert.ForeColor = Color.FromArgb(120, 95, 68);
            btnRevert.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);

            btnExportData.BackColor = Color.Transparent;
            btnExportData.ForeColor = Color.FromArgb(120, 95, 68);
            btnExportData.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);

            btnImportData.BackColor = Color.Transparent;
            btnImportData.ForeColor = Color.FromArgb(120, 95, 68);
            btnImportData.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            ApplyCommonButtonTheme();
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkStartMaximized.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkOpenDashboardAtStartup.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkShowNavLoading.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkShowExitPrompt.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkAutoStart.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkMinimizeToTray.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkShowShortcutClearCachePrompt.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkClearCacheOnExit.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblCacheSize.ForeColor = Color.FromArgb(170, 170, 170);
                chkRestoreLastVisitedPage.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkKeepDesktopShortcut.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkNotifyNewContent.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkDisableGpu.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblLayoutMode.ForeColor = UIConstants.DarkMode.ThemeColor;
                cmbLayoutMode.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                cmbLayoutMode.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                grpGeneral.ForeColor = UIConstants.DarkMode.ThemeColor;
                grpAppearance.ForeColor = UIConstants.DarkMode.ThemeColor;
                grpCache.ForeColor = UIConstants.DarkMode.ThemeColor;
                
                btnClose.BackColor = UIConstants.DarkMode.ThemeColor;
                btnClose.ForeColor = Color.White;
                btnClearCache.ForeColor = UIConstants.DarkMode.DangerColor;
                btnClearCache.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;
                btnClearCookies.ForeColor = UIConstants.DarkMode.DangerColor;
                btnClearCookies.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;
                btnClearAll.ForeColor = UIConstants.DarkMode.DangerColor;
                btnClearAll.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;
                btnRevert.ForeColor = UIConstants.DarkMode.ThemeColor;
                btnRevert.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

                btnExportData.ForeColor = UIConstants.DarkMode.ThemeColor;
                btnExportData.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                btnImportData.ForeColor = UIConstants.DarkMode.ThemeColor;
                btnImportData.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            }
            else
            {
                BackColor = Color.White;
                lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
                chkStartMaximized.ForeColor = Color.FromArgb(30, 30, 30);
                chkOpenDashboardAtStartup.ForeColor = Color.FromArgb(30, 30, 30);
                chkShowNavLoading.ForeColor = Color.FromArgb(30, 30, 30);
                chkShowExitPrompt.ForeColor = Color.FromArgb(30, 30, 30);
                chkAutoStart.ForeColor = Color.FromArgb(30, 30, 30);
                chkMinimizeToTray.ForeColor = Color.FromArgb(30, 30, 30);
                chkShowShortcutClearCachePrompt.ForeColor = Color.FromArgb(30, 30, 30);
                chkClearCacheOnExit.ForeColor = Color.FromArgb(30, 30, 30);
                lblCacheSize.ForeColor = Color.FromArgb(100, 100, 100);
                chkRestoreLastVisitedPage.ForeColor = Color.FromArgb(30, 30, 30);
                chkKeepDesktopShortcut.ForeColor = Color.FromArgb(30, 30, 30);
                chkNotifyNewContent.ForeColor = Color.FromArgb(30, 30, 30);
                chkDisableGpu.ForeColor = Color.FromArgb(30, 30, 30);
                lblLayoutMode.ForeColor = Color.FromArgb(74, 59, 42);
                cmbLayoutMode.BackColor = Color.White;
                cmbLayoutMode.ForeColor = Color.Black;

                grpGeneral.ForeColor = Color.FromArgb(74, 59, 42);
                grpAppearance.ForeColor = Color.FromArgb(74, 59, 42);
                grpCache.ForeColor = Color.FromArgb(74, 59, 42);
            }
        }

        private WebView2? GetWebView2()
        {
            var wv = _webViewController.WebView;
            return (wv != null && wv.CoreWebView2 != null) ? wv : null;
        }

        private async void BtnClearCache_Click(object? sender, EventArgs e)
        {
            var wv = GetWebView2();
            if (wv == null)
            {
                CustomMessageBox.Show(this, TextManager.GetString("SettingsWebViewNotInit"), TextManager.GetString("Wait"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                return;
            }

            var result = CustomMessageBox.Show(this,
                TextManager.GetString("SettingsClearCacheConfirm"),
                TextManager.GetString("SettingsClearCache"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, IsDark);

            if (result == DialogResult.Yes)
            {
                btnClearCache.Enabled = false;
                btnClearCache.Text = TextManager.GetString("Clearing");
                try
                {
                    await _webViewService.ClearCacheOnlyAsync(wv);
                    UpdateCacheSizeDisplay();
                    CustomMessageBox.Show(this, TextManager.GetString("SettingsCacheCleared"), TextManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, $"{TextManager.GetString("Error")}: {ex.Message}", TextManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                }
                finally
                {
                    btnClearCache.Text = TextManager.GetString("SettingsClearCache");
                    btnClearCache.Enabled = true;
                }
            }
        }

        private async void BtnClearCookies_Click(object? sender, EventArgs e)
        {
            var wv = GetWebView2();
            if (wv == null)
            {
                CustomMessageBox.Show(this, TextManager.GetString("SettingsWebViewNotInit"), TextManager.GetString("Wait"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                return;
            }

            var result = CustomMessageBox.Show(this,
                TextManager.GetString("SettingsClearCookiesConfirm"),
                TextManager.GetString("SettingsClearCookies"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, IsDark);

            if (result == DialogResult.Yes)
            {
                btnClearCookies.Enabled = false;
                btnClearCookies.Text = TextManager.GetString("Clearing");
                try
                {
                    await _webViewService.ClearCookiesOnlyAsync(wv);
                    UpdateCacheSizeDisplay();
                    CustomMessageBox.Show(this, TextManager.GetString("SettingsCookiesCleared"), TextManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, $"{TextManager.GetString("Error")}: {ex.Message}", TextManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                }
                finally
                {
                    btnClearCookies.Text = TextManager.GetString("SettingsClearCookies");
                    btnClearCookies.Enabled = true;
                }
            }
        }

        private async void BtnClearAll_Click(object? sender, EventArgs e)
        {
            var wv = GetWebView2();
            if (wv == null)
            {
                CustomMessageBox.Show(this, TextManager.GetString("SettingsWebViewNotInit"), TextManager.GetString("Wait"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                return;
            }

            var result = CustomMessageBox.Show(this,
                TextManager.GetString("SettingsClearAllConfirm"),
                TextManager.GetString("SettingsClearAll"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, IsDark);

            if (result == DialogResult.Yes)
            {
                btnClearAll.Enabled = false;
                btnClearAll.Text = TextManager.GetString("Clearing");
                try
                {
                    await _webViewService.ClearAllBrowsingDataAsync(wv);
                    UpdateCacheSizeDisplay();
                    CustomMessageBox.Show(this, TextManager.GetString("SettingsAllCleared"), TextManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, $"{TextManager.GetString("Error")}: {ex.Message}", TextManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                }
                finally
                {
                    btnClearAll.Text = TextManager.GetString("SettingsClearAll");
                    btnClearAll.Enabled = true;
                }
            }
        }

        private void BtnRevert_Click(object? sender, EventArgs e)
        {
            var result = CustomMessageBox.Show(this,
                TextManager.GetString("SettingsRevertConfirm"),
                TextManager.GetString("SettingsRevert"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, IsDark);

            if (result == DialogResult.Yes)
            {
                // Stage the defaults in the dialog; they are persisted only when the user saves.
                _revertRequested = true;
                ResetControlsToDefaults();
            }
        }

        private void ResetControlsToDefaults()
        {
            chkStartMaximized.Checked = true;
            chkOpenDashboardAtStartup.Checked = true;
            chkShowNavLoading.Checked = true;
            chkShowExitPrompt.Checked = true;
            chkAutoStart.Checked = false;
            chkMinimizeToTray.Checked = false;
            chkShowShortcutClearCachePrompt.Checked = true;
            chkClearCacheOnExit.Checked = false;
            chkRestoreLastVisitedPage.Checked = false;
            chkKeepDesktopShortcut.Checked = true;
            chkNotifyNewContent.Checked = true;
            chkDisableGpu.Checked = false;
            cmbLayoutMode.SelectedIndex = 0;
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            _saveRequested = true;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_saveRequested)
            {
                if (_revertRequested)
                {
                    _settingsManager.ResetToDefaults();
                }

                _settingsManager.SetStartMaximized(chkStartMaximized.Checked);
                _settingsManager.SetOpenDashboardAtStartup(chkOpenDashboardAtStartup.Checked);
                _settingsManager.SetShowNavigationLoading(chkShowNavLoading.Checked);
                _settingsManager.SetShowExitPrompt(chkShowExitPrompt.Checked);
                _settingsManager.SetAutoStartEnabled(chkAutoStart.Checked);
                _settingsManager.SetMinimizeToTrayEnabled(chkMinimizeToTray.Checked);
                _settingsManager.SetShowShortcutClearCachePrompt(chkShowShortcutClearCachePrompt.Checked);
                _settingsManager.SetClearCacheOnExit(chkClearCacheOnExit.Checked);
                _settingsManager.SetRestoreLastVisitedPage(chkRestoreLastVisitedPage.Checked);
                _settingsManager.SetKeepDesktopShortcut(chkKeepDesktopShortcut.Checked);
                _settingsManager.SetNotifyNewContent(chkNotifyNewContent.Checked);
                _settingsManager.SetDisableGpuAcceleration(chkDisableGpu.Checked);

                var selectedLayout = cmbLayoutMode.SelectedIndex switch
                {
                    1 => "tablet",
                    2 => "mobile",
                    _ => "desktop"
                };
                var oldLayout = _settingsManager.GetLayoutMode();
                _settingsManager.SetLayoutMode(selectedLayout);

                if (selectedLayout != oldLayout)
                {
                    _ = ApplyLayoutChangeAsync(selectedLayout);
                }

                DialogResult = DialogResult.OK;
            }

            base.OnFormClosing(e);
        }

        private async Task ApplyLayoutChangeAsync(string selectedLayout)
        {
            try
            {
                await _webViewController.UpdateUserAgentAsync(selectedLayout);
                _webViewController.Reload();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply layout change: {ex}");
            }
        }
    }
}

