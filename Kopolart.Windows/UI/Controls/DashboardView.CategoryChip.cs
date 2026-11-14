using Kopolart.Windows.Domain.Helpers;
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

        public sealed class DashboardCategoryChip : Control
        {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Title { get; set; } = string.Empty;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Url { get; set; } = string.Empty;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string CategoryKey { get; set; } = string.Empty;

            private int _badgeCount;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int BadgeCount
            {
                get => _badgeCount;
                set
                {
                    if (_badgeCount != value)
                    {
                        _badgeCount = value;
                        Invalidate();
                    }
                }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string IconGlyph { get; set; } = string.Empty;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color AccentColorLight { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color AccentColorDark { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color BadgeBgLight { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color BadgeBgDark { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color BorderLight { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color BorderDark { get; set; }

            public event EventHandler<string>? ChipClicked;

            private bool _isHovered;
            private bool _isDark;

            public DashboardCategoryChip()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);

                DoubleBuffered = true;
                Cursor = Cursors.Hand;
                Size = new Size(160, 52);
            }

            public void ApplyTheme(bool isDark)
            {
                _isDark = isDark;
                BackColor = isDark ? UIConstants.DarkMode.BrandBackground : UIConstants.BrandBackground;
                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _isHovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _isHovered = false;
                Invalidate();
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                if (!string.IsNullOrWhiteSpace(Url))
                {
                    ChipClicked?.Invoke(this, Url);
                }
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

                float borderThickness = _isHovered ? 1.5f : 1.0f;
                float halfBorder = borderThickness / 2f;
                var bounds = new RectangleF(halfBorder, halfBorder, Width - borderThickness, Height - borderThickness);
                float cornerRadius = 12f;

                using var path = CreateRoundedRectangle(bounds, cornerRadius);

                Color accentColor = _isDark ? AccentColorDark : AccentColorLight;
                Color badgeBg = _isDark ? BadgeBgDark : BadgeBgLight;
                Color chipBorder = _isHovered
                    ? accentColor
                    : (_isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight);

                Color chipBg = _isDark
                    ? (_isHovered ? UIConstants.DarkMode.CardBackgroundHoverDark : UIConstants.DarkMode.CardBackgroundDark)
                    : (_isHovered ? UIConstants.CardBackgroundHoverLight : UIConstants.CardBackgroundLight);

                // Fill chip background
                using (var bgBrush = new SolidBrush(chipBg))
                {
                    g.FillPath(bgBrush, path);
                }

                // Draw border
                using (var borderPen = new Pen(chipBorder, borderThickness))
                {
                    g.DrawPath(borderPen, path);
                }

                // Layout icon badge + text
                int badgeSize = 32;
                int badgeCornerRadius = 8;
                int badgeY = (Height - badgeSize) / 2;

                var textFont = FontManager.GetFont(10.5f, FontStyle.Bold);
                var textSize = TextRenderer.MeasureText(Title, textFont);

                int contentGap = 8;
                int totalContentW = badgeSize + contentGap + textSize.Width;
                int startX = Math.Max(6, (Width - totalContentW) / 2);

                int badgeX;
                int textX;
                bool isRtl = RightToLeft == RightToLeft.Yes;

                if (isRtl)
                {
                    // RTL: badge on the right, text on the left
                    badgeX = startX + textSize.Width + contentGap;
                    textX = startX;
                }
                else
                {
                    // LTR: badge on left, text on right
                    badgeX = startX;
                    textX = startX + badgeSize + contentGap;
                }

                // Draw badge background
                var badgeRect = new RectangleF(badgeX, badgeY, badgeSize, badgeSize);
                using var badgePath = CreateRoundedRectangle(badgeRect, badgeCornerRadius);
                using (var badgeBrush = new SolidBrush(badgeBg))
                {
                    g.FillPath(badgeBrush, badgePath);
                }

                Color badgeBorderColor = _isDark ? BorderDark : BorderLight;
                using (var badgePen = new Pen(badgeBorderColor, 1.0f))
                {
                    g.DrawPath(badgePen, badgePath);
                }

                // Draw icon glyph inside badge
                if (!string.IsNullOrEmpty(IconGlyph))
                {
                    var iconFont = FontManager.GetIconFont(13.5f);
                    using var iconBrush = new SolidBrush(accentColor);
                    using var iconSf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(IconGlyph, iconFont, iconBrush, badgeRect, iconSf);
                }

                // Draw notification count badge over the icon if > 0 (tools has 0 / no badge)
                if (BadgeCount > 0)
                {
                    string badgeText = PersianNumberHelper.ToPersianDigits(BadgeCount);
                    var countFont = FontManager.GetFont(7.5f, FontStyle.Bold);
                    var countSize = TextRenderer.MeasureText(badgeText, countFont);
                    int pillH = 18;
                    int pillW = Math.Max(pillH, countSize.Width + 6);

                    // Position overlapping top corner of icon badge
                    float pillX = isRtl
                        ? (badgeRect.Right - (pillW * 0.6f))
                        : (badgeRect.Left - (pillW * 0.4f));
                    float pillY = badgeRect.Top - 5;

                    var pillRect = new RectangleF(pillX, pillY, pillW, pillH);
                    using var pillPath = CreateRoundedRectangle(pillRect, pillH / 2f);

                    // Background: consistent StatusRed (#C62828)
                    Color statusRed = Color.FromArgb(198, 40, 40);
                    using (var countBgBrush = new SolidBrush(statusRed))
                    {
                        g.FillPath(countBgBrush, pillPath);
                    }

                    // Border outline matching chipBg so badge cleanly separates
                    using (var countPen = new Pen(chipBg, 1.2f))
                    {
                        g.DrawPath(countPen, pillPath);
                    }

                    // Bold white Persian number, vertically centered with breathing room
                    using (var countTextBrush = new SolidBrush(Color.White))
                    {
                        using var countSf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.NoWrap
                        };
                        // Expand layout box by 10px so GDI+ font cell is never clamped by small bounds, with +2.5f vertical offset to balance Persian font baseline
                        var textLayoutRect = new RectangleF(pillX - 10, pillY - 10 + 2.5f, pillW + 20, pillH + 20);
                        g.DrawString(badgeText, countFont, countTextBrush, textLayoutRect, countSf);
                    }
                }

                // Draw title
                Color textColor = _isDark ? Color.FromArgb(249, 246, 240) : Color.FromArgb(32, 28, 24);
                var textRect = new Rectangle(textX, (Height - textSize.Height) / 2, textSize.Width, textSize.Height);
                using (var textBrush = new SolidBrush(textColor))
                {
                    using var textSf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    g.DrawString(Title, textFont, textBrush, textRect, textSf);
                }
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
