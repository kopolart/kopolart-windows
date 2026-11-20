using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    /// <summary>
    /// Modern, luxury Instant Search Bar control for DashboardView:
    /// - Category selector (Posts, Colors, Palettes, Wallpapers) with icon and accent color
    /// - Instant auto-complete suggestions dropdown with thumbnails, titles, and URLs
    /// - 350ms debouncing, in-flight cancellation, in-memory caching
    /// - Responsive, theme-aware (Light/Dark), full RTL and Persian text support
    /// </summary>
    public class DashboardSearchBar : Control
    {
        private readonly ISettingsManager _settingsManager;
        private readonly ISearchSuggestionService? _searchSuggestionService;
        private bool _isDark;

        // UI Components
        private readonly TextBox _txtSearch;
        private readonly System.Windows.Forms.Timer _debounceTimer;
        private CancellationTokenSource? _searchCts;

        // Category selection
        private SearchCategoryType _selectedCategory = SearchCategoryType.Posts;
        private readonly List<CategoryMeta> _categories = new();

        // Interactive bounding boxes for custom drawing
        private Rectangle _categoryBtnRect;
        private Rectangle _searchBtnRect;
        private Rectangle _clearBtnRect;

        private bool _isCategoryBtnHovered;
        private bool _isSearchBtnHovered;
        private bool _isClearBtnHovered;

        // Suggestions Dropdown Popup
        private ToolStripDropDown? _suggestionsDropDown;
        private SearchSuggestionsPopupControl? _suggestionsControl;
        private ToolStripControlHost? _suggestionsHost;

        // Static thumbnail cache to prevent duplicate image downloads across suggestion refreshes
        private static readonly ConcurrentDictionary<string, Image> ThumbnailCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HttpClient ImageHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

        // Events
        public event EventHandler<string>? SearchRequested;
        public event EventHandler<string>? RecentSearchAdded;

        private record CategoryMeta(
            SearchCategoryType Type,
            string Title,
            string ShortTitle,
            string Glyph,
            Color AccentLight,
            Color AccentDark,
            string Placeholder,
            string SearchUrl)
        {
            public Color GetAccent(bool isDark) => isDark ? AccentDark : AccentLight;
        }

        public DashboardSearchBar(
            ISettingsManager settingsManager,
            ISearchSuggestionService? searchSuggestionService,
            bool isDark)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _searchSuggestionService = searchSuggestionService;
            _isDark = isDark;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;
            RightToLeft = RightToLeft.Yes;
            Height = 52;
            Cursor = Cursors.Default;

            InitializeCategories();
            RestoreLastSelectedCategory();

            // Setup input TextBox
            _txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                RightToLeft = RightToLeft.Yes,
                Font = FontManager.CreateFont(10.5F, FontStyle.Regular),
                Multiline = false
            };
            _txtSearch.TextChanged += OnSearchTextChanged;
            _txtSearch.KeyDown += OnSearchKeyDown;
            _txtSearch.Enter += (s, e) => Invalidate();
            _txtSearch.Leave += (s, e) => Invalidate();
            Controls.Add(_txtSearch);

            // Debounce timer (350ms)
            _debounceTimer = new System.Windows.Forms.Timer { Interval = 350 };
            _debounceTimer.Tick += OnDebounceTimerTick;

            ApplyTheme(_isDark);
            UpdateCategoryState();
        }

        private void InitializeCategories()
        {
            _categories.Clear();

            _categories.Add(new CategoryMeta(
                SearchCategoryType.Posts,
                TextManager.GetString("SearchCategoryPosts"),
                TextManager.GetString("SearchCategoryPosts"),
                KopolArtFaIcons.Newspaper,
                Color.FromArgb(0x24, 0x9C, 0xF0),
                Color.FromArgb(0x60, 0xA5, 0xFA),
                TextManager.GetString("SearchPlaceholderPosts"),
                AppConstants.Urls.PostsSearch));

            _categories.Add(new CategoryMeta(
                SearchCategoryType.Palettes,
                TextManager.GetString("SearchCategoryPalettes"),
                TextManager.GetString("SearchCategoryPalettes"),
                KopolArtFaIcons.Palette,
                Color.FromArgb(0x0D, 0x94, 0x88),
                Color.FromArgb(0x2D, 0xD4, 0xBF),
                TextManager.GetString("SearchPlaceholderPalettes"),
                AppConstants.Urls.ColorPalettesSearch));

            _categories.Add(new CategoryMeta(
                SearchCategoryType.Colors,
                TextManager.GetString("SearchCategoryColors"),
                TextManager.GetString("SearchCategoryColors"),
                KopolArtFaIcons.Sun,
                Color.FromArgb(0xD9, 0x77, 0x06),
                Color.FromArgb(0xFB, 0xBF, 0x24),
                TextManager.GetString("SearchPlaceholderColors"),
                AppConstants.Urls.ColorsSearch));

            _categories.Add(new CategoryMeta(
                SearchCategoryType.Wallpapers,
                TextManager.GetString("SearchCategoryWallpapers"),
                TextManager.GetString("SearchCategoryWallpapers"),
                KopolArtFaIcons.Image,
                Color.FromArgb(0x93, 0x33, 0xEA),
                Color.FromArgb(0xC0, 0x84, 0xFC),
                TextManager.GetString("SearchPlaceholderWallpapers"),
                AppConstants.Urls.WallpapersSearch));
        }

        private void RestoreLastSelectedCategory()
        {
            try
            {
                var saved = _settingsManager.GetLastSelectedSearchCategory();
                if (Enum.TryParse<SearchCategoryType>(saved, true, out var type))
                {
                    _selectedCategory = type;
                }
            }
            catch
            {
                _selectedCategory = SearchCategoryType.Posts;
            }
        }

        private CategoryMeta CurrentCategory =>
            _categories.Find(c => c.Type == _selectedCategory) ?? _categories[0];

        public void ApplyTheme(bool isDark)
        {
            _isDark = isDark;

            Color cardBg = isDark
                ? UIConstants.DarkMode.CardBackgroundDark
                : UIConstants.CardBackgroundLight;

            Color foreColor = isDark
                ? UIConstants.DarkMode.MenuStripForeColor
                : UIConstants.TextThemeColor;

            BackColor = isDark
                ? UIConstants.DarkMode.BrandBackground
                : UIConstants.BrandBackground;

            _txtSearch.BackColor = cardBg;
            _txtSearch.ForeColor = foreColor;

            _suggestionsControl?.ApplyTheme(isDark);
            Invalidate();
        }

        public void ApplyTexts()
        {
            InitializeCategories();
            UpdateCategoryState();
        }

        private void UpdateCategoryState()
        {
            var cat = CurrentCategory;
            _txtSearch.PlaceholderText = cat.Placeholder;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutSearchControls();
        }

        private void LayoutSearchControls()
        {
            int h = Height;
            int w = Width;
            if (w <= 0 || h <= 0) return;

            int paddingY = 6;
            int innerH = h - (paddingY * 2);

            // In RTL:
            // Right Side: Category Picker Button (width ~110)
            int catBtnW = 112;
            _categoryBtnRect = new Rectangle(w - 7 - catBtnW, paddingY, catBtnW, innerH);

            // Left Side: Search Action Button (38x38)
            int searchBtnW = 38;
            _searchBtnRect = new Rectangle(7, paddingY + (innerH - searchBtnW) / 2, searchBtnW, searchBtnW);

            // Clear Button (placed right next to Search button if text entered)
            bool hasText = !string.IsNullOrEmpty(_txtSearch.Text);
            int clearBtnW = hasText ? 28 : 0;
            if (hasText)
            {
                _clearBtnRect = new Rectangle(_searchBtnRect.Right + 6, paddingY + (innerH - 28) / 2, 28, 28);
            }
            else
            {
                _clearBtnRect = Rectangle.Empty;
            }

            // Text Box: occupies space between Clear/Search button and Category button
            int txtLeft = hasText ? _clearBtnRect.Right + 8 : _searchBtnRect.Right + 8;
            int txtRight = _categoryBtnRect.Left - 10;
            int txtW = Math.Max(50, txtRight - txtLeft);
            int txtH = _txtSearch.PreferredHeight;
            int txtY = (h - txtH) / 2;

            _txtSearch.SetBounds(txtLeft, txtY, txtW, txtH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = Width;
            int h = Height;
            if (w <= 0 || h <= 0) return;

            var cat = CurrentCategory;
            bool highContrast = SystemAccessibility.IsHighContrast;
            Color accentColor = highContrast ? SystemColors.Highlight : cat.GetAccent(_isDark);

            // 1. Draw Card Background and Border
            Color cardBg = highContrast
                ? SystemColors.Window
                : _isDark
                    ? UIConstants.DarkMode.CardBackgroundDark
                    : UIConstants.CardBackgroundLight;

            Color cardBorder = highContrast
                ? SystemColors.ControlDark
                : _isDark
                    ? UIConstants.DarkMode.CardBorderDark
                    : UIConstants.CardBorderLight;

            var cardRect = new RectangleF(0.5f, 0.5f, w - 1f, h - 1f);
            using (var cardPath = CreateRoundedRectanglePath(cardRect, 10f))
            {
                using var bgBrush = new SolidBrush(cardBg);
                g.FillPath(bgBrush, cardPath);

                // If textbox is focused or suggestions open, subtle accent border
                bool isFocused = _txtSearch.Focused || (_suggestionsDropDown?.Visible == true);
                Color borderPenColor = isFocused ? accentColor : cardBorder;
                float borderWidth = isFocused ? 1.5f : 1.0f;

                using var borderPen = new Pen(borderPenColor, borderWidth);
                g.DrawPath(borderPen, cardPath);
            }

            // 2. Draw Category Selector Button (Pill on the Right in RTL)
            DrawCategoryButton(g, cat, accentColor);

            // 3. Draw Vertical Divider between Category Button and Input
            int dividerX = _categoryBtnRect.Left - 5;
            using (var dividerPen = new Pen(highContrast
                ? SystemColors.ControlDark
                : _isDark ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0), 1f))
            {
                g.DrawLine(dividerPen, dividerX, 10, dividerX, h - 10);
            }

            // 4. Draw Clear Button if active
            if (!_clearBtnRect.IsEmpty)
            {
                DrawClearButton(g);
            }

            // 5. Draw Search Action Button (Left side in RTL)
            DrawSearchButton(g, accentColor);
        }

        private void DrawCategoryButton(Graphics g, CategoryMeta cat, Color accentColor)
        {
            var btnRect = new RectangleF(_categoryBtnRect.X, _categoryBtnRect.Y, _categoryBtnRect.Width, _categoryBtnRect.Height);
            using var btnPath = CreateRoundedRectanglePath(btnRect, 7f);

            // Soft tinted background
            Color pillBg = _isCategoryBtnHovered
                ? Color.FromArgb(_isDark ? 55 : 35, accentColor)
                : Color.FromArgb(_isDark ? 30 : 18, accentColor);

            using (var pillBrush = new SolidBrush(pillBg))
            {
                g.FillPath(pillBrush, btnPath);
            }

            using (var pillBorderPen = new Pen(Color.FromArgb(_isCategoryBtnHovered ? 120 : 60, accentColor), 1f))
            {
                g.DrawPath(pillBorderPen, btnPath);
            }

            // Draw Category Icon + Title + Downward Caret
            // Layout inside category button (RTL):
            // Icon on right side of pill, Title in middle, Caret on left side of pill
            float rightPadding = btnRect.Right - 8;
            var iconFont = FontManager.GetIconFont(11f);
            using var iconBrush = new SolidBrush(accentColor);

            // Icon
            var iconSize = g.MeasureString(cat.Glyph, iconFont);
            float iconX = rightPadding - iconSize.Width;
            float iconY = btnRect.Y + (btnRect.Height - iconSize.Height) / 2f;
            g.DrawString(cat.Glyph, iconFont, iconBrush, iconX, iconY);

            // Title
            var titleFont = FontManager.GetFont(9f, FontStyle.Bold);
            Color textClr = _isDark ? UIConstants.DarkMode.MenuStripForeColor : UIConstants.TextThemeColor;
            using var titleBrush = new SolidBrush(textClr);

            var titleSize = g.MeasureString(cat.ShortTitle, titleFont);
            float titleX = iconX - titleSize.Width - 4;
            float titleY = btnRect.Y + (btnRect.Height - titleSize.Height) / 2f;
            g.DrawString(cat.ShortTitle, titleFont, titleBrush, titleX, titleY);

            // Downward Caret (sort alt down or triangle)
            var caretFont = FontManager.GetFont(7.5f, FontStyle.Regular);
            using var caretBrush = new SolidBrush(Color.FromArgb(160, textClr));
            const string caretGlyph = "▼";
            var caretSize = g.MeasureString(caretGlyph, caretFont);
            float caretX = btnRect.Left + 7;
            float caretY = btnRect.Y + (btnRect.Height - caretSize.Height) / 2f + 1f;
            g.DrawString(caretGlyph, caretFont, caretBrush, caretX, caretY);
        }

        private void DrawClearButton(Graphics g)
        {
            var rect = new RectangleF(_clearBtnRect.X, _clearBtnRect.Y, _clearBtnRect.Width, _clearBtnRect.Height);
            if (_isClearBtnHovered)
            {
                using var hoverPath = CreateRoundedRectanglePath(rect, 6f);
                using var hoverBrush = new SolidBrush(_isDark ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0));
                g.FillPath(hoverBrush, hoverPath);
            }

            var iconFont = FontManager.GetIconFont(10.5f);
            Color clr = _isClearBtnHovered
                ? (_isDark ? Color.White : Color.Black)
                : (_isDark ? Color.FromArgb(160, 160, 160) : Color.FromArgb(120, 120, 120));

            using var brush = new SolidBrush(clr);
            var size = g.MeasureString(KopolArtFaIcons.Cancel, iconFont);
            float x = rect.X + (rect.Width - size.Width) / 2f;
            float y = rect.Y + (rect.Height - size.Height) / 2f;
            g.DrawString(KopolArtFaIcons.Cancel, iconFont, brush, x, y);
        }

        private void DrawSearchButton(Graphics g, Color accentColor)
        {
            var rect = new RectangleF(_searchBtnRect.X, _searchBtnRect.Y, _searchBtnRect.Width, _searchBtnRect.Height);
            using var path = CreateRoundedRectanglePath(rect, 8f);

            Color bg = _isSearchBtnHovered
                ? Color.FromArgb(235, accentColor)
                : accentColor;

            using (var brush = new SolidBrush(bg))
            {
                g.FillPath(brush, path);
            }

            // Search Icon (Contrast White)
            var iconFont = FontManager.GetIconFont(12f);
            using var iconBrush = new SolidBrush(Color.White);

            var size = g.MeasureString(KopolArtFaIcons.Search, iconFont);
            float x = rect.X + (rect.Width - size.Width) / 2f;
            float y = rect.Y + (rect.Height - size.Height) / 2f;
            g.DrawString(KopolArtFaIcons.Search, iconFont, iconBrush, x, y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool oldCatHover = _isCategoryBtnHovered;
            bool oldSearchHover = _isSearchBtnHovered;
            bool oldClearHover = _isClearBtnHovered;

            _isCategoryBtnHovered = _categoryBtnRect.Contains(e.Location);
            _isSearchBtnHovered = _searchBtnRect.Contains(e.Location);
            _isClearBtnHovered = !_clearBtnRect.IsEmpty && _clearBtnRect.Contains(e.Location);

            if (_isCategoryBtnHovered || _isSearchBtnHovered || _isClearBtnHovered)
            {
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
            }

            if (oldCatHover != _isCategoryBtnHovered ||
                oldSearchHover != _isSearchBtnHovered ||
                oldClearHover != _isClearBtnHovered)
            {
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isCategoryBtnHovered = false;
            _isSearchBtnHovered = false;
            _isClearBtnHovered = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left) return;

            if (_categoryBtnRect.Contains(e.Location))
            {
                ShowCategoryMenu();
            }
            else if (_searchBtnRect.Contains(e.Location))
            {
                ExecuteSearch();
            }
            else if (!_clearBtnRect.IsEmpty && _clearBtnRect.Contains(e.Location))
            {
                ClearSearch();
            }
            else
            {
                _txtSearch.Focus();
            }
        }

        private void ShowCategoryMenu()
        {
            var menu = new ContextMenuStrip
            {
                RightToLeft = RightToLeft.Yes,
                Font = FontManager.CreateFont(9.5F, FontStyle.Regular),
                Renderer = _isDark ? new DarkMenuRenderer() : new LightMenuRenderer()
            };

            foreach (var cat in _categories)
            {
                var item = new ToolStripMenuItem
                {
                    Text = cat.Title,
                    Checked = cat.Type == _selectedCategory,
                    CheckOnClick = false
                };

                var currentCat = cat;
                item.Click += (s, e) =>
                {
                    _selectedCategory = currentCat.Type;
                    try
                    {
                        _settingsManager.SetLastSelectedSearchCategory(_selectedCategory.ToString());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to persist search category: {ex.Message}");
                    }

                    UpdateCategoryState();

                    // If text exists, re-fetch suggestions for the new category
                    if (_txtSearch.Text.Trim().Length >= 2)
                    {
                        RestartDebounceTimer();
                    }
                };

                menu.Items.Add(item);
            }

            // Show below category button aligned to its right edge (RTL)
            var screenPoint = PointToScreen(new Point(_categoryBtnRect.Right - menu.PreferredSize.Width, _categoryBtnRect.Bottom + 4));
            menu.Show(screenPoint);
        }

        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            LayoutSearchControls();
            Invalidate();
            RestartDebounceTimer();
        }

        private void OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_suggestionsDropDown?.Visible == true && _suggestionsControl != null &&
                    _suggestionsControl.TryExecuteSelected(out var targetUrl) && !string.IsNullOrWhiteSpace(targetUrl))
                {
                    var query = _txtSearch.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        _settingsManager.AddRecentSearch(query);
                        RecentSearchAdded?.Invoke(this, query);
                    }
                    CloseSuggestions();
                    SearchRequested?.Invoke(this, targetUrl);
                    return;
                }
                ExecuteSearch();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                if (_suggestionsDropDown?.Visible == true)
                {
                    CloseSuggestions();
                }
                else
                {
                    ClearSearch();
                }
            }
            else if (e.KeyCode == Keys.Down && _suggestionsDropDown?.Visible == true)
            {
                e.SuppressKeyPress = true;
                _suggestionsControl?.SelectNextItem();
            }
            else if (e.KeyCode == Keys.Up && _suggestionsDropDown?.Visible == true)
            {
                e.SuppressKeyPress = true;
                _suggestionsControl?.SelectPreviousItem();
            }
        }

        private void RestartDebounceTimer()
        {
            _debounceTimer.Stop();
            var term = _txtSearch.Text.Trim();
            if (term.Length < 2)
            {
                CloseSuggestions();
                return;
            }

            _debounceTimer.Start();
        }

        private void OnDebounceTimerTick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            var term = _txtSearch.Text.Trim();
            if (term.Length < 2)
            {
                CloseSuggestions();
                return;
            }

            FetchSuggestionsAsync(term);
        }

        private async void FetchSuggestionsAsync(string term)
        {
            if (_searchSuggestionService == null) return;

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            EnsureSuggestionsDropDown();
            _suggestionsControl!.SetLoading(term);
            ShowSuggestionsDropDown();

            try
            {
                var items = await _searchSuggestionService.GetSuggestionsAsync(term, _selectedCategory, ct);
                if (ct.IsCancellationRequested) return;

                if (IsDisposed || !IsHandleCreated) return;

                BeginInvoke(new Action(() =>
                {
                    if (ct.IsCancellationRequested) return;

                    // Ensure current text still matches
                    if (!string.Equals(_txtSearch.Text.Trim(), term, StringComparison.OrdinalIgnoreCase))
                        return;

                    _suggestionsControl.SetItems(items, term, CurrentCategory.GetAccent(_isDark));
                    ShowSuggestionsDropDown();
                }));
            }
            catch (OperationCanceledException)
            {
                // Normal debounce cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DashboardSearchBar] Error in FetchSuggestionsAsync: {ex.Message}");
            }
        }

        private void EnsureSuggestionsDropDown()
        {
            if (_suggestionsDropDown != null) return;

            _suggestionsControl = new SearchSuggestionsPopupControl(_isDark);
            _suggestionsControl.SuggestionClicked += (s, url) =>
            {
                var query = _txtSearch.Text.Trim();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    _settingsManager.AddRecentSearch(query);
                    RecentSearchAdded?.Invoke(this, query);
                }
                CloseSuggestions();
                SearchRequested?.Invoke(this, url);
            };
            _suggestionsControl.ViewAllClicked += (s, e) =>
            {
                CloseSuggestions();
                ExecuteSearch();
            };

            _suggestionsHost = new ToolStripControlHost(_suggestionsControl)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            _suggestionsDropDown = new NonActivatingDropDown
            {
                AutoClose = false,
                DropShadowEnabled = true,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = _isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight,
                RightToLeft = RightToLeft.Yes
            };
            _suggestionsDropDown.Items.Add(_suggestionsHost);

            _suggestionsDropDown.Closed += (s, e) =>
            {
                UnregisterClickOutsideFilter();
                Invalidate();
            };
        }

        private void ShowSuggestionsDropDown()
        {
            if (_suggestionsDropDown == null || _suggestionsControl == null || _suggestionsHost == null) return;
            if (IsDisposed || !IsHandleCreated) return;

            int popupW = Math.Max(380, Width);
            int popupH = _suggestionsControl.CalculateHeight();

            _suggestionsControl.Size = new Size(popupW, popupH);
            _suggestionsHost.Size = new Size(popupW, popupH);
            _suggestionsDropDown.Size = new Size(popupW, popupH);

            var pt = PointToScreen(new Point(0, Height + 3));
            if (!_suggestionsDropDown.Visible)
            {
                _suggestionsDropDown.Show(pt);
                RegisterClickOutsideFilter();
            }
            else
            {
                _suggestionsDropDown.Location = pt;
            }

            if (!_txtSearch.Focused && _txtSearch.CanFocus)
            {
                _txtSearch.Focus();
                _txtSearch.SelectionStart = _txtSearch.Text.Length;
            }
        }

        public void CloseSuggestions()
        {
            UnregisterClickOutsideFilter();
            if (_suggestionsDropDown != null && _suggestionsDropDown.Visible)
            {
                _suggestionsDropDown.Close();
            }
        }

        private ClickOutsideFilter? _clickOutsideFilter;
        private bool _isFilterRegistered;

        private void RegisterClickOutsideFilter()
        {
            if (_isFilterRegistered) return;
            try
            {
                _clickOutsideFilter ??= new ClickOutsideFilter(this);
                System.Windows.Forms.Application.AddMessageFilter(_clickOutsideFilter);
                _isFilterRegistered = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DashboardSearchBar] Failed to add message filter: {ex.Message}");
            }
        }

        private void UnregisterClickOutsideFilter()
        {
            if (!_isFilterRegistered || _clickOutsideFilter == null) return;
            try
            {
                System.Windows.Forms.Application.RemoveMessageFilter(_clickOutsideFilter);
                _isFilterRegistered = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DashboardSearchBar] Failed to remove message filter: {ex.Message}");
            }
        }

        public bool ContainsPointInSearchOrPopup(Point screenPt)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return false;

                if (RectangleToScreen(ClientRectangle).Contains(screenPt))
                    return true;

                if (_suggestionsDropDown != null && _suggestionsDropDown.Visible &&
                    _suggestionsDropDown.Bounds.Contains(screenPt))
                    return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardSearchBar] Failed to check suggestion drop-down bounds: {ex.Message}");
            }
            return false;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            var parentForm = FindForm();
            if (parentForm != null)
            {
                parentForm.Deactivate -= OnParentFormDeactivate;
                parentForm.Deactivate += OnParentFormDeactivate;
                parentForm.Move -= OnParentFormMove;
                parentForm.Move += OnParentFormMove;
            }
        }

        private void OnParentFormDeactivate(object? sender, EventArgs e)
        {
            CloseSuggestions();
        }

        private void OnParentFormMove(object? sender, EventArgs e)
        {
            if (_suggestionsDropDown?.Visible == true)
            {
                _suggestionsDropDown.Location = PointToScreen(new Point(0, Height + 3));
            }
        }

        public void ExecuteSearch()
        {
            CloseSuggestions();
            var query = _txtSearch.Text.Trim();
            var cat = CurrentCategory;

            string targetUrl;
            if (!string.IsNullOrWhiteSpace(query))
            {
                _settingsManager.AddRecentSearch(query);
                RecentSearchAdded?.Invoke(this, query);
                targetUrl = $"{cat.SearchUrl}?query={Uri.EscapeDataString(query)}";
            }
            else
            {
                targetUrl = cat.SearchUrl;
            }

            SearchRequested?.Invoke(this, targetUrl);
        }

        public void SetSearchQuery(string query, bool execute = true)
        {
            _txtSearch.Text = query ?? string.Empty;
            _txtSearch.SelectionStart = _txtSearch.Text.Length;
            LayoutSearchControls();
            Invalidate();
            if (execute && !string.IsNullOrWhiteSpace(query))
            {
                ExecuteSearch();
            }
        }

        public void ClearSearch()
        {
            _txtSearch.Text = string.Empty;
            CloseSuggestions();
            _txtSearch.Focus();
            LayoutSearchControls();
            Invalidate();
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterClickOutsideFilter();
                var parentForm = FindForm();
                if (parentForm != null)
                {
                    parentForm.Deactivate -= OnParentFormDeactivate;
                    parentForm.Move -= OnParentFormMove;
                }
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
                _searchCts?.Cancel();
                _searchCts?.Dispose();
                _suggestionsDropDown?.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class ClickOutsideFilter : IMessageFilter
        {
            private readonly DashboardSearchBar _owner;

            public ClickOutsideFilter(DashboardSearchBar owner)
            {
                _owner = owner;
            }

            public bool PreFilterMessage(ref Message m)
            {
                // WM_LBUTTONDOWN (0x0201), WM_RBUTTONDOWN (0x0204), WM_NCLBUTTONDOWN (0x00A1), WM_NCRBUTTONDOWN (0x00A4)
                if (m.Msg == 0x0201 || m.Msg == 0x0204 || m.Msg == 0x00A1 || m.Msg == 0x00A4)
                {
                    if (_owner._suggestionsDropDown?.Visible == true)
                    {
                        var cursorPt = Cursor.Position;
                        if (!_owner.ContainsPointInSearchOrPopup(cursorPt))
                        {
                            _owner.BeginInvoke(new Action(() => _owner.CloseSuggestions()));
                        }
                    }
                }
                return false;
            }
        }

        private sealed class NonActivatingDropDown : ToolStripDropDown
        {
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WM_MOUSEACTIVATE = 0x0021;
            private const int MA_NOACTIVATE = 3;

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_NOACTIVATE;
                    return cp;
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_MOUSEACTIVATE)
                {
                    m.Result = (IntPtr)MA_NOACTIVATE;
                    return;
                }
                base.WndProc(ref m);
            }
        }

        #region Suggestions Popup Sub-Control

        private sealed class SearchSuggestionsPopupControl : Control
        {
            private bool _isDark;
            private bool _isLoading;
            private string _currentTerm = string.Empty;
            private Color _accentColor;
            private readonly List<SearchSuggestionItem> _items = new();
            private int _hoveredIndex = -1;

            private const int ItemHeight = 56;
            private const int MaxVisibleItems = 5;
            private const int FooterHeight = 44;
            private bool _isFooterHovered;

            public event EventHandler<string>? SuggestionClicked;
            public event EventHandler? ViewAllClicked;

            public SearchSuggestionsPopupControl(bool isDark)
            {
                _isDark = isDark;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);

                DoubleBuffered = true;
                RightToLeft = RightToLeft.Yes;
                Cursor = Cursors.Hand;
            }

            public void ApplyTheme(bool isDark)
            {
                _isDark = isDark;
                BackColor = isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight;
                Invalidate();
            }

            public void SetLoading(string term)
            {
                _isLoading = true;
                _currentTerm = term;
                _items.Clear();
                _hoveredIndex = -1;
                _isFooterHovered = false;
                Invalidate();
            }

            public void SetItems(List<SearchSuggestionItem> items, string term, Color accentColor)
            {
                _isLoading = false;
                _currentTerm = term;
                _accentColor = accentColor;
                _items.Clear();
                if (items != null)
                {
                    _items.AddRange(items);
                }
                _hoveredIndex = -1;
                _isFooterHovered = false;
                Invalidate();
            }

            public int CalculateHeight()
            {
                if (_isLoading)
                {
                    return 54;
                }

                if (_items.Count == 0)
                {
                    return 54;
                }

                int count = Math.Min(MaxVisibleItems, _items.Count);
                return (count * ItemHeight) + FooterHeight;
            }

            public void SelectNextItem()
            {
                if (_isLoading || _items.Count == 0) return;
                int visibleCount = Math.Min(MaxVisibleItems, _items.Count);
                _isFooterHovered = false;
                _hoveredIndex++;
                if (_hoveredIndex >= visibleCount) _hoveredIndex = 0;
                Invalidate();
            }

            public void SelectPreviousItem()
            {
                if (_isLoading || _items.Count == 0) return;
                int visibleCount = Math.Min(MaxVisibleItems, _items.Count);
                _isFooterHovered = false;
                _hoveredIndex--;
                if (_hoveredIndex < 0) _hoveredIndex = visibleCount - 1;
                Invalidate();
            }

            public bool TryExecuteSelected(out string? targetUrl)
            {
                targetUrl = null;
                if (_isLoading || _items.Count == 0) return false;
                int visibleCount = Math.Min(MaxVisibleItems, _items.Count);
                if (_hoveredIndex >= 0 && _hoveredIndex < visibleCount)
                {
                    targetUrl = _items[_hoveredIndex].FullTargetUrl;
                    return !string.IsNullOrWhiteSpace(targetUrl);
                }
                return false;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (_isLoading || _items.Count == 0) return;

                int visibleCount = Math.Min(MaxVisibleItems, _items.Count);
                int itemsBottom = visibleCount * ItemHeight;

                bool oldFooterHover = _isFooterHovered;
                int oldHoveredIndex = _hoveredIndex;

                if (e.Y >= itemsBottom && e.Y < itemsBottom + FooterHeight)
                {
                    _isFooterHovered = true;
                    _hoveredIndex = -1;
                }
                else
                {
                    _isFooterHovered = false;
                    int newIndex = e.Y / ItemHeight;
                    if (newIndex >= visibleCount) newIndex = -1;
                    _hoveredIndex = newIndex;
                }

                if (oldFooterHover != _isFooterHovered || oldHoveredIndex != _hoveredIndex)
                {
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hoveredIndex = -1;
                _isFooterHovered = false;
                Invalidate();
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left || _isLoading || _items.Count == 0) return;

                int visibleCount = Math.Min(MaxVisibleItems, _items.Count);
                int itemsBottom = visibleCount * ItemHeight;

                if (e.Y >= itemsBottom && e.Y < itemsBottom + FooterHeight)
                {
                    ViewAllClicked?.Invoke(this, EventArgs.Empty);
                    return;
                }

                int index = e.Y / ItemHeight;
                if (index >= 0 && index < visibleCount)
                {
                    var item = _items[index];
                    if (!string.IsNullOrWhiteSpace(item.FullTargetUrl))
                    {
                        SuggestionClicked?.Invoke(this, item.FullTargetUrl);
                    }
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                bool highContrast = SystemAccessibility.IsHighContrast;
                Color cardBg = highContrast ? SystemColors.Window : _isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight;
                Color borderClr = highContrast ? SystemColors.ControlDark : _isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight;
                Color textPrimary = highContrast ? SystemColors.WindowText : _isDark ? UIConstants.DarkMode.MenuStripForeColor : UIConstants.TextThemeColor;
                Color textSecondary = highContrast ? SystemColors.WindowText : _isDark ? Color.FromArgb(160, 160, 160) : Color.FromArgb(120, 120, 120);

                // Background & Border
                using (var bgBrush = new SolidBrush(cardBg))
                {
                    g.FillRectangle(bgBrush, ClientRectangle);
                }
                using (var pen = new Pen(borderClr, 1f))
                {
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }

                if (_isLoading)
                {
                    var loadingFont = FontManager.GetFont(9.5f, FontStyle.Regular);
                    using var loadingBrush = new SolidBrush(textSecondary);
                    string loadingText = TextManager.GetString("SearchLoading");
                    var sz = g.MeasureString(loadingText, loadingFont);
                    g.DrawString(loadingText, loadingFont, loadingBrush, (Width - sz.Width) / 2f, (Height - sz.Height) / 2f);
                    return;
                }

                if (_items.Count == 0)
                {
                    var emptyFont = FontManager.GetFont(9.5f, FontStyle.Regular);
                    using var emptyBrush = new SolidBrush(textSecondary);
                    string emptyText = TextManager.GetString("SearchNoResults");
                    var sz = g.MeasureString(emptyText, emptyFont);
                    g.DrawString(emptyText, emptyFont, emptyBrush, (Width - sz.Width) / 2f, (Height - sz.Height) / 2f);
                    return;
                }

                int visibleCount = Math.Min(MaxVisibleItems, _items.Count);
                for (int i = 0; i < visibleCount; i++)
                {
                    var item = _items[i];
                    int itemY = i * ItemHeight;
                    var itemRect = new Rectangle(0, itemY, Width, ItemHeight);

                    // Hover background
                    bool rowHovered = i == _hoveredIndex;
                    Color rowPrimary = highContrast && rowHovered ? SystemColors.HighlightText : textPrimary;
                    Color rowSecondary = highContrast && rowHovered ? SystemColors.HighlightText : textSecondary;

                    if (rowHovered)
                    {
                        Color hoverBg = highContrast
                            ? SystemColors.Highlight
                            : _isDark
                                ? UIConstants.DarkMode.CardBackgroundHoverDark
                                : UIConstants.CardBackgroundHoverLight;

                        using var hoverBrush = new SolidBrush(hoverBg);
                        g.FillRectangle(hoverBrush, itemRect);
                    }

                    // Divider between rows
                    if (i > 0)
                    {
                        using var divPen = new Pen(highContrast
                            ? SystemColors.ControlDark
                            : _isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(25, 0, 0, 0));
                        g.DrawLine(divPen, 12, itemY, Width - 12, itemY);
                    }

                    // Thumbnail / Icon (Right side in RTL)
                    int thumbSize = 40;
                    int thumbX = Width - 12 - thumbSize;
                    int thumbY = itemY + (ItemHeight - thumbSize) / 2;
                    var thumbRect = new Rectangle(thumbX, thumbY, thumbSize, thumbSize);

                    DrawSuggestionThumbnail(g, item, thumbRect);

                    // Chevron (Left side in RTL)
                    var chevronFont = FontManager.GetIconFont(9.5f);
                    using (var chevronBrush = new SolidBrush(rowSecondary))
                    {
                        var chevSz = g.MeasureString(KopolArtFaIcons.AngleLeft, chevronFont);
                        g.DrawString(KopolArtFaIcons.AngleLeft, chevronFont, chevronBrush, 14, itemY + (ItemHeight - chevSz.Height) / 2f);
                    }

                    // Text (Between Left Chevron and Right Thumbnail)
                    int textRight = thumbX - 10;
                    int textLeft = 36;
                    int textWidth = Math.Max(20, textRight - textLeft);

                    var titleFont = FontManager.GetFont(9.5f, FontStyle.Bold);

                    if (item.HasValueText)
                    {
                        // 2 lines: Title and Subtitle
                        var titleRect = new Rectangle(textLeft, itemY + 8, textWidth, 20);
                        var subRect = new Rectangle(textLeft, itemY + 28, textWidth, 18);

                        TextRenderer.DrawText(g, item.Label, titleFont, titleRect, rowPrimary,
                            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

                        var subFont = FontManager.GetFont(8.5f, FontStyle.Regular);
                        TextRenderer.DrawText(g, item.Value, subFont, subRect, rowSecondary,
                            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    }
                    else
                    {
                        // 1 centered line: Title
                        var titleRect = new Rectangle(textLeft, itemY, textWidth, ItemHeight);
                        TextRenderer.DrawText(g, item.Label, titleFont, titleRect, rowPrimary,
                            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    }
                }

                // --- Footer: View all results ("نمایش تمام نتایج در سایت") ---
                int footerY = visibleCount * ItemHeight;
                var footerRect = new Rectangle(0, footerY, Width, FooterHeight);

                // Divider line above footer
                using (var divPen = new Pen(_isDark ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0), 1f))
                {
                    g.DrawLine(divPen, 0, footerY, Width, footerY);
                }

                // Footer background (subtle tint or hover highlight)
                Color footerBg = _isFooterHovered
                    ? (_isDark ? Color.FromArgb(50, _accentColor) : Color.FromArgb(30, _accentColor))
                    : (_isDark ? Color.FromArgb(20, _accentColor) : Color.FromArgb(12, _accentColor));

                using (var footerBrush = new SolidBrush(footerBg))
                {
                    g.FillRectangle(footerBrush, footerRect.X, footerRect.Y + 1, footerRect.Width, footerRect.Height - 1);
                }

                // Footer content: Search icon + Text ("نمایش تمام نتایج در سایت")
                Color footerTextColor = _accentColor.IsEmpty
                    ? (_isDark ? UIConstants.BrandPrimaryDark : UIConstants.BrandPrimary)
                    : _accentColor;

                var footerFont = FontManager.GetFont(9.5f, FontStyle.Bold);
                var footerIconFont = FontManager.GetIconFont(11f);

                string footerText = TextManager.GetString("SearchViewAllResults");
                var textSz = g.MeasureString(footerText, footerFont);
                var iconSz = g.MeasureString(KopolArtFaIcons.Search, footerIconFont);

                float spacing = 8f;
                float totalW = iconSz.Width + spacing + textSz.Width;
                float startX = (Width - totalW) / 2f;

                using (var textBrush = new SolidBrush(footerTextColor))
                {
                    // In RTL: Draw Icon on right, Text on left
                    float iconDrawX = startX + textSz.Width + spacing;
                    float iconDrawY = footerY + (FooterHeight - iconSz.Height) / 2f;
                    g.DrawString(KopolArtFaIcons.Search, footerIconFont, textBrush, iconDrawX, iconDrawY);

                    float textDrawX = startX;
                    float textDrawY = footerY + (FooterHeight - textSz.Height) / 2f;
                    g.DrawString(footerText, footerFont, textBrush, textDrawX, textDrawY);
                }
            }

            private void DrawSuggestionThumbnail(Graphics g, SearchSuggestionItem item, Rectangle rect)
            {
                using var thumbPath = CreateRoundedRectanglePath(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), 6f);

                // Fallback / Placeholder background
                Color fallbackBg = _isDark ? Color.FromArgb(45, 45, 48) : Color.FromArgb(235, 230, 222);
                using (var fbBrush = new SolidBrush(fallbackBg))
                {
                    g.FillPath(fbBrush, thumbPath);
                }

                bool drawnImage = false;
                if (item.HasImage && !string.IsNullOrWhiteSpace(item.FullImageUrl))
                {
                    if (ThumbnailCache.TryGetValue(item.FullImageUrl, out var cachedImg))
                    {
                        var oldClip = g.Clip;
                        g.SetClip(thumbPath);
                        g.DrawImage(cachedImg, rect);
                        g.Clip = oldClip;
                        drawnImage = true;
                    }
                    else
                    {
                        // Trigger async background download
                        QueueImageDownload(item.FullImageUrl);
                    }
                }

                if (!drawnImage)
                {
                    // Draw Search Icon fallback inside thumbnail
                    var iconFont = FontManager.GetIconFont(11f);
                    Color iconClr = _accentColor.IsEmpty ? (_isDark ? Color.LightGray : Color.Gray) : _accentColor;
                    using var iconBrush = new SolidBrush(iconClr);
                    var sz = g.MeasureString(KopolArtFaIcons.Search, iconFont);
                    g.DrawString(KopolArtFaIcons.Search, iconFont, iconBrush,
                        rect.X + (rect.Width - sz.Width) / 2f,
                        rect.Y + (rect.Height - sz.Height) / 2f);
                }

                // Subtle border around thumbnail
                using var borderPen = new Pen(_isDark ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0), 1f);
                g.DrawPath(borderPen, thumbPath);
            }

            private void QueueImageDownload(string url)
            {
                if (string.IsNullOrWhiteSpace(url) || ThumbnailCache.ContainsKey(url)) return;

                Task.Run(async () =>
                {
                    try
                    {
                        using var res = await ImageHttpClient.GetAsync(url).ConfigureAwait(false);
                        if (!res.IsSuccessStatusCode) return;

                        var bytes = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        using var ms = new MemoryStream(bytes);
                        using var srcImg = Image.FromStream(ms);

                        // Scale to 80x80 for crisp retina / HiDPI rendering
                        var scaled = new Bitmap(80, 80);
                        using (var sg = Graphics.FromImage(scaled))
                        {
                            sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            sg.SmoothingMode = SmoothingMode.HighQuality;
                            sg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            sg.DrawImage(srcImg, 0, 0, 80, 80);
                        }

                        if (ThumbnailCache.Count > 100)
                        {
                            ThumbnailCache.Clear();
                        }

                        ThumbnailCache[url] = scaled;

                        if (!IsDisposed && IsHandleCreated)
                        {
                            BeginInvoke(new Action(Invalidate));
                        }
                    }
                    catch
                    {
                        // Ignore thumbnail download errors safely
                    }
                });
            }
        }

        #endregion
    }
}
