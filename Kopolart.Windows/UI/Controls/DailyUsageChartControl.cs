using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    public class DailyUsageChartControl : Control
    {
        private static string ToPersianDigits(string input) => DailyUsageChartDataBuilder.ToPersianDigits(input);
        private static string FormatDuration(long ms) => DailyUsageChartDataBuilder.FormatDuration(ms);
        private readonly List<DailyChartBarData> _items = new();
        private bool _isDark;
        private int _hoveredIndex = -1;
        private long _maxMs = 0;
        private double _averageMs = 0;
        private long _totalPeriodMs = 0;
        private bool _isMonthlyMode = false;

        private Font? _cachedFontSmall;
        private Font? _cachedAvgFont;
        private Font? _cachedFontRegular;
        private Font? _cachedFontBold;
        private Font? _cachedDateFontRegular;
        private Font? _cachedDateFontBold;
        private Font? _cachedHoverTitleFont;
        private Font? _cachedHoverValFont;

        private readonly StringFormat _sfY = new() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _sfTop = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft };
        private readonly StringFormat _sfCenter = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
        private readonly StringFormat _sfHover = new() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

        private void EnsureCachedFonts()
        {
            if (_cachedFontSmall != null) return;

            var family = Font.FontFamily;
            _cachedFontSmall = new Font(family, 7.5F, FontStyle.Regular);
            _cachedAvgFont = new Font(family, 7.5F, FontStyle.Bold);
            _cachedFontRegular = new Font(family, 7.5F, FontStyle.Regular);
            _cachedFontBold = new Font(family, 7.5F, FontStyle.Bold);
            _cachedDateFontRegular = new Font(family, 7F, FontStyle.Regular);
            _cachedDateFontBold = new Font(family, 7F, FontStyle.Bold);
            _cachedHoverTitleFont = new Font(family, 8.5F, FontStyle.Bold);
            _cachedHoverValFont = new Font(family, 8F, FontStyle.Regular);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ReleaseCachedFonts();
            Invalidate();
        }

        private void ReleaseCachedFonts()
        {
            _cachedFontSmall?.Dispose();
            _cachedAvgFont?.Dispose();
            _cachedFontRegular?.Dispose();
            _cachedFontBold?.Dispose();
            _cachedDateFontRegular?.Dispose();
            _cachedDateFontBold?.Dispose();
            _cachedHoverTitleFont?.Dispose();
            _cachedHoverValFont?.Dispose();

            _cachedFontSmall = null;
            _cachedAvgFont = null;
            _cachedFontRegular = null;
            _cachedFontBold = null;
            _cachedDateFontRegular = null;
            _cachedDateFontBold = null;
            _cachedHoverTitleFont = null;
            _cachedHoverValFont = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseCachedFonts();
                _sfY.Dispose();
                _sfTop.Dispose();
                _sfCenter.Dispose();
                _sfHover.Dispose();
            }
            base.Dispose(disposing);
        }

        public long TotalPeriodMilliseconds => _totalPeriodMs;
        public double AverageMilliseconds => _averageMs;
        public bool IsMonthlyMode => _isMonthlyMode;
        public DailyChartBarData? PeakItem => _items.Where(x => x.Milliseconds > 0).OrderByDescending(x => x.Milliseconds).FirstOrDefault();

        public event EventHandler<DailyChartBarData?>? BarHovered;

        public DailyUsageChartControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            Cursor = Cursors.Default;
        }


        public void SetDarkMode(bool isDark)
        {
            _isDark = isDark;
            Invalidate();
        }

        public void SetData(Dictionary<string, long> dailyUsage, int daysCount)
        {
            _items.Clear();
            _hoveredIndex = -1;

            var dataSet = DailyUsageChartDataBuilder.BuildDataSet(dailyUsage, daysCount);
            _items.AddRange(dataSet.Items);
            _totalPeriodMs = dataSet.TotalMilliseconds;
            _averageMs = dataSet.AverageMilliseconds;
            _maxMs = dataSet.MaxMilliseconds;
            _isMonthlyMode = dataSet.IsMonthlyMode;

            Invalidate();
        }

        private string FormatDetailedDuration(long ms)
        {
            if (ms <= 0) return "استفاده نشده";
            var ts = TimeSpan.FromMilliseconds(ms);
            int h = (int)ts.TotalHours;
            int m = ts.Minutes;
            int s = ts.Seconds;

            var parts = new List<string>();
            if (h > 0) parts.Add($"{DailyUsageChartDataBuilder.ToPersianDigits(h.ToString())} ساعت");
            if (m > 0) parts.Add($"{DailyUsageChartDataBuilder.ToPersianDigits(m.ToString())} دقیقه");
            if (s > 0 || parts.Count == 0) parts.Add($"{DailyUsageChartDataBuilder.ToPersianDigits(s.ToString())} ثانیه");
            return string.Join(" و ", parts);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int newHovered = -1;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ColumnHitArea.Contains(e.Location))
                {
                    newHovered = i;
                    break;
                }
            }

            if (newHovered != _hoveredIndex)
            {
                _hoveredIndex = newHovered;
                Invalidate();
                BarHovered?.Invoke(this, _hoveredIndex >= 0 ? _items[_hoveredIndex] : null);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Invalidate();
                BarHovered?.Invoke(this, null);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            bool highContrast = SystemAccessibility.IsHighContrast;

            // Background
            var bgCol = highContrast
                ? SystemColors.Window
                : _isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight;
            using (var bgBrush = new SolidBrush(bgCol))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Border
            var borderCol = highContrast
                ? SystemColors.ControlDark
                : _isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight;
            using (var borderPen = new Pen(borderCol, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            if (_items.Count == 0)
            {
                string emptyText = TextManager.GetString("DailyUsageNoData");
                using var emptyBrush = new SolidBrush(highContrast ? SystemColors.WindowText : _isDark ? Color.Gray : Color.DarkGray);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(emptyText, Font, emptyBrush, ClientRectangle, sf);
                return;
            }

            EnsureCachedFonts();

            // Layout metrics (RTL: Y-axis on right, bars flow right-to-left)
            float leftPad = 25f;
            float rightPad = 52f;
            float topPad = 40f;
            float bottomPad = 68f;

            float chartWidth = Width - leftPad - rightPad;
            float chartHeight = Height - topPad - bottomPad;

            if (chartWidth <= 10 || chartHeight <= 10)
                return;

            float baselineY = topPad + chartHeight;
            float chartRight = leftPad + chartWidth;

            // Colors
            Color gridLineCol = highContrast ? SystemColors.ControlDark : _isDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(235, 228, 220);
            Color textCol = highContrast ? SystemColors.WindowText : _isDark ? Color.FromArgb(170, 170, 170) : Color.FromArgb(110, 100, 90);
            Color barColNormal = highContrast ? SystemColors.ControlText : _isDark ? Color.FromArgb(145, 110, 85) : Color.FromArgb(196, 154, 108);
            Color barColToday = highContrast ? SystemColors.Highlight : _isDark ? Color.FromArgb(195, 140, 95) : Color.FromArgb(176, 125, 79);
            Color barColHover = highContrast ? SystemColors.Highlight : _isDark ? Color.FromArgb(220, 165, 115) : Color.FromArgb(150, 100, 55);
            Color avgLineCol = highContrast ? SystemColors.ControlText : _isDark ? Color.FromArgb(100, 160, 210) : Color.FromArgb(65, 130, 185);

            // Draw horizontal grid lines & Y-axis scale (RTL: Y-axis scale on the right)
            int gridStepCount = 4;
            double scaleMax = _maxMs * 1.15; // 15% head room for values on top of bars
            if (scaleMax <= 0) scaleMax = 60000;

            using (var gridPen = new Pen(gridLineCol, 1) { DashStyle = DashStyle.Dash })
            using (var textBrush = new SolidBrush(textCol))
            {
                for (int i = 0; i <= gridStepCount; i++)
                {
                    float y = baselineY - (float)((chartHeight / gridStepCount) * i);
                    g.DrawLine(gridPen, leftPad, y, chartRight, y);

                    long stepMs = (long)((scaleMax / gridStepCount) * i);
                    string yLabel = FormatDuration(stepMs);
                    var yRect = new RectangleF(chartRight + 6, y - 8, rightPad - 8, 16);
                    g.DrawString(yLabel, _cachedFontSmall!, textBrush, yRect, _sfY);
                }
            }

            // Draw Bars (RTL: Today on the right, preceding days flowing to the left)
            int count = _items.Count;
            float totalSlotWidth = chartWidth / count;
            float barWidth = Math.Max(6f, Math.Min(totalSlotWidth * 0.65f, 42f));

            var todayDate = DateTime.Today;

            for (int i = 0; i < count; i++)
            {
                var item = _items[i];
                float slotLeft = leftPad + (i * totalSlotWidth);
                float slotCenterX = slotLeft + (totalSlotWidth / 2f);
                float barLeft = slotCenterX - (barWidth / 2f);

                float barHeight = (float)((item.Milliseconds / scaleMax) * chartHeight);
                if (barHeight < 3f && item.Milliseconds > 0) barHeight = 3f;

                float barTop = baselineY - barHeight;
                var barRect = new RectangleF(barLeft, barTop, barWidth, barHeight);
                item.BarRect = barRect;
                item.ColumnHitArea = new RectangleF(slotLeft, topPad, totalSlotWidth, chartHeight + bottomPad);

                bool isHovered = (i == _hoveredIndex);
                bool isToday = _isMonthlyMode ? item.IsCurrentPeriod : (item.Date.Date == todayDate);

                Color currentBarColor = isHovered
                    ? barColHover
                    : (isToday ? barColToday : barColNormal);

                if (barHeight > 0)
                {
                    using var path = CreateRoundedTopBarPath(barRect, Math.Min(4f, barWidth / 2f));
                    using var fillBrush = new SolidBrush(currentBarColor);
                    g.FillPath(fillBrush, path);

                    if (isHovered || isToday)
                    {
                        using var outlinePen = new Pen(Color.FromArgb(50, highContrast ? SystemColors.Window : Color.White), 1);
                        g.DrawPath(outlinePen, path);
                    }
                }
                else
                {
                    // Draw subtle 2px baseline pill for 0 usage days
                    var zeroRect = new RectangleF(barLeft, baselineY - 2, barWidth, 2);
                    using var zeroBrush = new SolidBrush(highContrast ? SystemColors.ControlDark : _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 215, 210));
                    g.FillRectangle(zeroBrush, zeroRect);
                }

                var currentTopFont = isToday ? _cachedFontBold! : _cachedFontRegular!;
                var dayFont = isToday ? _cachedFontBold! : _cachedFontRegular!;
                var dateFont = isToday ? _cachedDateFontBold! : _cachedDateFontRegular!;

                // Bar top value string (draw only if slot is wide enough or bar is hovered)
                if (item.Milliseconds > 0 && (totalSlotWidth >= 28 || isHovered))
                {
                    using var topBrush = new SolidBrush(isHovered ? currentBarColor : textCol);
                    g.DrawString(item.FormattedDuration, currentTopFont, topBrush, slotCenterX, barTop - 2, _sfTop);
                }

                // X-Axis Labels (adaptive based on column count)
                using var lblBrush = new SolidBrush(isToday ? currentBarColor : textCol);

                if (_isMonthlyMode)
                {
                    g.DrawString(item.DayName, dayFont, lblBrush, slotCenterX, baselineY + 4, _sfCenter);
                    g.DrawString(item.DayNumber, dateFont, lblBrush, slotCenterX, baselineY + 18, _sfCenter);
                }
                else if (count <= 7)
                {
                    g.DrawString(item.DayName, dayFont, lblBrush, slotCenterX, baselineY + 4, _sfCenter);
                    g.DrawString(item.DateLabel, dateFont, lblBrush, slotCenterX, baselineY + 18, _sfCenter);
                }
                else if (count <= 14)
                {
                    g.DrawString(item.DayName, dayFont, lblBrush, slotCenterX, baselineY + 4, _sfCenter);
                    g.DrawString(item.DayNumber, dateFont, lblBrush, slotCenterX, baselineY + 18, _sfCenter);
                }
                else
                {
                    // In 30 days view: show day number on every 4th day, 1st day, today, or hovered
                    bool showLabel = (i % 4 == 0) || (i == count - 1) || isToday || isHovered;
                    if (showLabel)
                    {
                        g.DrawString(item.DayNumber, dayFont, lblBrush, slotCenterX, baselineY + 6, _sfCenter);
                    }
                    else
                    {
                        using var tickPen = new Pen(gridLineCol, 1);
                        g.DrawLine(tickPen, slotCenterX, baselineY + 2, slotCenterX, baselineY + 6);
                    }
                }
            }

            // Draw Average Line & Label if > 0 (Drawn on top of bars so bars do not cover it)
            if (_averageMs > 0 && _averageMs <= scaleMax)
            {
                float avgY = baselineY - (float)((_averageMs / scaleMax) * chartHeight);
                using (var avgPen = new Pen(avgLineCol, 1.5F) { DashStyle = DashStyle.DashDot })
                {
                    g.DrawLine(avgPen, leftPad, avgY, chartRight, avgY);
                }

                string avgFormat = _isMonthlyMode
                    ? (TextManager.GetString("MonthlyUsageAverage") + ": {0}")
                    : TextManager.GetString("DailyUsageAvgLine");

                string avgText = string.Format(
                    avgFormat,
                    FormatDuration((long)_averageMs));

                var avgSize = g.MeasureString(avgText, _cachedAvgFont!);
                float avgX = chartRight - avgSize.Width - 8f;
                if (avgX < leftPad + 5) avgX = leftPad + 5;
                float avgTextY = avgY - avgSize.Height - 3f;
                if (avgTextY < topPad + 2f) avgTextY = avgY + 3f;

                // Pill badge behind text so text is completely legible even when overlapping a bar
                var badgeRect = new RectangleF(avgX - 4f, avgTextY - 1f, avgSize.Width + 8f, avgSize.Height + 2f);
                Color badgeBg = _isDark ? Color.FromArgb(235, 36, 34, 32) : Color.FromArgb(240, 255, 255, 255);
                Color badgeBorder = _isDark ? Color.FromArgb(60, avgLineCol) : Color.FromArgb(90, avgLineCol);
                using var badgeBgBrush = new SolidBrush(badgeBg);
                using var badgeBorderPen = new Pen(badgeBorder, 1);
                using var badgePath = CreateRoundedRectanglePath(badgeRect, 3f);
                g.FillPath(badgeBgBrush, badgePath);
                g.DrawPath(badgeBorderPen, badgePath);

                using var avgBrush = new SolidBrush(avgLineCol);
                g.DrawString(avgText, _cachedAvgFont!, avgBrush, avgX, avgTextY);
            }

            // Draw Hover Tooltip if bar hovered
            if (_hoveredIndex >= 0 && _hoveredIndex < _items.Count)
            {
                DrawHoverCard(g, _items[_hoveredIndex], chartWidth, chartHeight, topPad, leftPad);
            }
        }

        private void DrawHoverCard(Graphics g, DailyChartBarData item, float chartWidth, float chartHeight, float topPad, float leftPad)
        {
            string dateStr = item.FullDateString;
            string durationStr = FormatDetailedDuration(item.Milliseconds);
            string pctStr = _totalPeriodMs > 0
                ? $"{DailyUsageChartDataBuilder.ToPersianDigits(item.PercentageOfTotal.ToString("0.#"))}%"
                : $"{DailyUsageChartDataBuilder.ToPersianDigits("0")}%";

            var titleSize = g.MeasureString(dateStr, _cachedHoverTitleFont!);
            var valSize = g.MeasureString($"{durationStr} ({pctStr})", _cachedHoverValFont!);

            float cardW = Math.Max(titleSize.Width, valSize.Width) + 24f;
            float cardH = titleSize.Height + valSize.Height + 14f;

            float cardX = item.BarRect.X + (item.BarRect.Width / 2f) - (cardW / 2f);
            float cardY = item.BarRect.Y - cardH - 10f;

            if (cardY < topPad)
            {
                cardY = item.BarRect.Y + item.BarRect.Height + 10f;
            }
            if (cardX < leftPad) cardX = leftPad + 4;
            if (cardX + cardW > Width - 10) cardX = Width - 10 - cardW;

            var cardRect = new RectangleF(cardX, cardY, cardW, cardH);

            // Card background & shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                var shadowRect = new RectangleF(cardX + 2, cardY + 2, cardW, cardH);
                using var shadowPath = CreateRoundedRectanglePath(shadowRect, 6);
                g.FillPath(shadowBrush, shadowPath);
            }

            bool highContrast = SystemAccessibility.IsHighContrast;
            Color cardBg = highContrast ? SystemColors.Window : _isDark ? Color.FromArgb(50, 48, 46) : Color.White;
            Color cardBorder = highContrast ? SystemColors.ControlDark : _isDark ? Color.FromArgb(90, 80, 70) : Color.FromArgb(200, 180, 160);

            using (var cardPath = CreateRoundedRectanglePath(cardRect, 6))
            using (var cardBrush = new SolidBrush(cardBg))
            using (var borderPen = new Pen(cardBorder, 1))
            {
                g.FillPath(cardBrush, cardPath);
                g.DrawPath(borderPen, cardPath);
            }

            // Card text
            using var titleBrush = new SolidBrush(highContrast ? SystemColors.WindowText : _isDark ? Color.White : Color.FromArgb(40, 40, 40));
            using var valBrush = new SolidBrush(highContrast ? SystemColors.WindowText : _isDark ? Color.FromArgb(215, 170, 130) : Color.FromArgb(160, 110, 60));

            float textX = cardRect.Right - 10;
            g.DrawString(dateStr, _cachedHoverTitleFont!, titleBrush, textX, cardRect.Top + (titleSize.Height / 2) + 5, _sfHover);
            g.DrawString($"{durationStr}  -  {pctStr}", _cachedHoverValFont!, valBrush, textX, cardRect.Bottom - (valSize.Height / 2) - 5, _sfHover);
        }

        private static GraphicsPath CreateRoundedTopBarPath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0.1f)
            {
                path.AddRectangle(rect);
                return path;
            }

            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + radius);
            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom);
            path.CloseFigure();
            return path;
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
    }
}

