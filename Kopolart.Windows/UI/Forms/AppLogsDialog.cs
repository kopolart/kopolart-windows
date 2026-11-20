using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controls;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class AppLogsDialog : ThemedDialog
    {
        private readonly ISettingsManager _settingsManager;
        private readonly DateTime _sessionStartTime;
        private readonly IUserIpLogger? _userIpLogger;
        private readonly UsageTrackerService? _usageTracker;

        private int _selectedDaysRange = 7;
        private int _currentTabIndex = 0; // 0 = Chart, 1 = Sessions

        // Main controls
        private Label lblTitle = null!;
        private Button btnTabChart = null!;
        private Button btnTabSessions = null!;
        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private Panel pnlFooter = null!;
        private Button btnClear = null!;
        private Button btnClose = null!;
        private Timer timerDuration = null!;
        private Color _currentCardBorderColor = Color.Transparent;
        private readonly ToolTip _cardDataToolTip = new();

        // Tab 1: Chart & Analytics controls
        private Panel pnlChartTab = null!;
        private TableLayoutPanel tlpKpiCards = null!;
        private Panel pnlCardToday = null!;
        private Panel pnlCardAverage = null!;
        private Panel pnlCardPeak = null!;
        private Panel pnlCardTotal = null!;
        private Panel pnlCardData = null!;
        private Label lblCardTodayTitle = null!;
        private Label lblCardTodayVal = null!;
        private Label lblCardAvgTitle = null!;
        private Label lblCardAvgVal = null!;
        private Label lblCardPeakTitle = null!;
        private Label lblCardPeakVal = null!;
        private Label lblCardTotalTitle = null!;
        private Label lblCardTotalVal = null!;
        private Label lblCardDataTitle = null!;
        private Label lblCardDataVal = null!;

        private Panel pnlRangeBar = null!;
        private Button btnRange7 = null!;
        private Button btnRange14 = null!;
        private Button btnRange30 = null!;
        private Button btnRangeYear = null!;
        private Label lblRangeSummary = null!;
        private DailyUsageChartControl chartControl = null!;
        private Label lblHoverDetail = null!;

        // Tab 2: Sessions controls
        private Panel pnlSessionsTab = null!;
        private Label lblDuration = null!;
        private Label lblTotalUsage = null!;
        private Label lblTotalData = null!;
        private ListBox lstLogs = null!;

        public AppLogsDialog(
            ISettingsManager settingsManager,
            bool isDark,
            DateTime sessionStartTime,
            UsageTrackerService? usageTracker = null,
            IUserIpLogger? userIpLogger = null,
            int initialTab = 0)
            : base(isDark)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _sessionStartTime = sessionStartTime;
            _userIpLogger = userIpLogger ?? (IUserIpLogger?)Program.ServiceProvider?.GetService(typeof(IUserIpLogger));
            _usageTracker = usageTracker;
            _currentTabIndex = initialTab;

            InitializeComponent();
            FontManager.ApplyCustomFont(this);

            SwitchTab(_currentTabIndex);
            SetRange(7);
            LoadLogs();
            UpdateDuration();

            InitializeTheme();

            timerDuration.Start();
        }


        private void InitializeComponent()
        {
            lblTitle = new Label();
            btnTabChart = new Button();
            btnTabSessions = new Button();
            pnlHeader = new Panel();
            pnlContent = new Panel();
            pnlFooter = new Panel();
            btnClear = new Button();
            btnClose = new Button();
            timerDuration = new Timer();

            pnlChartTab = new Panel();
            tlpKpiCards = new TableLayoutPanel();
            pnlCardToday = new Panel();
            pnlCardAverage = new Panel();
            pnlCardPeak = new Panel();
            pnlCardTotal = new Panel();
            pnlCardData = new Panel();

            lblCardTodayTitle = new Label();
            lblCardTodayVal = new Label();
            lblCardAvgTitle = new Label();
            lblCardAvgVal = new Label();
            lblCardPeakTitle = new Label();
            lblCardPeakVal = new Label();
            lblCardTotalTitle = new Label();
            lblCardTotalVal = new Label();
            lblCardDataTitle = new Label();
            lblCardDataVal = new Label();

            pnlRangeBar = new Panel();
            btnRange7 = new Button();
            btnRange14 = new Button();
            btnRange30 = new Button();
            btnRangeYear = new Button();
            lblRangeSummary = new Label();
            chartControl = new DailyUsageChartControl();
            lblHoverDetail = new Label();

            pnlSessionsTab = new Panel();
            lblDuration = new Label();
            lblTotalUsage = new Label();
            lblTotalData = new Label();
            lstLogs = new ListBox();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(860, 640);
            MinimumSize = new Size(740, 540);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("AppLogsTitle");

            // ==================== Header Panel ====================
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 54;
            pnlHeader.Padding = new Padding(16, 8, 16, 8);

            lblTitle.Text = TextManager.GetString("AppLogsTitle");
            lblTitle.Font = FontManager.CreateBlackFont(12F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(pnlHeader.ClientSize.Width - 16 - lblTitle.PreferredWidth, (pnlHeader.ClientSize.Height - lblTitle.PreferredHeight) / 2);
            pnlHeader.Resize += (s, e) =>
            {
                lblTitle.Location = new Point(pnlHeader.ClientSize.Width - 16 - lblTitle.PreferredWidth, (pnlHeader.ClientSize.Height - lblTitle.PreferredHeight) / 2);
            };

            btnTabChart.Text = TextManager.GetString("AppLogsTabDailyChart");
            btnTabChart.Size = new Size(160, 36);
            btnTabChart.Cursor = Cursors.Hand;
            btnTabChart.FlatStyle = FlatStyle.Flat;
            btnTabChart.FlatAppearance.BorderSize = 1;
            btnTabChart.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            btnTabChart.Click += (s, e) => SwitchTab(0);

            btnTabSessions.Text = TextManager.GetString("AppLogsTabSessions");
            btnTabSessions.Size = new Size(150, 36);
            btnTabSessions.Cursor = Cursors.Hand;
            btnTabSessions.FlatStyle = FlatStyle.Flat;
            btnTabSessions.FlatAppearance.BorderSize = 1;
            btnTabSessions.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            btnTabSessions.Click += (s, e) => SwitchTab(1);

            var pnlTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(0, 2, 0, 0)
            };
            btnTabSessions.Margin = new Padding(0, 0, 8, 0);
            btnTabChart.Margin = new Padding(0, 0, 0, 0);
            pnlTabs.Controls.Add(btnTabSessions);
            pnlTabs.Controls.Add(btnTabChart);

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(pnlTabs);

            // ==================== Footer Panel ====================
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Height = 56;
            pnlFooter.Padding = new Padding(16, 10, 16, 12);

            btnClear.Text = TextManager.GetString("AppLogsClear");
            btnClear.Size = new Size(140, 32);
            btnClear.Cursor = Cursors.Hand;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClear.Click += BtnClear_Click;
            btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClear.Location = new Point(pnlFooter.Width - 140 - 16, 12);

            btnClose.Text = TextManager.GetString("Close");
            btnClose.Size = new Size(96, 32);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            btnClose.Click += BtnClose_Click;
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnClose.Location = new Point(16, 12);

            pnlFooter.Controls.Add(btnClear);
            pnlFooter.Controls.Add(btnClose);

            // ==================== Content Panel ====================
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(16, 6, 16, 14);

            // ==================== Tab 1: Chart Panel ====================
            pnlChartTab.Dock = DockStyle.Fill;
            pnlChartTab.Padding = new Padding(0, 0, 0, 8);

            // KPI Cards TableLayoutPanel (5 columns)
            tlpKpiCards.Dock = DockStyle.Top;
            tlpKpiCards.Height = 92;
            tlpKpiCards.ColumnCount = 5;
            tlpKpiCards.RowCount = 1;
            tlpKpiCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpKpiCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpiCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpiCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpiCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKpiCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            SetupKpiCard(pnlCardToday, lblCardTodayTitle, lblCardTodayVal, TextManager.GetString("DailyUsageToday"), "-");
            SetupKpiCard(pnlCardAverage, lblCardAvgTitle, lblCardAvgVal, TextManager.GetString("DailyUsageAverage"), "-");
            SetupKpiCard(pnlCardPeak, lblCardPeakTitle, lblCardPeakVal, TextManager.GetString("DailyUsagePeak"), "-");
            SetupKpiCard(pnlCardTotal, lblCardTotalTitle, lblCardTotalVal, TextManager.GetString("DailyUsageTotalPeriod"), "-");
            SetupKpiCard(pnlCardData, lblCardDataTitle, lblCardDataVal, TextManager.GetString("DailyUsageDataUsed"), "-");

            tlpKpiCards.Controls.Add(pnlCardToday, 0, 0);
            tlpKpiCards.Controls.Add(pnlCardAverage, 1, 0);
            tlpKpiCards.Controls.Add(pnlCardPeak, 2, 0);
            tlpKpiCards.Controls.Add(pnlCardTotal, 3, 0);
            tlpKpiCards.Controls.Add(pnlCardData, 4, 0);

            // Range Bar
            pnlRangeBar.Dock = DockStyle.Top;
            pnlRangeBar.Height = 52;
            pnlRangeBar.Padding = new Padding(0, 12, 0, 12);

            btnRange7.Text = TextManager.GetString("DailyUsage7Days");
            btnRange7.Size = new Size(100, 28);
            btnRange7.Cursor = Cursors.Hand;
            btnRange7.FlatStyle = FlatStyle.Flat;
            btnRange7.FlatAppearance.BorderSize = 1;
            btnRange7.Location = new Point(0, 12);
            btnRange7.Click += (s, e) => SetRange(7);

            btnRange14.Text = TextManager.GetString("DailyUsage14Days");
            btnRange14.Size = new Size(100, 28);
            btnRange14.Cursor = Cursors.Hand;
            btnRange14.FlatStyle = FlatStyle.Flat;
            btnRange14.FlatAppearance.BorderSize = 1;
            btnRange14.Location = new Point(106, 12);
            btnRange14.Click += (s, e) => SetRange(14);

            btnRange30.Text = TextManager.GetString("DailyUsage30Days");
            btnRange30.Size = new Size(100, 28);
            btnRange30.Cursor = Cursors.Hand;
            btnRange30.FlatStyle = FlatStyle.Flat;
            btnRange30.FlatAppearance.BorderSize = 1;
            btnRange30.Location = new Point(212, 12);
            btnRange30.Click += (s, e) => SetRange(30);

            btnRangeYear.Text = TextManager.GetString("DailyUsage1Year");
            btnRangeYear.Size = new Size(100, 28);
            btnRangeYear.Cursor = Cursors.Hand;
            btnRangeYear.FlatStyle = FlatStyle.Flat;
            btnRangeYear.FlatAppearance.BorderSize = 1;
            btnRangeYear.Location = new Point(318, 12);
            btnRangeYear.Click += (s, e) => SetRange(365);

            lblRangeSummary.AutoSize = false;
            lblRangeSummary.Size = new Size(240, 28);
            lblRangeSummary.RightToLeft = RightToLeft.Yes;
            lblRangeSummary.TextAlign = ContentAlignment.MiddleLeft;
            lblRangeSummary.Location = new Point(428, 12);
            lblRangeSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            pnlRangeBar.Controls.Add(btnRange7);
            pnlRangeBar.Controls.Add(btnRange14);
            pnlRangeBar.Controls.Add(btnRange30);
            pnlRangeBar.Controls.Add(btnRangeYear);
            pnlRangeBar.Controls.Add(lblRangeSummary);

            // Chart Control
            var pnlChartContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 6),
                BackColor = Color.Transparent
            };
            chartControl.Dock = DockStyle.Fill;
            chartControl.SetDarkMode(IsDark);
            chartControl.BarHovered += ChartControl_BarHovered;
            pnlChartContainer.Controls.Add(chartControl);

            // Hover Detail Label
            lblHoverDetail.Dock = DockStyle.Bottom;
            lblHoverDetail.Height = 26;
            lblHoverDetail.TextAlign = ContentAlignment.MiddleCenter;
            lblHoverDetail.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            pnlChartTab.Controls.Add(pnlChartContainer);
            pnlChartTab.Controls.Add(lblHoverDetail);
            pnlChartTab.Controls.Add(pnlRangeBar);
            pnlChartTab.Controls.Add(tlpKpiCards);

            // ==================== Tab 2: Sessions Panel ====================
            pnlSessionsTab.Dock = DockStyle.Fill;

            lblDuration.Dock = DockStyle.Top;
            lblDuration.Height = 28;
            lblDuration.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);

            lblTotalUsage.Dock = DockStyle.Top;
            lblTotalUsage.Height = 28;
            lblTotalUsage.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);

            lblTotalData.Dock = DockStyle.Top;
            lblTotalData.Height = 28;
            lblTotalData.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);

            lstLogs.Dock = DockStyle.Fill;
            lstLogs.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            lstLogs.HorizontalScrollbar = true;

            pnlSessionsTab.Controls.Add(lstLogs);
            pnlSessionsTab.Controls.Add(lblTotalData);
            pnlSessionsTab.Controls.Add(lblTotalUsage);
            pnlSessionsTab.Controls.Add(lblDuration);

            // Assemble Form
            pnlContent.Controls.Add(pnlChartTab);
            pnlContent.Controls.Add(pnlSessionsTab);

            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);
            Controls.Add(pnlFooter);

            CancelButton = btnClose;

            // Timer
            timerDuration.Interval = 1000;
            timerDuration.Tick += TimerDuration_Tick;

            ResumeLayout(false);
            PerformLayout();
        }

        private void SetupKpiCard(Panel card, Label lblTitle, Label lblVal, string title, string val)
        {
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(4);
            card.Padding = new Padding(6, 6, 6, 6);

            lblTitle.Text = title;
            lblTitle.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 20;
            lblTitle.RightToLeft = RightToLeft.Yes;
            lblTitle.TextAlign = ContentAlignment.TopLeft;

            lblVal.Text = val;
            lblVal.Font = FontManager.CreateBlackFont(9.5F);
            lblVal.Dock = DockStyle.Fill;
            lblVal.RightToLeft = RightToLeft.Yes;
            lblVal.TextAlign = ContentAlignment.TopLeft;

            card.Controls.Add(lblVal);
            card.Controls.Add(lblTitle);
            card.Paint += Card_Paint;
        }

        private void SwitchTab(int tabIndex)
        {
            _currentTabIndex = tabIndex;
            bool isChart = tabIndex == 0;

            pnlChartTab.Visible = isChart;
            pnlSessionsTab.Visible = !isChart;

            Color activeBg = IsDark ? Color.FromArgb(70, 70, 70) : UIConstants.ThemeColor;
            Color activeFg = IsDark ? Color.White : UIConstants.TextThemeColor;
            Color inactiveBg = Color.Transparent;
            Color inactiveFg = IsDark ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);

            btnTabChart.BackColor = isChart ? activeBg : inactiveBg;
            btnTabChart.ForeColor = isChart ? activeFg : inactiveFg;
            btnTabChart.Font = FontManager.CreateFont(9.5F, isChart ? FontStyle.Bold : FontStyle.Regular);

            btnTabSessions.BackColor = !isChart ? activeBg : inactiveBg;
            btnTabSessions.ForeColor = !isChart ? activeFg : inactiveFg;
            btnTabSessions.Font = FontManager.CreateFont(9.5F, !isChart ? FontStyle.Bold : FontStyle.Regular);

            if (isChart)
            {
                RefreshChartData();
            }
        }

        private void SetRange(int days)
        {
            _selectedDaysRange = days;

            Color activeBg = IsDark ? Color.FromArgb(80, 75, 70) : Color.FromArgb(230, 215, 200);
            Color inactiveBg = Color.Transparent;

            btnRange7.BackColor = days == 7 ? activeBg : inactiveBg;
            btnRange7.Font = FontManager.CreateFont(8.5F, days == 7 ? FontStyle.Bold : FontStyle.Regular);

            btnRange14.BackColor = days == 14 ? activeBg : inactiveBg;
            btnRange14.Font = FontManager.CreateFont(8.5F, days == 14 ? FontStyle.Bold : FontStyle.Regular);

            btnRange30.BackColor = days == 30 ? activeBg : inactiveBg;
            btnRange30.Font = FontManager.CreateFont(8.5F, days == 30 ? FontStyle.Bold : FontStyle.Regular);

            btnRangeYear.BackColor = days >= 365 ? activeBg : inactiveBg;
            btnRangeYear.Font = FontManager.CreateFont(8.5F, days >= 365 ? FontStyle.Bold : FontStyle.Regular);

            RefreshChartData();
        }

        private void RefreshChartData()
        {
            Dictionary<string, long> liveDaily;

            if (_usageTracker != null)
            {
                liveDaily = _usageTracker.GetLiveDailyUsage();
            }
            else
            {
                liveDaily = _settingsManager.GetDailyUsage();
                // Add current in-flight session to today
                var todayKey = DateTime.Today.ToString("yyyy-MM-dd");
                var inFlightMs = (long)(DateTime.Now - _sessionStartTime).TotalMilliseconds;
                liveDaily[todayKey] = liveDaily.GetValueOrDefault(todayKey, 0L) + inFlightMs;
            }

            chartControl.SetData(liveDaily, _selectedDaysRange);
            UpdateKpiCards(liveDaily);
        }

        private void UpdateKpiCards(Dictionary<string, long> dailyUsage)
        {
            var dailyInternet = _usageTracker?.GetDailyInternetBytes() ?? _settingsManager.GetDailyInternetBytes();
            long totalLifetimeInternetBytes = _usageTracker?.GetTotalLifetimeInternetBytes() ?? _settingsManager.GetTotalInternetBytes();
            long todayDataBytes = _usageTracker?.GetTodayInternetBytes() ?? 0L;

            var peak = chartControl.PeakItem;
            var summary = UsageAnalyticsService.CalculateKpiSummary(
                dailyUsage,
                dailyInternet,
                totalLifetimeInternetBytes,
                todayDataBytes,
                _selectedDaysRange,
                chartControl.IsMonthlyMode,
                chartControl.TotalPeriodMilliseconds,
                (long)chartControl.AverageMilliseconds,
                peak?.Milliseconds ?? 0,
                peak?.DayName);

            lblCardTodayVal.Text = summary.FormattedTodayUsage;

            if (chartControl.IsMonthlyMode)
            {
                lblCardAvgTitle.Text = TextManager.GetString("MonthlyUsageAverage");
                lblCardPeakTitle.Text = TextManager.GetString("MonthlyUsagePeak");
                lblCardDataTitle.Text = TextManager.GetString("DailyUsageTotalData");
            }
            else
            {
                lblCardAvgTitle.Text = TextManager.GetString("DailyUsageAverage");
                lblCardPeakTitle.Text = TextManager.GetString("DailyUsagePeak");
                lblCardDataTitle.Text = TextManager.GetString("DailyUsageDataUsed");
            }

            lblCardAvgVal.Text = summary.FormattedAverageUsage;
            lblCardTotalVal.Text = summary.FormattedTotalUsage;
            lblCardPeakVal.Text = summary.PeakLabel;
            lblCardDataVal.Text = summary.FormattedPeriodData;

            _cardDataToolTip.SetToolTip(pnlCardData, summary.TooltipText);
            _cardDataToolTip.SetToolTip(lblCardDataTitle, summary.TooltipText);
            _cardDataToolTip.SetToolTip(lblCardDataVal, summary.TooltipText);

            if (!string.IsNullOrWhiteSpace(summary.PeakLabel) && summary.PeakLabel != "-")
            {
                string peakTooltip = summary.PeakLabel.Replace("\n", " - ");
                _cardDataToolTip.SetToolTip(pnlCardPeak, peakTooltip);
                _cardDataToolTip.SetToolTip(lblCardPeakTitle, peakTooltip);
                _cardDataToolTip.SetToolTip(lblCardPeakVal, peakTooltip);
            }
        }

        private void ChartControl_BarHovered(object? sender, DailyChartBarData? item)
        {
            if (item == null)
            {
                lblHoverDetail.Text = "";
            }
            else
            {
                lblHoverDetail.Text = $"{item.FullDateString}  -  {item.FormattedDuration} ({item.PercentageOfTotal:0.#}%)";
            }
        }

        private void LoadLogs()
        {
            using var paintScope = lstLogs.SuspendPainting();
            lstLogs.Items.Clear();
            var list = _settingsManager.GetUsageLog();
            if (list != null && list.Count > 0)
            {
                var culture = new CultureInfo("fa-IR");
                culture.DateTimeFormat.Calendar = new PersianCalendar();

                var ordered = list.OrderByDescending(t => t.Date).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    var session = ordered[i];
                    TimeSpan duration = (i == 0 && _usageTracker != null && _usageTracker.GetSessionBrowsingDuration() > TimeSpan.Zero)
                        ? _usageTracker.GetSessionBrowsingDuration()
                        : TimeSpan.FromSeconds(session.DurationSeconds);

                    lstLogs.Items.Add(FormatSessionLogItem(session, duration, culture));
                }
            }
            btnClear.Enabled = true;
        }

        private static string FormatSessionLogItem(UsageSession session, TimeSpan duration, CultureInfo culture)
        {
            string formatStr = "dddd d MMMM yyyy\u060C ساعت HH:mm:ss";
            string text = UsageAnalyticsService.ToPersianDigits(session.Date.ToString(formatStr, culture));
            if (duration > TimeSpan.Zero)
            {
                string durationStr = DurationFormatter.FormatUsageDuration(duration);
                text += $" - مدت: {UsageAnalyticsService.ToPersianDigits(durationStr)}";
            }
            if (!string.IsNullOrWhiteSpace(session.Ip))
            {
                string ipStr = session.Ip.Trim();
                if (!ipStr.StartsWith("IP:", StringComparison.OrdinalIgnoreCase))
                {
                    ipStr = $"IP: {ipStr}";
                }
                text += $" - {UsageAnalyticsService.ToPersianDigits(ipStr)}";
            }
            return text;
        }

        private string FormatDurationString(TimeSpan elapsed)
        {
            return DurationFormatter.FormatUsageDuration(elapsed);
        }

        private void UpdateDuration()
        {
            var currentSessionElapsed = _usageTracker != null
                ? _usageTracker.GetSessionBrowsingDuration()
                : TimeSpan.Zero;
            TimeSpan totalElapsed;

            if (_usageTracker != null)
            {
                var totalMs = _usageTracker.GetTotalLifetimeUsageMilliseconds();
                totalElapsed = TimeSpan.FromMilliseconds(totalMs);
            }
            else
            {
                var totalPastSeconds = _settingsManager.GetTotalPastUsageSeconds();
                totalElapsed = TimeSpan.FromSeconds(totalPastSeconds) + currentSessionElapsed;
            }

            string currentSessionText = FormatDurationString(currentSessionElapsed);
            string totalUsageText = FormatDurationString(totalElapsed);

            long totalBytes = _usageTracker != null
                ? _usageTracker.GetTotalLifetimeInternetBytes()
                : _settingsManager.GetTotalInternetBytes();
            string totalDataText = UsageTrackerService.FormatDataSize(totalBytes);

            lblDuration.Text = $"{TextManager.GetString("AppLogsDuration")}: {currentSessionText}";
            lblTotalUsage.Text = $"{TextManager.GetString("AppLogsTotalUsage")}: {totalUsageText}";
            lblTotalData.Text = $"{TextManager.GetString("DailyUsageTotalData")}: {totalDataText}";

            // Live update today's card & chart if on chart tab
            if (_currentTabIndex == 0)
            {
                RefreshChartData();
            }
            else if (_currentTabIndex == 1 && lstLogs.Items.Count > 0 && _usageTracker != null)
            {
                var list = _settingsManager.GetUsageLog();
                if (list != null && list.Count > 0)
                {
                    var latest = list[^1];
                    var culture = new CultureInfo("fa-IR");
                    culture.DateTimeFormat.Calendar = new PersianCalendar();
                    var duration = _usageTracker.GetSessionBrowsingDuration();
                    string updatedText = FormatSessionLogItem(latest, duration, culture);
                    if ((string)lstLogs.Items[0] != updatedText)
                    {
                        lstLogs.Items[0] = updatedText;
                    }
                }
            }
        }

        private void TimerDuration_Tick(object? sender, EventArgs e)
        {
            UpdateDuration();
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            var confirmResult = CustomMessageBox.Show(this,
                TextManager.GetString("AppLogsClearConfirm"),
                TextManager.GetString("AppLogsClear"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, IsDark);

            if (confirmResult == DialogResult.Yes)
            {
                _settingsManager.ClearUsageLog();
                _settingsManager.ClearDailyUsage();
                _settingsManager.SetTotalPastUsageSeconds(0);
                _settingsManager.ClearAchievedMilestones();
                _settingsManager.ClearInternetBytes();
                _settingsManager.LogUsage(_sessionStartTime);

                LoadLogs();
                RefreshChartData();
                UpdateDuration();
            }
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_userIpLogger != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _userIpLogger.LogUserIpAsync();
                        if (!IsDisposed)
                        {
                            try
                            {
                                BeginInvoke(new Action(LoadLogs));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AppLogsDialog] BeginInvoke LoadLogs failed: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log user IP from logs dialog: {ex}");
                    }
                });
            }
        }

        private void ApplyLightTheme()
        {
            BackColor = Color.White;

            pnlHeader.BackColor = Color.FromArgb(248, 248, 248);
            lblTitle.ForeColor = Color.FromArgb(40, 40, 40);

            pnlFooter.BackColor = Color.FromArgb(248, 248, 248);

            // KPI Cards light mode
            Color cardBg = Color.White;
            Color cardBorder = Color.FromArgb(225, 218, 210);
            Color titleColor = Color.FromArgb(120, 110, 100);
            Color valColor = Color.FromArgb(50, 45, 40);

            ApplyCardTheme(pnlCardToday, lblCardTodayTitle, lblCardTodayVal, cardBg, cardBorder, titleColor, valColor);
            ApplyCardTheme(pnlCardAverage, lblCardAvgTitle, lblCardAvgVal, cardBg, cardBorder, titleColor, valColor);
            ApplyCardTheme(pnlCardPeak, lblCardPeakTitle, lblCardPeakVal, cardBg, cardBorder, titleColor, valColor);
            ApplyCardTheme(pnlCardTotal, lblCardTotalTitle, lblCardTotalVal, cardBg, cardBorder, titleColor, valColor);
            ApplyCardTheme(pnlCardData, lblCardDataTitle, lblCardDataVal, cardBg, cardBorder, titleColor, valColor);

            // Range buttons light mode
            Color rangeBorder = Color.FromArgb(210, 200, 190);
            btnRange7.FlatAppearance.BorderColor = rangeBorder;
            btnRange14.FlatAppearance.BorderColor = rangeBorder;
            btnRange30.FlatAppearance.BorderColor = rangeBorder;
            btnRangeYear.FlatAppearance.BorderColor = rangeBorder;
            lblRangeSummary.ForeColor = Color.FromArgb(120, 110, 100);
            lblHoverDetail.ForeColor = Color.FromArgb(100, 85, 70);

            // Sessions Tab
            lblDuration.ForeColor = Color.FromArgb(60, 60, 60);
            lblTotalUsage.ForeColor = Color.FromArgb(60, 60, 60);
            lblTotalData.ForeColor = Color.FromArgb(60, 60, 60);
            lstLogs.BackColor = Color.White;
            lstLogs.ForeColor = Color.FromArgb(30, 30, 30);
            lstLogs.BorderStyle = BorderStyle.Fixed3D;

            // Buttons
            btnTabChart.FlatAppearance.BorderColor = Color.FromArgb(200, 185, 170);
            btnTabSessions.FlatAppearance.BorderColor = Color.FromArgb(200, 185, 170);

            btnClear.BackColor = Color.Transparent;
            btnClear.ForeColor = UIConstants.DangerColor;
            btnClear.FlatAppearance.BorderColor = UIConstants.DangerColor;

            btnClose.BackColor = Color.FromArgb(220, 215, 205);
            btnClose.ForeColor = Color.FromArgb(50, 50, 50);

            chartControl.SetDarkMode(false);
        }

        private void ApplyDarkTheme()
        {
                BackColor = UIConstants.DarkMode.BrandBackground;

                pnlHeader.BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                pnlFooter.BackColor = UIConstants.DarkMode.BrandBackground;

                // KPI Cards dark mode
                Color cardBg = UIConstants.DarkMode.CardBackgroundDark;
                Color cardBorder = UIConstants.DarkMode.CardBorderDark;
                Color titleColor = Color.FromArgb(170, 160, 150);
                Color valColor = Color.FromArgb(235, 230, 220);

                ApplyCardTheme(pnlCardToday, lblCardTodayTitle, lblCardTodayVal, cardBg, cardBorder, titleColor, valColor);
                ApplyCardTheme(pnlCardAverage, lblCardAvgTitle, lblCardAvgVal, cardBg, cardBorder, titleColor, valColor);
                ApplyCardTheme(pnlCardPeak, lblCardPeakTitle, lblCardPeakVal, cardBg, cardBorder, titleColor, valColor);
                ApplyCardTheme(pnlCardTotal, lblCardTotalTitle, lblCardTotalVal, cardBg, cardBorder, titleColor, valColor);
                ApplyCardTheme(pnlCardData, lblCardDataTitle, lblCardDataVal, cardBg, cardBorder, titleColor, valColor);

                // Range buttons dark mode
                Color rangeBorder = Color.FromArgb(70, 70, 70);
                btnRange7.FlatAppearance.BorderColor = rangeBorder;
                btnRange7.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnRange14.FlatAppearance.BorderColor = rangeBorder;
                btnRange14.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnRange30.FlatAppearance.BorderColor = rangeBorder;
                btnRange30.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnRangeYear.FlatAppearance.BorderColor = rangeBorder;
                btnRangeYear.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblRangeSummary.ForeColor = Color.FromArgb(170, 160, 150);
                lblHoverDetail.ForeColor = Color.FromArgb(215, 180, 140);

                // Sessions Tab
                lblDuration.ForeColor = Color.FromArgb(200, 190, 180);
                lblTotalUsage.ForeColor = Color.FromArgb(200, 190, 180);
                lblTotalData.ForeColor = Color.FromArgb(200, 190, 180);
                lstLogs.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lstLogs.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lstLogs.BorderStyle = BorderStyle.FixedSingle;

                // Tab buttons
                btnTabChart.FlatAppearance.BorderColor = Color.FromArgb(75, 75, 75);
                btnTabSessions.FlatAppearance.BorderColor = Color.FromArgb(75, 75, 75);

                btnClear.BackColor = Color.Transparent;
                btnClear.ForeColor = UIConstants.DarkMode.DangerColor;
                btnClear.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;

                btnClose.BackColor = Color.FromArgb(60, 60, 60);
                btnClose.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                chartControl.SetDarkMode(true);
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark) ApplyDarkTheme();
            else ApplyLightTheme();
        }

        private void ApplyCardTheme(Panel card, Label title, Label val, Color bg, Color border, Color titleCol, Color valCol)
        {
            card.BackColor = bg;
            title.ForeColor = titleCol;
            val.ForeColor = valCol;
            _currentCardBorderColor = border;
            card.Invalidate();
        }

        private void Card_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is Panel card && _currentCardBorderColor != Color.Transparent)
            {
                Color borderColor = SystemAccessibility.IsHighContrast ? SystemColors.ControlDark : _currentCardBorderColor;
                using var p = new Pen(borderColor, 1);
                e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timerDuration?.Stop();
                timerDuration?.Dispose();
                chartControl?.Dispose();
                _cardDataToolTip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

