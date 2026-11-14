using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls.NavBar
{
    /// <summary>
    /// Holds strongly-typed references to all menu items constructed in AppNavigationBar.
    /// </summary>
    public sealed class NavBarMenuItems
    {
        public ToolStripMenuItem FileMenu { get; set; } = null!;
        public ToolStripMenuItem ExportDataMenuItem { get; set; } = null!;
        public ToolStripMenuItem ImportDataMenuItem { get; set; } = null!;
        public ToolStripMenuItem ExitMenuItem { get; set; } = null!;
        public ToolStripMenuItem ToolsMenu { get; set; } = null!;
        public ToolStripMenuItem SearchMenuItem { get; set; } = null!;
        public ToolStripMenuItem SettingsMenuItem { get; set; } = null!;
        public ToolStripMenuItem DailyUsageMenuItem { get; set; } = null!;
        public ToolStripMenuItem AppLogsMenuItem { get; set; } = null!;
        public ToolStripMenuItem HistoryMenuItem { get; set; } = null!;
        public ToolStripMenuItem DownloadsMenuItem { get; set; } = null!;
        public ToolStripMenuItem BookmarksMenu { get; set; } = null!;
        public ToolStripMenuItem ContactMenu { get; set; } = null!;
        public ToolStripMenuItem HelpMenu { get; set; } = null!;
        public ToolStripMenuItem FaqMenuItem { get; set; } = null!;
        public ToolStripMenuItem ShortcutsMenuItem { get; set; } = null!;
        public ToolStripMenuItem ContactUsMenuItem { get; set; } = null!;
        public ToolStripMenuItem GithubMenuItem { get; set; } = null!;
        public ToolStripMenuItem DonateMenuItem { get; set; } = null!;
        public ToolStripMenuItem AboutUsMenuItem { get; set; } = null!;
        public ToolStripMenuItem TermsMenuItem { get; set; } = null!;
        public ToolStripMenuItem LicenseMenuItem { get; set; } = null!;
        public ToolStripMenuItem UpdatesMenuItem { get; set; } = null!;
        public ToolStripMenuItem AboutMenuItem { get; set; } = null!;
        public ToolStripMenuItem ProjectOrderMenuItem { get; set; } = null!;
    }

    /// <summary>
    /// Handles text updates across all navigation bar buttons, menus, tooltips, and RTL layout arrows.
    /// </summary>
    public sealed class NavBarTextHandler
    {
        private readonly IMainFormActions _parentForm;
        private readonly ISettingsManager _settingsManager;
        private readonly NavBarMenuItems _menuItems;
        private readonly NavBarNavigationButtonsHandler _buttonsHandler;
        private readonly NavBarBookmarksHandler _bookmarksHandler;
        private readonly NavBarUpdateHandler _updateHandler;
        private readonly Action _onUpdateHeight;

        public NavBarTextHandler(
            IMainFormActions parentForm,
            ISettingsManager settingsManager,
            NavBarMenuItems menuItems,
            NavBarNavigationButtonsHandler buttonsHandler,
            NavBarBookmarksHandler bookmarksHandler,
            NavBarUpdateHandler updateHandler,
            Action onUpdateHeight)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _menuItems = menuItems ?? throw new ArgumentNullException(nameof(menuItems));
            _buttonsHandler = buttonsHandler ?? throw new ArgumentNullException(nameof(buttonsHandler));
            _bookmarksHandler = bookmarksHandler ?? throw new ArgumentNullException(nameof(bookmarksHandler));
            _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
            _onUpdateHeight = onUpdateHeight ?? throw new ArgumentNullException(nameof(onUpdateHeight));
        }

        public void ApplyTexts()
        {
            // Update Navigation buttons tooltips and arrow directions for RTL
            _buttonsHandler.DashboardButton.ToolTipText = TextManager.GetString("NavDashboardTooltip");
            _buttonsHandler.BackButton.ToolTipText = TextManager.GetString("NavBackTooltip");
            _buttonsHandler.ForwardButton.ToolTipText = TextManager.GetString("NavForwardTooltip");
            _buttonsHandler.RefreshButton.ToolTipText = TextManager.GetString("NavRefreshTooltip");
            _buttonsHandler.SearchButton.ToolTipText = TextManager.GetString("SearchInPageTooltip");
            _buttonsHandler.CopyLinkButton.ToolTipText = TextManager.GetString("CopyLinkTooltip");
            _buttonsHandler.OpenInBrowserButton.ToolTipText = TextManager.GetString("OpenInBrowser");

            // Fixed RTL arrows for Persian
            _buttonsHandler.BackButton.Text = "\uE72A"; // Right arrow acts as Back in RTL layout
            _buttonsHandler.ForwardButton.Text = "\uE72B"; // Left arrow acts as Forward in RTL layout

            _bookmarksHandler.UpdateBookmarkButtonState();

            // Update Menu texts
            _menuItems.FileMenu.Text = TextManager.GetString("MenuFile");
            _menuItems.ExportDataMenuItem.Text = TextManager.GetString("MenuExportData");
            _menuItems.ImportDataMenuItem.Text = TextManager.GetString("MenuImportData");
            _menuItems.ExitMenuItem.Text = TextManager.GetString("MenuExit");
            _menuItems.ToolsMenu.Text = TextManager.GetString("MenuTools");
            _menuItems.SettingsMenuItem.Text = TextManager.GetString("MenuSettings");
            _menuItems.DailyUsageMenuItem.Text = TextManager.GetString("MenuDailyUsage");
            _menuItems.AppLogsMenuItem.Text = TextManager.GetString("MenuAppLogs");
            _menuItems.AboutMenuItem.Text = TextManager.GetString("MenuAbout");
            _menuItems.SearchMenuItem.Text = TextManager.GetString("SearchInPage");
            _menuItems.BookmarksMenu.Text = TextManager.GetString("MenuBookmarks");
            _menuItems.ContactMenu.Text = TextManager.GetString("MenuContactUs");
            _menuItems.ContactUsMenuItem.Text = TextManager.GetString("MenuContactPage");
            _menuItems.GithubMenuItem.Text = TextManager.GetString("MenuGithub");
            _menuItems.DonateMenuItem.Text = TextManager.GetString("MenuDonate");
            _menuItems.AboutUsMenuItem.Text = TextManager.GetString("MenuAboutUs");
            _menuItems.FaqMenuItem.Text = TextManager.GetString("MenuFaq");
            _menuItems.TermsMenuItem.Text = TextManager.GetString("MenuTerms");
            _menuItems.LicenseMenuItem.Text = TextManager.GetString("MenuLicense");
            _menuItems.ProjectOrderMenuItem.Text = TextManager.GetString("MenuProjectOrder");
            _menuItems.ShortcutsMenuItem.Text = TextManager.GetString("MenuShortcuts");
            _menuItems.HistoryMenuItem.Text = TextManager.GetString("MenuHistory");
            _menuItems.DownloadsMenuItem.Text = TextManager.GetString("MenuDownloads");

            _updateHandler.UpdateMenuTextsWithSpacing(MenuUpdateNotifier.IsUpdateAvailable);

            // Update System Menu
            ApplySystemMenuTexts();

            // Dynamically adjust control height based on preferred size
            _onUpdateHeight();
        }

        public void ApplySystemMenuTexts()
        {
            SystemMenuHelper.InitializeSystemMenu(
                _parentForm.AsForm,
                _settingsManager.GetStartMaximized(),
                _settingsManager.GetShowNavigationLoading());
        }
    }
}
