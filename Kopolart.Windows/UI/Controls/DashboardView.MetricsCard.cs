using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    public partial class DashboardView : UserControl
    {

        private class DashboardMetricsCard : Control
        {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string DurationValue { get; private set; } = "--";

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string VisitedPagesValue { get; private set; } = "--";

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string OnlineUsersValue { get; private set; } = "--";

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string DataValue { get; private set; } = "--";

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool IsOnlineLive { get; private set; }

            private string _durationTitle = TextManager.GetString("DashboardUsageDuration");
            private string _pagesTitle = TextManager.GetString("DashboardVisitedPages");
            private string _onlineTitle = TextManager.GetString("DashboardOnlineUsers");
            private string _dataTitle = TextManager.GetString("DashboardInternetData");
            private bool _isDark;
            private readonly Timer? _blinkTimer;
            private bool _isBlinkFaded;

            public DashboardMetricsCard()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);

                DoubleBuffered = true;
                BackColor = UIConstants.DarkMode.BrandBackground;

                _blinkTimer = new Timer { Interval = 850 };
                _blinkTimer.Tick += (s, e) =>
                {
                    if (IsOnlineLive && Visible && !Disposing && !IsDisposed)
                    {
                        _isBlinkFaded = !_isBlinkFaded;
                        Invalidate();
                    }
                };
                _blinkTimer.Start();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _blinkTimer?.Stop();
                    _blinkTimer?.Dispose();
                }
                base.Dispose(disposing);
            }

            public void RefreshTexts()
            {
                _durationTitle = TextManager.GetString("DashboardUsageDuration");
                _pagesTitle = TextManager.GetString("DashboardVisitedPages");
                _onlineTitle = TextManager.GetString("DashboardOnlineUsers");
                _dataTitle = TextManager.GetString("DashboardInternetData");
                Invalidate();
            }

            public void ApplyTheme(bool isDark)
            {
                _isDark = isDark;
                BackColor = isDark ? UIConstants.DarkMode.BrandBackground : UIConstants.BrandBackground;
                Invalidate();
            }

            public void SetMetrics(string duration, string pages, string data)
            {
                DurationValue = duration;
                VisitedPagesValue = pages;
                DataValue = data;
                Invalidate();
            }

            public void UpdateOnlineUsers(string online, bool isLive)
            {
                OnlineUsersValue = online;
                IsOnlineLive = isLive;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                Color parentBg = _isDark ? UIConstants.DarkMode.BrandBackground : UIConstants.BrandBackground;
                g.Clear(parentBg);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                float borderThickness = 1.0F;
                float halfBorder = borderThickness / 2f;
                var bounds = new RectangleF(halfBorder, halfBorder, Width - borderThickness, Height - borderThickness);
                using var path = CreateRoundedRectangle(bounds, 12f);

                Color cardBg = _isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight;
                Color cardBorder = _isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight;

                using (var bgBrush = new SolidBrush(cardBg))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(cardBorder, borderThickness))
                {
                    g.DrawPath(borderPen, path);
                }

                int fourthWidth = Width / 4;

                // 3 Dividers
                Color dividerColor = _isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight;
                using (var dividerPen = new Pen(dividerColor, 1F))
                {
                    g.DrawLine(dividerPen, fourthWidth, 14, fourthWidth, Height - 14);
                    g.DrawLine(dividerPen, fourthWidth * 2, 14, fourthWidth * 2, Height - 14);
                    g.DrawLine(dividerPen, fourthWidth * 3, 14, fourthWidth * 3, Height - 14);
                }

                var col0Rect = new Rectangle(12, 10, fourthWidth - 24, Height - 20);
                var col1Rect = new Rectangle(fourthWidth + 12, 10, fourthWidth - 24, Height - 20);
                var col2Rect = new Rectangle((fourthWidth * 2) + 12, 10, fourthWidth - 24, Height - 20);
                var col3Rect = new Rectangle((fourthWidth * 3) + 12, 10, Width - (fourthWidth * 3) - 24, Height - 20);

                // Persian is RTL:
                // Col 3 (Rightmost): Duration
                // Col 2: Visited Pages
                // Col 1: Online Users (Blinking Green)
                // Col 0 (Leftmost): Internet Data
                var durationRect = col3Rect;
                var pagesRect = col2Rect;
                var onlineRect = col1Rect;
                var dataRect = col0Rect;

                Color mutedColor = _isDark ? Color.FromArgb(189, 181, 170) : Color.FromArgb(105, 95, 88);
                Color primaryColor = _isDark ? Color.FromArgb(242, 169, 59) : UIConstants.BrandPrimary;
                Color pagesColor = _isDark ? Color.FromArgb(255, 179, 71) : Color.FromArgb(180, 90, 40);
                Color secondaryColor = _isDark ? UIConstants.BrandSlateBlueDark : UIConstants.BrandSlateBlue;

                Color liveGreen = _isDark ? UIConstants.LiveGreenDark : UIConstants.LiveGreenLight;
                Color fadedGreen = _isDark ? UIConstants.LiveGreenFadedDark : UIConstants.LiveGreenFadedLight;
                Color onlineColor = IsOnlineLive
                    ? (_isBlinkFaded ? fadedGreen : liveGreen)
                    : mutedColor;

                using var labelFont = FontManager.CreateFont(9F, FontStyle.Regular);
                using var mutedBrush = new SolidBrush(mutedColor);

                var labelFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near
                };

                // Draw Duration
                var dLabelRect = new Rectangle(durationRect.X, durationRect.Y + 4, durationRect.Width, 22);
                var dValRect = new Rectangle(durationRect.X, durationRect.Y + 28, durationRect.Width, 48);
                g.DrawString(_durationTitle, labelFont, mutedBrush, dLabelRect, labelFormat);
                DrawMetricValue(g, DurationValue, dValRect, primaryColor);

                // Draw Visited Pages
                var pLabelRect = new Rectangle(pagesRect.X, pagesRect.Y + 4, pagesRect.Width, 22);
                var pValRect = new Rectangle(pagesRect.X, pagesRect.Y + 28, pagesRect.Width, 48);
                g.DrawString(_pagesTitle, labelFont, mutedBrush, pLabelRect, labelFormat);
                DrawMetricValue(g, VisitedPagesValue, pValRect, pagesColor);

                // Draw Online Users (Live Green & Blinking)
                var oLabelRect = new Rectangle(onlineRect.X, onlineRect.Y + 4, onlineRect.Width, 22);
                var oValRect = new Rectangle(onlineRect.X, onlineRect.Y + 28, onlineRect.Width, 48);
                g.DrawString(_onlineTitle, labelFont, mutedBrush, oLabelRect, labelFormat);
                DrawMetricValue(g, OnlineUsersValue, oValRect, onlineColor);

                // Draw Data
                var bLabelRect = new Rectangle(dataRect.X, dataRect.Y + 4, dataRect.Width, 22);
                var bValRect = new Rectangle(dataRect.X, dataRect.Y + 28, dataRect.Width, 48);
                g.DrawString(_dataTitle, labelFont, mutedBrush, bLabelRect, labelFormat);
                DrawMetricValue(g, DataValue, bValRect, secondaryColor);
            }

            private static void DrawMetricValue(Graphics g, string text, Rectangle rect, Color color)
            {
                float fontSize = 12F;
                using var baseFont = FontManager.CreateBlackFont(fontSize);
                var size = TextRenderer.MeasureText(text, baseFont);

                float effectiveSize = fontSize;
                if (size.Width > rect.Width - 8 && rect.Width > 20)
                {
                    effectiveSize = Math.Max(8.5F, fontSize * ((float)(rect.Width - 8) / size.Width));
                }

                using var font = FontManager.CreateBlackFont(effectiveSize);
                using var brush = new SolidBrush(color);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                };

                g.DrawString(text, font, brush, rect, sf);
            }

            private static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
            {
                var path = new GraphicsPath();
                float diameter = radius * 2;
                var size = new SizeF(diameter, diameter);
                var arc = new RectangleF(bounds.Location, size);

                path.AddArc(arc, 180, 90);
                arc.X = bounds.Right - diameter;
                path.AddArc(arc, 270, 90);
                arc.Y = bounds.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                arc.X = bounds.Left;
                path.AddArc(arc, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

    }
}
