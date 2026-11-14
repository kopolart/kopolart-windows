using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Forms;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls.NavBar
{
    /// <summary>
    /// Builds and wires the MenuStrip items and dialog handlers for the application navigation bar.
    /// </summary>
    public static class NavBarMenuBuilder
    {
        public static NavBarMenuItems Build(
            MenuStrip menuStrip,
            IMainFormActions parentForm,
            WebViewController webViewController,
            ISettingsManager settingsManager,
            IWebViewService webViewService,
            Func<bool> isDarkModeProvider,
            ToolStripMenuItem bookmarksMenu,
            Action openHistoryDialog,
            Action openDownloadsDialog,
            Func<Task> checkForUpdatesManualAsync,
            IUpdateService updateService,
            DataBackupCoordinator? backupCoordinator = null)
        {
            // 1. File Menu
            var exportItem = new ToolStripMenuItem(TextManager.GetString("MenuExportData"))
            {
                Name = "exportDataToolStripMenuItem"
            };
            exportItem.Click += async (s, e) =>
            {
                if (backupCoordinator != null)
                {
                    await backupCoordinator.ExportDataAsync();
                }
            };

            var importItem = new ToolStripMenuItem(TextManager.GetString("MenuImportData"))
            {
                Name = "importDataToolStripMenuItem"
            };
            importItem.Click += async (s, e) =>
            {
                if (backupCoordinator != null)
                {
                    await backupCoordinator.ImportDataAsync();
                }
            };

            var exitItem = new ToolStripMenuItem(TextManager.GetString("MenuExit")) { Name = "exitToolStripMenuItem" };
            exitItem.Click += (s, e) => parentForm.PromptExit();

            var fileMenu = new ToolStripMenuItem(TextManager.GetString("MenuFile")) { Name = "fileToolStripMenuItem" };
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                exportItem,
                importItem,
                new ToolStripSeparator(),
                exitItem
            });

            // 2. Tools Menu
            var searchItem = new ToolStripMenuItem(TextManager.GetString("SearchInPage"))
            {
                Name = "searchToolStripMenuItem",
                ShortcutKeys = Keys.Control | Keys.F,
                ShortcutKeyDisplayString = "Ctrl+F"
            };
            searchItem.Click += (s, e) => webViewController.ShowFindUI();

            var historyItem = new ToolStripMenuItem(TextManager.GetString("MenuHistory"))
            {
                Name = "historyToolStripMenuItem",
                ShortcutKeys = Keys.Control | Keys.H,
                ShortcutKeyDisplayString = "Ctrl+H"
            };
            historyItem.Click += (s, e) => openHistoryDialog();

            var downloadsItem = new ToolStripMenuItem(TextManager.GetString("MenuDownloads"))
            {
                Name = "downloadsToolStripMenuItem",
                ShortcutKeys = Keys.Control | Keys.J,
                ShortcutKeyDisplayString = "Ctrl+J"
            };
            downloadsItem.Click += (s, e) => openDownloadsDialog();

            var settingsItem = new ToolStripMenuItem(TextManager.GetString("MenuSettings")) { Name = "settingsToolStripMenuItem" };
            settingsItem.Click += (s, e) =>
            {
                using var settingsDialog = new SettingsDialog(settingsManager, webViewService, webViewController, isDarkModeProvider(), backupCoordinator);
                if (settingsDialog.ShowDialog(parentForm.AsForm) == DialogResult.OK)
                {
                    SystemMenuHelper.UpdateStartMaximizedMenuCheck(parentForm.AsForm, settingsManager.GetStartMaximized());
                    SystemMenuHelper.UpdateNavLoadingMenuCheck(parentForm.AsForm, settingsManager.GetShowNavigationLoading());
                }
            };

            var dailyUsageItem = new ToolStripMenuItem(TextManager.GetString("MenuDailyUsage"))
            {
                Name = "dailyUsageToolStripMenuItem"
            };
            dailyUsageItem.Click += (s, e) =>
            {
                using var logsDialog = new AppLogsDialog(
                    settingsManager,
                    isDarkModeProvider(),
                    parentForm.SessionStartTime,
                    parentForm.UsageTracker,
                    initialTab: 0);
                logsDialog.ShowDialog(parentForm);
            };

            var appLogsItem = new ToolStripMenuItem(TextManager.GetString("AppLogsTitle"))
            {
                Name = "appLogsToolStripMenuItem"
            };
            appLogsItem.Click += (s, e) =>
            {
                using var logsDialog = new AppLogsDialog(
                    settingsManager,
                    isDarkModeProvider(),
                    parentForm.SessionStartTime,
                    parentForm.UsageTracker,
                    initialTab: 1);
                logsDialog.ShowDialog(parentForm);
            };

            var toolsMenu = new ToolStripMenuItem(TextManager.GetString("MenuTools")) { Name = "toolsToolStripMenuItem" };
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                searchItem,
                historyItem,
                downloadsItem,
                new ToolStripSeparator(),
                settingsItem,
                dailyUsageItem,
                appLogsItem
            });

            // 3. Help Menu
            var faqItem = new ToolStripMenuItem(TextManager.GetString("MenuFaq"))
            {
                Name = "faqToolStripMenuItem"
            };
            faqItem.Click += (s, e) => parentForm.ShowWebView($"https://{AppConstants.Urls.Domain}/faq");

            var shortcutsItem = new ToolStripMenuItem(TextManager.GetString("MenuShortcuts"))
            {
                Name = "shortcutsToolStripMenuItem"
            };
            shortcutsItem.Click += (s, e) =>
            {
                using var dialog = new ShortcutsDialog(isDarkModeProvider());
                dialog.ShowDialog(parentForm.AsForm);
            };

            var contactUsItem = new ToolStripMenuItem(TextManager.GetString("MenuContactPage"))
            {
                Name = "contactUsToolStripMenuItem"
            };
            contactUsItem.Click += (s, e) => parentForm.ShowWebView($"https://{AppConstants.Urls.Domain}/contact-us");

            var githubItem = new ToolStripMenuItem(TextManager.GetString("MenuGithub"))
            {
                Name = "githubToolStripMenuItem"
            };
            githubItem.Click += (s, e) => WebView2SecurityManager.TryOpenExternalUri(AppConstants.Urls.GithubUrl);

            var donateItem = new ToolStripMenuItem(TextManager.GetString("MenuDonate"))
            {
                Name = "donateToolStripMenuItem"
            };
            donateItem.Click += (s, e) =>
            {
                if (webViewController.WebView?.CoreWebView2 != null)
                {
                    parentForm.ShowWebView(AppConstants.Urls.DonateUrl);
                }
                else
                {
                    WebView2SecurityManager.TryOpenExternalUri(AppConstants.Urls.DonateUrl);
                }
            };

            var aboutUsItem = new ToolStripMenuItem(TextManager.GetString("MenuAboutUs"))
            {
                Name = "aboutUsToolStripMenuItem"
            };
            aboutUsItem.Click += (s, e) => parentForm.ShowWebView($"https://{AppConstants.Urls.Domain}/about-us");

            var termsItem = new ToolStripMenuItem(TextManager.GetString("MenuTerms"))
            {
                Name = "termsToolStripMenuItem"
            };
            termsItem.Click += (s, e) => parentForm.ShowWebView($"https://{AppConstants.Urls.Domain}/terms");

            var licenseItem = new ToolStripMenuItem(TextManager.GetString("MenuLicense"))
            {
                Name = "licenseToolStripMenuItem"
            };
            licenseItem.Click += (s, e) =>
            {
                if (webViewController.WebView?.CoreWebView2 != null)
                {
                    webViewController.WebView.CoreWebView2.Navigate(AppConstants.Urls.LicenseUrl);
                }
                else
                {
                    WebView2SecurityManager.TryOpenExternalUri(AppConstants.Urls.LicenseUrl);
                }
            };

            var updatesItem = new ToolStripMenuItem(TextManager.GetString("MenuUpdates"))
            {
                Name = "updatesToolStripMenuItem"
            };
            updatesItem.Click += async (s, e) => await checkForUpdatesManualAsync();

            var updatesHistoryItem = new ToolStripMenuItem(TextManager.GetString("MenuUpdatesHistory"))
            {
                Name = "updatesHistoryToolStripMenuItem"
            };
            updatesHistoryItem.Click += (s, e) =>
            {
                using var dialog = new UpdatesHistoryDialog(updateService, isDarkModeProvider(), checkForUpdatesManualAsync);
                dialog.ShowDialog(parentForm.AsForm);
            };

            var aboutItem = new ToolStripMenuItem(TextManager.GetString("MenuAbout")) { Name = "aboutToolStripMenuItem" };
            aboutItem.Click += (s, e) =>
            {
                var version = System.Windows.Forms.Application.ProductVersion;
                string format = TextManager.GetString("AboutText");
                string aboutText = string.Format(format, version);
                string aboutTitle = TextManager.GetString("MenuAbout");
                CustomMessageBox.Show(parentForm, aboutText, aboutTitle, MessageBoxButtons.OK, MessageBoxIcon.Information, isDarkModeProvider());
            };

            var contactMenu = new ToolStripMenuItem(TextManager.GetString("MenuContactUs")) { Name = "contactMenuToolStripMenuItem" };
            contactMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                contactUsItem,
                githubItem,
                new ToolStripSeparator(),
                donateItem
            });

            var helpMenu = new ToolStripMenuItem(TextManager.GetString("MenuHelp")) { Name = "helpToolStripMenuItem" };
            helpMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                faqItem,
                shortcutsItem,
                new ToolStripSeparator(),
                aboutUsItem,
                termsItem,
                licenseItem,
                new ToolStripSeparator(),
                updatesItem,
                updatesHistoryItem,
                aboutItem
            });

            // 4. Project Order Link
            var projectOrderItem = new ToolStripMenuItem
            {
                Name = "projectOrderToolStripMenuItem",
                Text = TextManager.GetString("MenuProjectOrder"),
                ForeColor = UIConstants.ProjectOrderColor
            };
            projectOrderItem.Click += (s, e) =>
            {
                _ = webViewController.AppVisitTrackerService?.TrackVisitAsync(AppConstants.PartialVisitKeys.JosephyBrowse);
                WebView2SecurityManager.TryOpenExternalUri("https://josephy.ir");
            };

            // 5. Add all top-level menus to MenuStrip
            menuStrip.Items.AddRange(new ToolStripItem[]
            {
                fileMenu,
                toolsMenu,
                bookmarksMenu,
                contactMenu,
                helpMenu,
                projectOrderItem
            });

            return new NavBarMenuItems
            {
                FileMenu = fileMenu,
                ExportDataMenuItem = exportItem,
                ImportDataMenuItem = importItem,
                ExitMenuItem = exitItem,
                ToolsMenu = toolsMenu,
                SearchMenuItem = searchItem,
                SettingsMenuItem = settingsItem,
                DailyUsageMenuItem = dailyUsageItem,
                AppLogsMenuItem = appLogsItem,
                HistoryMenuItem = historyItem,
                DownloadsMenuItem = downloadsItem,
                BookmarksMenu = bookmarksMenu,
                ContactMenu = contactMenu,
                HelpMenu = helpMenu,
                FaqMenuItem = faqItem,
                ShortcutsMenuItem = shortcutsItem,
                ContactUsMenuItem = contactUsItem,
                GithubMenuItem = githubItem,
                DonateMenuItem = donateItem,
                AboutUsMenuItem = aboutUsItem,
                TermsMenuItem = termsItem,
                LicenseMenuItem = licenseItem,
                UpdatesMenuItem = updatesItem,
                AboutMenuItem = aboutItem,
                ProjectOrderMenuItem = projectOrderItem
            };
        }
    }
}

