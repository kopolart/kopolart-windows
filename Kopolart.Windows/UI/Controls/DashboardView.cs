using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Services;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Kopolart.Windows.Domain.Models;
using System.Threading.Tasks;
using Kopolart.Windows.Domain.Constants;

namespace Kopolart.Windows.UI.Controls
{
    /// <summary>
    /// Modern, luxury Dashboard control consistent with the MAUI application experience.
    /// Provides quick access to Web browsing, personal website, live usage metrics,
    /// settings, history, downloads, bookmarks, keyboard shortcuts, contact info, and app logs.
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private readonly ISettingsManager _settingsManager;
        private readonly UsageTrackerService _usageTracker;
        private readonly IAssetService _assetService;
        private readonly IUpdateService _updateService;
        private readonly IOnlineUsersService? _onlineUsersService;
        private readonly IRssFeedService? _rssFeedService;
        private readonly ISearchSuggestionService? _searchSuggestionService;

        private bool _isDark;

        // Container panels
        private Panel pnlScrollContainer = null!;
        private Panel pnlContent = null!;

        // Header controls
        private Panel pnlHeader = null!;
        private PictureBox pbLogo = null!;

        // Hero Card
        private DashboardActionCard cardStartBrowsing = null!;

        // Categories Quick Hub (One Row)
        private Panel pnlCategoriesHub = null!;
        private readonly List<DashboardCategoryChip> _categoryChips = new();

        // Instant Search Bar
        private DashboardSearchBar searchBar = null!;

        // Recent Search Chips
        private Panel pnlRecentSearches = null!;
        private Label lblRecentIcon = null!;
        private Label lblRecentText = null!;
        private readonly List<DashboardRecentSearchChip> _recentSearchChips = new();

        // Featured Card
        private DashboardActionCard cardPersonalWebsite = null!;

        // Metrics Pill Card
        private DashboardMetricsCard cardMetrics = null!;

        // User Account Card
        private DashboardUserCard cardUser = null!;

        // Action Cards Container
        private Panel pnlCardsContainer = null!;
        private DashboardActionCard cardSettings = null!;
        private DashboardActionCard cardUsage = null!;
        private DashboardActionCard cardHistory = null!;
        private DashboardActionCard cardDownloads = null!;
        private DashboardActionCard cardBookmarks = null!;
        private DashboardActionCard cardShortcuts = null!;
        private DashboardActionCard cardContact = null!;
        private DashboardActionCard cardAbout = null!;

        // Events
        public event EventHandler? StartBrowsingRequested;
        public event EventHandler<string>? SearchRequested;
        public event EventHandler<string>? CategoryRequested;
        public event EventHandler? PersonalWebsiteRequested;
        public event EventHandler? UserProfileRequested;
        public event EventHandler? LoginRequested;
        public event EventHandler? RegisterRequested;
        public event EventHandler? SettingsRequested;
        public event EventHandler? UsageRequested;
        public event EventHandler? HistoryRequested;
        public event EventHandler? DownloadsRequested;
        public event EventHandler? BookmarksRequested;
        public event EventHandler? ShortcutsRequested;
        public event EventHandler? ContactRequested;
        public event EventHandler? AboutRequested;

        public DashboardView(
            ISettingsManager settingsManager,
            UsageTrackerService usageTracker,
            IAssetService assetService,
            IUpdateService updateService,
            bool isDark,
            IOnlineUsersService? onlineUsersService = null,
            IRssFeedService? rssFeedService = null,
            ISearchSuggestionService? searchSuggestionService = null)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _isDark = isDark;
            _onlineUsersService = onlineUsersService;
            _rssFeedService = rssFeedService;
            _searchSuggestionService = searchSuggestionService;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;
            InitializeComponent();
            ApplyTheme(_isDark);
            ApplyTexts();
            RefreshMetrics();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            Font = FontManager.CreateFont(9.5F, FontStyle.Regular);

            // Scrollable host panel (always LTR coordinates for reliable centering across all Windows versions)
            pnlScrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                RightToLeft = RightToLeft.No
            };
            Controls.Add(pnlScrollContainer);

            // Centered content panel
            pnlContent = new Panel
            {
                Width = 960,
                Height = 840,
                AutoSize = false,
                Location = new Point(0, 0),
                Padding = new Padding(24, 20, 24, 24),
                RightToLeft = RightToLeft.Yes
            };
            pnlScrollContainer.Controls.Add(pnlContent);

            pnlScrollContainer.Resize += (s, e) => CenterContentPanel();

            int currentY = 25;
            int sectionGap = 17; // +3 margin for better spacing (was 14)

            // 1. Header (Centered Enlarged Logo - 144x144, 2% bigger)
            pnlHeader = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 154),
                BackColor = Color.Transparent
            };

            pbLogo = new PictureBox
            {
                Size = new Size(144, 144), // 2% bigger (was 141x141)
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try
            {
                var logoImg = _assetService.LoadLogo(_isDark);
                if (logoImg != null)
                {
                    pbLogo.Image = logoImg;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load logo in DashboardView: {ex.Message}");
            }

            pbLogo.Location = new Point((pnlHeader.Width - pbLogo.Width) / 2, (pnlHeader.Height - pbLogo.Height) / 2);
            pnlHeader.Controls.Add(pbLogo);
            pnlContent.Controls.Add(pnlHeader);

            currentY += 154 + sectionGap;

            // 2. HERO BUTTON: Start Browsing (height 96, was 90)
            cardStartBrowsing = new DashboardActionCard
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 96),
                Title = TextManager.GetString("DashboardStartBrowsing"),
                Subtitle = TextManager.GetString("DashboardStartBrowsingDesc"),
                BadgeText = TextManager.GetString("DashboardStartBrowsingBadge"),
                BadgeColor = UIConstants.ThemeColor,
                BadgeTextColor = UIConstants.TextThemeColor,
                IconGlyph = KopolArtFaIcons.Globe,
                IsHero = true
            };
            cardStartBrowsing.CardClicked += (s, e) => StartBrowsingRequested?.Invoke(this, EventArgs.Empty);
            pnlContent.Controls.Add(cardStartBrowsing);

            currentY += 96 + sectionGap;

            // 2.5 CATEGORIES QUICK HUB (5 items in ONE single row)
            pnlCategoriesHub = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 54),
                BackColor = Color.Transparent,
                RightToLeft = RightToLeft.Yes
            };
            CreateCategoryChips();
            LayoutCategoriesHub();
            pnlContent.Controls.Add(pnlCategoriesHub);

            currentY += 54 + sectionGap;

            // 2.7 INSTANT SEARCH BAR (height 52)
            searchBar = new DashboardSearchBar(_settingsManager, _searchSuggestionService, _isDark)
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 52),
                RightToLeft = RightToLeft.Yes
            };
            searchBar.SearchRequested += (s, url) => SearchRequested?.Invoke(this, url);
            searchBar.RecentSearchAdded += (s, q) =>
            {
                RefreshRecentSearches();
                CenterContentPanel();
            };
            pnlContent.Controls.Add(searchBar);

            currentY += 52 + 6;

            // 2.8 RECENT SEARCH CHIPS (height 28)
            pnlRecentSearches = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 28),
                BackColor = _isDark ? UIConstants.DarkMode.BrandBackground : UIConstants.BrandBackground,
                RightToLeft = RightToLeft.Yes,
                Visible = false
            };

            lblRecentIcon = new Label
            {
                Text = KopolArtFaIcons.Clock,
                Font = FontManager.GetIconFont(10.5f),
                ForeColor = _isDark ? Color.FromArgb(189, 181, 170) : Color.FromArgb(105, 95, 88),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(18, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            };
            pnlRecentSearches.Controls.Add(lblRecentIcon);

            var textFont = FontManager.GetFont(9f, FontStyle.Bold);
            string headerText = TextManager.GetString("SearchRecentQueries");
            var headerSize = TextRenderer.MeasureText(headerText, textFont);

            lblRecentText = new Label
            {
                Text = headerText,
                Font = textFont,
                ForeColor = _isDark ? Color.FromArgb(189, 181, 170) : Color.FromArgb(105, 95, 88),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(headerSize.Width + 4, 28),
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            };
            pnlRecentSearches.Controls.Add(lblRecentText);
            pnlContent.Controls.Add(pnlRecentSearches);

            CreateRecentSearchChips();
            if (pnlRecentSearches.Visible)
            {
                currentY += 28 + sectionGap;
            }
            else
            {
                currentY += sectionGap - 6;
            }

            // 3. FEATURED CARD: Personal Website (height 88, was 84)
            cardPersonalWebsite = new DashboardActionCard
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 88),
                Title = TextManager.GetString("DashboardPersonalWebsite"),
                Subtitle = TextManager.GetString("DashboardPersonalWebsiteDesc"),
                BadgeText = TextManager.GetString("DashboardPersonalWebsiteBadge"),
                IconGlyph = KopolArtFaIcons.User,
                BadgeColor = UIConstants.BrandSlateBlue
            };
            cardPersonalWebsite.CardClicked += (s, e) => PersonalWebsiteRequested?.Invoke(this, EventArgs.Empty);
            pnlContent.Controls.Add(cardPersonalWebsite);

            currentY += 88 + sectionGap;

            // 4. Quick Metrics Summary Pill (height 90, was 78)
            cardMetrics = new DashboardMetricsCard
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 90)
            };
            pnlContent.Controls.Add(cardMetrics);

            currentY += 90 + sectionGap;

            // 4.5 User Account Card (height 88, was 68)
            cardUser = new DashboardUserCard
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 88)
            };
            cardUser.UserProfileRequested += (s, e) => UserProfileRequested?.Invoke(this, EventArgs.Empty);
            cardUser.LoginRequested += (s, e) => LoginRequested?.Invoke(this, EventArgs.Empty);
            cardUser.RegisterRequested += (s, e) => RegisterRequested?.Invoke(this, EventArgs.Empty);
            pnlContent.Controls.Add(cardUser);

            currentY += 88 + sectionGap;

            // 5. Action Cards Grid (3 Rows: Row 1 has 3, Row 2 has 3, Row 3 has 2 in center - height 298, was 286)
            pnlCardsContainer = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(912, 298),
                BackColor = Color.Transparent
            };

            cardSettings = CreateActionCard(
                TextManager.GetString("DashboardCardSettings"),
                TextManager.GetString("DashboardCardSettingsDesc"),
                KopolArtFaIcons.Setting,
                () => SettingsRequested?.Invoke(this, EventArgs.Empty));

            cardUsage = CreateActionCard(
                TextManager.GetString("DashboardCardUsage"),
                TextManager.GetString("DashboardCardUsageDesc"),
                KopolArtFaIcons.ChartPie,
                () => UsageRequested?.Invoke(this, EventArgs.Empty));

            cardHistory = CreateActionCard(
                TextManager.GetString("DashboardCardHistory"),
                TextManager.GetString("DashboardCardHistoryDesc"),
                KopolArtFaIcons.DocTextInv,
                () => HistoryRequested?.Invoke(this, EventArgs.Empty));

            cardDownloads = CreateActionCard(
                TextManager.GetString("DashboardCardDownloads"),
                TextManager.GetString("DashboardCardDownloadsDesc"),
                KopolArtFaIcons.Download,
                () => DownloadsRequested?.Invoke(this, EventArgs.Empty));

            cardBookmarks = CreateActionCard(
                TextManager.GetString("DashboardCardBookmarks"),
                TextManager.GetString("DashboardCardBookmarksDesc"),
                KopolArtFaIcons.Bookmark,
                () => BookmarksRequested?.Invoke(this, EventArgs.Empty));

            cardShortcuts = CreateActionCard(
                TextManager.GetString("DashboardCardShortcuts"),
                TextManager.GetString("DashboardCardShortcutsDesc"),
                KopolArtFaIcons.Slack,
                () => ShortcutsRequested?.Invoke(this, EventArgs.Empty));

            cardContact = CreateActionCard(
                TextManager.GetString("DashboardCardContact"),
                TextManager.GetString("DashboardCardContactDesc"),
                KopolArtFaIcons.Phone,
                () => ContactRequested?.Invoke(this, EventArgs.Empty));

            cardAbout = CreateActionCard(
                TextManager.GetString("DashboardCardAbout"),
                TextManager.GetString("DashboardCardAboutDesc"),
                KopolArtFaIcons.Info,
                () => AboutRequested?.Invoke(this, EventArgs.Empty));

            pnlCardsContainer.Controls.Add(cardSettings);
            pnlCardsContainer.Controls.Add(cardUsage);
            pnlCardsContainer.Controls.Add(cardHistory);
            pnlCardsContainer.Controls.Add(cardDownloads);
            pnlCardsContainer.Controls.Add(cardBookmarks);
            pnlCardsContainer.Controls.Add(cardShortcuts);
            pnlCardsContainer.Controls.Add(cardContact);
            pnlCardsContainer.Controls.Add(cardAbout);

            LayoutActionCards();
            pnlContent.Controls.Add(pnlCardsContainer);

            currentY += 298 + sectionGap;

            ResumeLayout(false);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CenterContentPanel();
        }

        private void LayoutActionCards()
        {
            if (pnlCardsContainer == null || cardSettings == null) return;

            bool isRtl = RightToLeft == RightToLeft.Yes;
            int containerW = pnlCardsContainer.ClientSize.Width;
            if (containerW <= 0) containerW = pnlCardsContainer.Width;
            if (containerW <= 0) containerW = 912;

            int gapX = 21; // +3 margin for better spacing (was 18)
            int gapY = 17; // +3 margin for better spacing (was 14)
            int cardH = 88; // increased height for black font clearance & luxury spacing (was 84)
            int minCardW = 240;

            var cards = new[]
            {
                cardSettings, cardUsage, cardHistory,
                cardDownloads, cardBookmarks, cardShortcuts,
                cardContact, cardAbout
            };

            int columns = Math.Clamp((containerW + gapX) / (minCardW + gapX), 1, 3);
            int cardW = columns == 1
                ? containerW
                : Math.Max(minCardW, (containerW - (gapX * (columns - 1))) / columns);
            int rows = (cards.Length + columns - 1) / columns;

            for (int row = 0; row < rows; row++)
            {
                int first = row * columns;
                int count = Math.Min(columns, cards.Length - first);
                int rowWidth = (count * cardW) + ((count - 1) * gapX);
                int startX = Math.Max(0, (containerW - rowWidth) / 2);
                int y = row * (cardH + gapY);

                for (int col = 0; col < count; col++)
                {
                    var card = cards[first + col];
                    int logicalX = startX + (col * (cardW + gapX));

                    card.Size = new Size(cardW, cardH);
                    card.Location = new Point(isRtl ? containerW - logicalX - cardW : logicalX, y);
                }
            }

            pnlCardsContainer.Height = (rows * cardH) + ((rows - 1) * gapY);
        }

        private void CreateCategoryChips()
        {
            if (pnlCategoriesHub == null) return;
            pnlCategoriesHub.Controls.Clear();
            _categoryChips.Clear();

            var categories = new[]
            {
                new {
                    Title = "رنگ‌ها",
                    Url = AppConstants.Urls.Colors,
                    CategoryKey = "colors",
                    Glyph = KopolArtFaIcons.Sun,
                    AccentLight = Color.FromArgb(0xD9, 0x77, 0x06),
                    AccentDark = Color.FromArgb(0xFB, 0xBF, 0x24),
                    BadgeBgLight = Color.FromArgb(0x1A, 0xD9, 0x77, 0x06),
                    BadgeBgDark = Color.FromArgb(0x30, 0xFB, 0xBF, 0x24),
                    BorderLight = Color.FromArgb(0x33, 0xD9, 0x77, 0x06),
                    BorderDark = Color.FromArgb(0x55, 0xFB, 0xBF, 0x24)
                },
                new {
                    Title = "پالت‌ها",
                    Url = AppConstants.Urls.ColorPalettes,
                    CategoryKey = "color-palettes",
                    Glyph = KopolArtFaIcons.Palette,
                    AccentLight = Color.FromArgb(0x0D, 0x94, 0x88),
                    AccentDark = Color.FromArgb(0x2D, 0xD4, 0xBF),
                    BadgeBgLight = Color.FromArgb(0x1A, 0x0D, 0x94, 0x88),
                    BadgeBgDark = Color.FromArgb(0x30, 0x2D, 0xD4, 0xBF),
                    BorderLight = Color.FromArgb(0x33, 0x0D, 0x94, 0x88),
                    BorderDark = Color.FromArgb(0x55, 0x2D, 0xD4, 0xBF)
                },
                new {
                    Title = "نگاره‌ها",
                    Url = AppConstants.Urls.Posts,
                    CategoryKey = "posts",
                    Glyph = KopolArtFaIcons.Newspaper,
                    AccentLight = Color.FromArgb(0x24, 0x9C, 0xF0),
                    AccentDark = Color.FromArgb(0x60, 0xA5, 0xFA),
                    BadgeBgLight = Color.FromArgb(0x1A, 0x24, 0x9C, 0xF0),
                    BadgeBgDark = Color.FromArgb(0x30, 0x60, 0xA5, 0xFA),
                    BorderLight = Color.FromArgb(0x33, 0x24, 0x9C, 0xF0),
                    BorderDark = Color.FromArgb(0x55, 0x60, 0xA5, 0xFA)
                },
                new {
                    Title = "والپیپرها",
                    Url = AppConstants.Urls.Wallpapers,
                    CategoryKey = "wallpapers",
                    Glyph = KopolArtFaIcons.Image,
                    AccentLight = Color.FromArgb(0x93, 0x33, 0xEA),
                    AccentDark = Color.FromArgb(0xC0, 0x84, 0xFC),
                    BadgeBgLight = Color.FromArgb(0x1A, 0x93, 0x33, 0xEA),
                    BadgeBgDark = Color.FromArgb(0x30, 0xC0, 0x84, 0xFC),
                    BorderLight = Color.FromArgb(0x33, 0x93, 0x33, 0xEA),
                    BorderDark = Color.FromArgb(0x55, 0xC0, 0x84, 0xFC)
                },
                new {
                    Title = "ابزارها",
                    Url = AppConstants.Urls.Tools,
                    CategoryKey = "tools",
                    Glyph = KopolArtFaIcons.Setting,
                    AccentLight = Color.FromArgb(0x05, 0x96, 0x69),
                    AccentDark = Color.FromArgb(0x34, 0xD3, 0x99),
                    BadgeBgLight = Color.FromArgb(0x1A, 0x05, 0x96, 0x69),
                    BadgeBgDark = Color.FromArgb(0x30, 0x34, 0xD3, 0x99),
                    BorderLight = Color.FromArgb(0x33, 0x05, 0x96, 0x69),
                    BorderDark = Color.FromArgb(0x55, 0x34, 0xD3, 0x99)
                }
            };

            foreach (var item in categories)
            {
                var chip = new DashboardCategoryChip
                {
                    Title = item.Title,
                    Url = item.Url,
                    CategoryKey = item.CategoryKey,
                    IconGlyph = item.Glyph,
                    AccentColorLight = item.AccentLight,
                    AccentColorDark = item.AccentDark,
                    BadgeBgLight = item.BadgeBgLight,
                    BadgeBgDark = item.BadgeBgDark,
                    BorderLight = item.BorderLight,
                    BorderDark = item.BorderDark,
                    Height = 52
                };
                chip.ApplyTheme(_isDark);
                chip.ChipClicked += (s, url) => CategoryRequested?.Invoke(this, url);
                _categoryChips.Add(chip);
                pnlCategoriesHub.Controls.Add(chip);
            }
        }

        private void LayoutCategoriesHub()
        {
            if (pnlCategoriesHub == null || _categoryChips == null || _categoryChips.Count == 0) return;

            int containerW = pnlCategoriesHub.ClientSize.Width;
            if (containerW <= 0) return;

            int count = _categoryChips.Count;
            int gap = 10;
            int chipH = 52;
            int rowGap = 8;
            int minChipW = 96;
            bool isRtl = RightToLeft == RightToLeft.Yes;

            int columns = Math.Clamp((containerW + gap) / (minChipW + gap), 1, count);
            int chipW = Math.Max(0, (containerW - (gap * (columns - 1))) / columns);
            int rows = (count + columns - 1) / columns;

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                int first = row * columns;
                int inRow = Math.Min(columns, count - first);
                int rowWidth = (inRow * chipW) + ((inRow - 1) * gap);
                int startX = Math.Max(0, (containerW - rowWidth) / 2);
                int logicalX = startX + (col * (chipW + gap));

                var chip = _categoryChips[i];
                chip.Size = new Size(chipW, chipH);
                chip.Location = new Point(isRtl ? containerW - logicalX - chipW : logicalX, row * (chipH + rowGap));
            }

            pnlCategoriesHub.Height = (rows * chipH) + ((rows - 1) * rowGap);
        }

        private DashboardActionCard CreateActionCard(string title, string subtitle, string glyph, Action clickAction)
        {
            var card = new DashboardActionCard
            {
                Size = new Size(290, 88),
                Title = title,
                Subtitle = subtitle,
                IconGlyph = glyph
            };
            card.CardClicked += (s, e) => clickAction();
            return card;
        }

        private void CenterContentPanel()
        {
            if (pnlScrollContainer == null || pnlContent == null) return;

            int clientW = pnlScrollContainer.ClientSize.Width;
            if (clientW <= 0) return;

            int maxContentWidth = 960;
            int contentWidth = Math.Min(maxContentWidth, Math.Max(360, clientW));

            pnlContent.Width = contentWidth;

            // Center pnlContent horizontally in pnlScrollContainer
            if (clientW > contentWidth)
            {
                pnlContent.Left = (clientW - contentWidth) / 2;
            }
            else
            {
                pnlContent.Left = 0;
            }

            int cardMarginX = 24;
            int cardWidth = Math.Max(280, contentWidth - (cardMarginX * 2));
            int cardX = (contentWidth - cardWidth) / 2;

            if (pnlHeader != null)
            {
                pnlHeader.Location = new Point(cardX, pnlHeader.Top);
                pnlHeader.Width = cardWidth;
                if (pbLogo != null)
                {
                    pbLogo.Left = (pnlHeader.ClientSize.Width - pbLogo.Width) / 2;
                    pbLogo.Top = (pnlHeader.ClientSize.Height - pbLogo.Height) / 2;
                }
            }

            if (cardStartBrowsing != null)
            {
                cardStartBrowsing.Location = new Point(cardX, cardStartBrowsing.Top);
                cardStartBrowsing.Width = cardWidth;
            }

            int searchTop = searchBar?.Top ?? 0;

            if (pnlCategoriesHub != null)
            {
                pnlCategoriesHub.Location = new Point(cardX, pnlCategoriesHub.Top);
                pnlCategoriesHub.Width = cardWidth;
                LayoutCategoriesHub();
                searchTop = pnlCategoriesHub.Bottom + 17;
            }

            if (searchBar != null)
            {
                searchBar.Location = new Point(cardX, searchTop);
                searchBar.Width = cardWidth;
            }

            int currentY = (searchBar != null) ? searchBar.Bottom + 6 : 0;

            if (pnlRecentSearches != null)
            {
                pnlRecentSearches.Location = new Point(cardX, currentY);
                pnlRecentSearches.Width = cardWidth;
                LayoutRecentSearches();
                if (pnlRecentSearches.Visible)
                {
                    currentY += pnlRecentSearches.Height + 14;
                }
                else
                {
                    currentY += 8;
                }
            }
            else
            {
                currentY += 14;
            }

            if (cardPersonalWebsite != null)
            {
                cardPersonalWebsite.Location = new Point(cardX, currentY);
                cardPersonalWebsite.Width = cardWidth;
                currentY += cardPersonalWebsite.Height + 14;
            }

            if (cardMetrics != null)
            {
                cardMetrics.Location = new Point(cardX, currentY);
                cardMetrics.Width = cardWidth;
                currentY += cardMetrics.Height + 14;
            }

            if (cardUser != null)
            {
                cardUser.Location = new Point(cardX, currentY);
                cardUser.Width = cardWidth;
                currentY += cardUser.Height + 14;
            }

            if (pnlCardsContainer != null)
            {
                pnlCardsContainer.Location = new Point(cardX, currentY);
                pnlCardsContainer.Width = cardWidth;
                LayoutActionCards();
                pnlContent.Height = Math.Max(840, pnlCardsContainer.Bottom + 32);
            }
        }

        public void UpdateUserInfo(UserInfo? user)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateUserInfo(user)));
                return;
            }
            cardUser?.SetUserInfo(user);
        }

        public void ApplyTheme(bool isDark)
        {
            _isDark = isDark;

            Color bgColor = isDark
                ? UIConstants.DarkMode.BrandBackground
                : UIConstants.BrandBackground;

            BackColor = bgColor;
            pnlScrollContainer.BackColor = bgColor;
            pnlContent.BackColor = bgColor;
            if (pnlHeader != null) pnlHeader.BackColor = bgColor;
            if (pnlCardsContainer != null) pnlCardsContainer.BackColor = bgColor;

            if (pbLogo != null)
            {
                try
                {
                    var logoImg = _assetService.LoadLogo(isDark);
                    if (logoImg != null)
                    {
                        var oldImg = pbLogo.Image;
                        pbLogo.Image = logoImg;
                        oldImg?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to update logo in DashboardView: {ex.Message}");
                }
            }

            cardStartBrowsing.ApplyTheme(isDark);
            cardPersonalWebsite.ApplyTheme(isDark);
            if (_categoryChips != null)
            {
                foreach (var chip in _categoryChips)
                {
                    chip.ApplyTheme(isDark);
                }
            }
            cardMetrics.ApplyTheme(isDark);
            cardUser?.ApplyTheme(isDark);

            cardSettings.ApplyTheme(isDark);
            cardUsage.ApplyTheme(isDark);
            cardHistory.ApplyTheme(isDark);
            cardDownloads.ApplyTheme(isDark);
            cardBookmarks.ApplyTheme(isDark);
            cardShortcuts.ApplyTheme(isDark);
            cardContact.ApplyTheme(isDark);
            cardAbout.ApplyTheme(isDark);
            searchBar?.ApplyTheme(isDark);

            if (pnlRecentSearches != null) pnlRecentSearches.BackColor = bgColor;
            Color mutedCol = isDark ? Color.FromArgb(189, 181, 170) : Color.FromArgb(105, 95, 88);
            if (lblRecentIcon != null) lblRecentIcon.ForeColor = mutedCol;
            if (lblRecentText != null) lblRecentText.ForeColor = mutedCol;
            if (_recentSearchChips != null)
            {
                foreach (var chip in _recentSearchChips)
                {
                    chip.ApplyTheme(isDark);
                }
            }

            Invalidate(true);
        }

        public void ApplyTexts()
        {
            RightToLeft = RightToLeft.Yes;
            LayoutActionCards();

            cardStartBrowsing.Title = TextManager.GetString("DashboardStartBrowsing");
            cardStartBrowsing.Subtitle = TextManager.GetString("DashboardStartBrowsingDesc");
            cardStartBrowsing.BadgeText = TextManager.GetString("DashboardStartBrowsingBadge");

            searchBar?.ApplyTexts();

            if (lblRecentText != null)
            {
                lblRecentText.Text = TextManager.GetString("SearchRecentQueries");
                var hSize = TextRenderer.MeasureText(lblRecentText.Text, lblRecentText.Font);
                lblRecentText.Width = hSize.Width + 4;
            }

            cardPersonalWebsite.Title = TextManager.GetString("DashboardPersonalWebsite");
            cardPersonalWebsite.Subtitle = TextManager.GetString("DashboardPersonalWebsiteDesc");
            cardPersonalWebsite.BadgeText = TextManager.GetString("DashboardPersonalWebsiteBadge");

            cardMetrics.RefreshTexts();

            cardSettings.Title = TextManager.GetString("DashboardCardSettings");
            cardSettings.Subtitle = TextManager.GetString("DashboardCardSettingsDesc");

            cardUsage.Title = TextManager.GetString("DashboardCardUsage");
            cardUsage.Subtitle = TextManager.GetString("DashboardCardUsageDesc");

            cardHistory.Title = TextManager.GetString("DashboardCardHistory");
            cardHistory.Subtitle = TextManager.GetString("DashboardCardHistoryDesc");

            cardDownloads.Title = TextManager.GetString("DashboardCardDownloads");
            cardDownloads.Subtitle = TextManager.GetString("DashboardCardDownloadsDesc");

            cardBookmarks.Title = TextManager.GetString("DashboardCardBookmarks");
            cardBookmarks.Subtitle = TextManager.GetString("DashboardCardBookmarksDesc");

            cardShortcuts.Title = TextManager.GetString("DashboardCardShortcuts");
            cardShortcuts.Subtitle = TextManager.GetString("DashboardCardShortcutsDesc");

            cardContact.Title = TextManager.GetString("DashboardCardContact");
            cardContact.Subtitle = TextManager.GetString("DashboardCardContactDesc");

            cardAbout.Title = TextManager.GetString("DashboardCardAbout");
            cardAbout.Subtitle = TextManager.GetString("DashboardCardAboutDesc");

            CenterContentPanel();
            RefreshMetrics();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                RefreshRecentSearches();
            }
        }

        public void RefreshMetrics()
        {
            try
            {
                RefreshRecentSearches();
            }
            catch { }

            try
            {
                var totalMs = _usageTracker.GetTotalLifetimeUsageMilliseconds();
                var duration = TimeSpan.FromMilliseconds(totalMs);
                string durationStr = DurationFormatter.FormatUsageDuration(duration);

                var totalBytes = _usageTracker.GetTotalLifetimeInternetBytes();
                string dataStr = UsageTrackerService.FormatDataSize(totalBytes);

                int pageCount = _settingsManager.GetHistoryCount();
                string pageCountStr = $"{PersianNumberHelper.ToPersianDigits(pageCount)} صفحه";

                cardMetrics.SetMetrics(durationStr, pageCountStr, dataStr);

                // Fetch online users non-blockingly (force refresh on dashboard display)
                _ = LoadOnlineUsersAsync(forceRefresh: true);

                // Fetch and update category badges from RSS non-blockingly
                _ = RefreshCategoryBadgesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to refresh dashboard metrics: {ex.Message}");
            }
        }

        public async Task RefreshCategoryBadgesAsync()
        {
            if (_rssFeedService == null) return;
            try
            {
                // If offline, skip network RSS fetch to ensure instantaneous dashboard loading
                try
                {
                    if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        return;
                    }
                }
                catch { /* ignored */ }

                var counts = await _rssFeedService.GetRecentCategoryCountsAsync(TimeSpan.FromDays(7));
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => ApplyCategoryBadges(counts)));
                }
                else
                {
                    ApplyCategoryBadges(counts);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DashboardView] Error refreshing category badges: {ex.Message}");
            }
        }

        private void ApplyCategoryBadges(Dictionary<string, int> counts)
        {
            if (_categoryChips == null || _categoryChips.Count == 0) return;
            foreach (var chip in _categoryChips)
            {
                if (!string.IsNullOrWhiteSpace(chip.CategoryKey) && counts.TryGetValue(chip.CategoryKey, out var count))
                {
                    chip.BadgeCount = count;
                }
                else
                {
                    chip.BadgeCount = 0;
                }
            }
        }

        private async Task LoadOnlineUsersAsync(bool forceRefresh = false)
        {
            if (_onlineUsersService == null) return;
            try
            {
                var count = await _onlineUsersService.GetOnlineUsersCountAsync(forceRefresh);
                if (count.HasValue)
                {
                    string onlineStr = $"{PersianNumberHelper.ToPersianDigits(count.Value)} نفر";
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => cardMetrics.UpdateOnlineUsers(onlineStr, true)));
                    }
                    else
                    {
                        cardMetrics.UpdateOnlineUsers(onlineStr, true);
                    }
                }
                else
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => cardMetrics.UpdateOnlineUsers("--", false)));
                    }
                    else
                    {
                        cardMetrics.UpdateOnlineUsers("--", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load online users in DashboardView: {ex.Message}");
            }
        }

        public void SetUpdateBadge(bool hasUpdate, string? badgeText = null)
        {
            cardSettings.BadgeText = hasUpdate ? (badgeText ?? "جدید") : null;
            cardSettings.BadgeColor = Color.FromArgb(198, 40, 40); // Luxury red badge
        }


    }
}

