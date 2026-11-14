using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Controllers;
using Kopolart.Windows.UI.Forms;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Interfaces;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls.NavBar
{
    /// <summary>
    /// Handles bookmarks menu generation, dynamic recent items, bookmark toggle, and management dialog.
    /// </summary>
    public sealed class NavBarBookmarksHandler
    {
        private readonly IMainFormActions _parentForm;
        private readonly WebViewController _webViewController;
        private readonly ISettingsManager _settingsManager;
        private readonly Func<bool> _isDarkModeProvider;

        public ToolStripMenuItem BookmarksMenu { get; }
        public ToolStripButton? BookmarkPageButton { get; set; }

        public NavBarBookmarksHandler(
            IMainFormActions parentForm,
            WebViewController webViewController,
            ISettingsManager settingsManager,
            Func<bool> isDarkModeProvider)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _webViewController = webViewController ?? throw new ArgumentNullException(nameof(webViewController));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));

            BookmarksMenu = new ToolStripMenuItem
            {
                Name = "bookmarksToolStripMenuItem",
                Text = TextManager.GetString("MenuBookmarks")
            };
            BookmarksMenu.DropDownOpening += (s, e) => RebuildBookmarksMenu();
        }

        public void UpdateBookmarkButtonState()
        {
            if (BookmarkPageButton == null) return;
            var url = _webViewController.WebView?.Source?.ToString();
            bool isBookmarked = !string.IsNullOrWhiteSpace(url) && url != "about:blank" && _settingsManager.IsBookmarked(url);

            BookmarkPageButton.Text = isBookmarked ? "\uE735" : "\uE734";
            BookmarkPageButton.ToolTipText = isBookmarked
                ? TextManager.GetString("RemoveCurrentBookmark")
                : TextManager.GetString("BookmarkCurrentPage");
        }

        public void ToggleCurrentPageBookmark()
        {
            var url = _webViewController.WebView?.Source?.ToString();
            if (string.IsNullOrWhiteSpace(url) || url == "about:blank")
                return;

            bool isDarkMode = _isDarkModeProvider();
            if (_settingsManager.IsBookmarked(url))
            {
                _settingsManager.RemoveBookmark(url);
                CustomMessageBox.Show(_parentForm,
                    TextManager.GetString("BookmarkRemoved"),
                    TextManager.GetString("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information, isDarkMode);
            }
            else
            {
                var title = _webViewController.DocumentTitle;
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = url;
                }
                _settingsManager.AddBookmark(new BookmarkItem { Title = title, Url = url });
                CustomMessageBox.Show(_parentForm,
                    TextManager.GetString("BookmarkAdded"),
                    TextManager.GetString("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information, isDarkMode);
            }

            UpdateBookmarkButtonState();
        }

        public void OpenManageBookmarksDialog()
        {
            using var dialog = new ManageBookmarksDialog(_settingsManager, _isDarkModeProvider(), (url) => {
                _parentForm.ShowWebView(url);
            });
            dialog.ShowDialog(_parentForm.AsForm);
        }

        public void RebuildBookmarksMenu()
        {
            BookmarksMenu.DropDownItems.Clear();

            var url = _webViewController.WebView?.Source?.ToString();
            bool isBookmarked = !string.IsNullOrWhiteSpace(url) && url != "about:blank" && _settingsManager.IsBookmarked(url);

            // 1. Add / Remove Bookmark Current Page Item
            string addRemoveText = isBookmarked
                ? TextManager.GetString("RemoveCurrentBookmark")
                : TextManager.GetString("BookmarkCurrentPage");

            var addRemoveItem = new ToolStripMenuItem(addRemoveText, null, (s, e) =>
            {
                ToggleCurrentPageBookmark();
                UpdateBookmarkButtonState();
            })
            {
                ShortcutKeys = Keys.Control | Keys.D,
                Enabled = !string.IsNullOrWhiteSpace(url) && url != "about:blank"
            };
            ApplyMenuItemFont(addRemoveItem);

            // 2. Manage Bookmarks Item
            var manageItem = new ToolStripMenuItem(TextManager.GetString("ManageBookmarks"), null, (s, e) => OpenManageBookmarksDialog());
            ApplyMenuItemFont(manageItem);

            BookmarksMenu.DropDownItems.Add(addRemoveItem);
            BookmarksMenu.DropDownItems.Add(manageItem);
            BookmarksMenu.DropDownItems.Add(new ToolStripSeparator());

            // 3. Dynamic Bookmarks List
            var bookmarks = _settingsManager.GetBookmarks();
            if (bookmarks == null || bookmarks.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem(TextManager.GetString("NoBookmarks"))
                {
                    Enabled = false
                };
                ApplyMenuItemFont(emptyItem);
                BookmarksMenu.DropDownItems.Add(emptyItem);
            }
            else
            {
                // Take 10 most recent
                var recentBookmarks = bookmarks.OrderByDescending(b => b.DateAdded).Take(10).ToList();

                foreach (var b in recentBookmarks)
                {
                    string displayName = b.Title ?? string.Empty;
                    if (displayName.Length > 35)
                    {
                        displayName = displayName.Substring(0, 32).TrimEnd() + "...";
                    }

                    var bItem = new ToolStripMenuItem(displayName, null, (s, e) => _parentForm.ShowWebView(b.Url))
                    {
                        ToolTipText = $"{b.Title}\n{b.Url}"
                    };
                    ApplyMenuItemFont(bItem);
                    BookmarksMenu.DropDownItems.Add(bItem);
                }

                if (bookmarks.Count > 10)
                {
                    BookmarksMenu.DropDownItems.Add(new ToolStripSeparator());
                    string showAllText = TextManager.GetString("ShowAllBookmarks");
                    var showAllItem = new ToolStripMenuItem(showAllText, null, (s, e) => OpenManageBookmarksDialog());
                    ApplyMenuItemFont(showAllItem);
                    BookmarksMenu.DropDownItems.Add(showAllItem);
                }
            }
        }

        private static void ApplyMenuItemFont(ToolStripMenuItem item)
        {
            item.Font = FontManager.CreateFont(item.Font.Size, item.Font.Style);
        }
    }
}

