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

        private class DashboardActionCard : Control
        {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Title { get; set; } = string.Empty;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Subtitle { get; set; } = string.Empty;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string? BadgeText { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color BadgeColor { get; set; } = UIConstants.BrandSlateBlue;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Color? BadgeTextColor { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string IconGlyph { get; set; } = string.Empty;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool IsHero { get; set; }

            public event EventHandler? CardClicked;

            private bool _isHovered;
            private bool _isDark;

            public DashboardActionCard()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);

                DoubleBuffered = true;
                BackColor = UIConstants.DarkMode.BrandBackground;
                Cursor = Cursors.Hand;
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
                CardClicked?.Invoke(this, EventArgs.Empty);
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

                float borderThickness = _isHovered ? 1.5F : 1.0F;
                float halfBorder = borderThickness / 2f;
                var bounds = new RectangleF(halfBorder, halfBorder, Width - borderThickness, Height - borderThickness);
                float cornerRadius = IsHero ? 16f : 12f;

                using var path = CreateRoundedRectangle(bounds, cornerRadius);

                Color cardBg;
                Color cardBorder;
                Color titleColor;
                Color subtitleColor;
                Color iconColor;

                bool highContrast = SystemAccessibility.IsHighContrast;
                if (highContrast)
                {
                    cardBg = SystemColors.Window;
                    cardBorder = SystemColors.ControlDark;
                    titleColor = SystemColors.WindowText;
                    subtitleColor = SystemColors.WindowText;
                    iconColor = SystemColors.WindowText;
                }
                else if (IsHero)
                {
                    cardBg = _isHovered
                        ? Color.FromArgb(158, 99, 40)
                        : UIConstants.BrandPrimary;
                    cardBorder = cardBg;
                    titleColor = Color.White;
                    subtitleColor = Color.FromArgb(242, 230, 216);
                    iconColor = Color.White;
                }
                else
                {
                    cardBg = _isDark
                        ? (_isHovered ? UIConstants.DarkMode.CardBackgroundHoverDark : UIConstants.DarkMode.CardBackgroundDark)
                        : (_isHovered ? UIConstants.CardBackgroundHoverLight : UIConstants.CardBackgroundLight);

                    cardBorder = _isDark
                        ? (_isHovered ? Color.FromArgb(242, 169, 59) : UIConstants.DarkMode.CardBorderDark)
                        : (_isHovered ? UIConstants.BrandPrimary : UIConstants.CardBorderLight);

                    titleColor = _isDark
                        ? Color.FromArgb(249, 246, 240)
                        : Color.FromArgb(32, 28, 24);

                    subtitleColor = _isDark
                        ? Color.FromArgb(189, 181, 170)
                        : Color.FromArgb(105, 95, 88);

                    iconColor = _isDark
                        ? Color.FromArgb(242, 169, 59)
                        : UIConstants.BrandPrimary;
                }

                // Fill card background
                using (var bgBrush = new SolidBrush(cardBg))
                {
                    g.FillPath(bgBrush, path);
                }

                // Draw border
                using (var borderPen = new Pen(cardBorder, borderThickness))
                {
                    g.DrawPath(borderPen, path);
                }

                bool isRtl = RightToLeft == RightToLeft.Yes;

                // 1. Badge metrics (calculate first if present so text width adjusts accordingly)
                Rectangle badgeRect = Rectangle.Empty;
                if (!string.IsNullOrEmpty(BadgeText))
                {
                    var badgeMeasureFont = FontManager.GetFont(8.5F, FontStyle.Bold);
                    int badgeWidth = Math.Max(52, TextRenderer.MeasureText(BadgeText, badgeMeasureFont).Width + 18);
                    int badgeHeight = 24;
                    int badgeX = isRtl ? 16 : (Width - badgeWidth - 16);
                    int badgeY = (Height - badgeHeight) / 2;
                    badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);

                    Color effectiveBadgeColor = highContrast ? SystemColors.Control : BadgeColor;
                    if (_isDark && !highContrast && (effectiveBadgeColor == UIConstants.BrandSlateBlue || (effectiveBadgeColor.R == 83 && effectiveBadgeColor.G == 94 && effectiveBadgeColor.B == 123)))
                    {
                        effectiveBadgeColor = UIConstants.BrandSlateBlueDark;
                    }

                    using var badgePath = CreateRoundedRectangle(badgeRect, 6);
                    using var badgeBgBrush = new SolidBrush(effectiveBadgeColor);
                    g.FillPath(badgeBgBrush, badgePath);

                    Color badgeTextColor = highContrast
                        ? SystemColors.ControlText
                        : BadgeTextColor ?? (effectiveBadgeColor.GetBrightness() > 0.55f
                            ? UIConstants.TextThemeColor
                            : Color.White);

                    using var badgeTextBrush = new SolidBrush(badgeTextColor);
                    var badgeFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(BadgeText, badgeMeasureFont, badgeTextBrush, badgeRect, badgeFormat);
                }

                // 2. Icon layout & rendering
                int iconBoxSize = IsHero ? 52 : 42;
                int iconY = (Height - iconBoxSize) / 2;
                int iconX = isRtl ? (Width - iconBoxSize - 16) : 16;
                var iconRect = new Rectangle(iconX, iconY, iconBoxSize, iconBoxSize);

                if (!string.IsNullOrEmpty(IconGlyph))
                {
                    // Draw subtle rounded container behind icon
                    if (IsHero)
                    {
                        using var iconBgBrush = new SolidBrush(Color.FromArgb(_isDark ? 35 : 20, 255, 255, 255));
                        using var iconBgPath = CreateRoundedRectangle(iconRect, 12);
                        g.FillPath(iconBgBrush, iconBgPath);
                    }
                    else
                    {
                        Color iconBoxBg = _isDark
                            ? Color.FromArgb(28, iconColor.R, iconColor.G, iconColor.B)
                            : Color.FromArgb(16, iconColor.R, iconColor.G, iconColor.B);
                        using var iconBgBrush = new SolidBrush(iconBoxBg);
                        using var iconBgPath = CreateRoundedRectangle(iconRect, 10);
                        g.FillPath(iconBgBrush, iconBgPath);
                    }

                    float iconSize = IsHero ? 24F : 17F;
                    var iconFont = FontManager.GetIconFont(iconSize);
                    using var iconBrush = new SolidBrush(iconColor);
                    var iconFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(IconGlyph, iconFont, iconBrush, iconRect, iconFormat);
                }

                // 3. Text layout (Title and Subtitle)
                int textStartX;
                int textWidth;

                if (isRtl)
                {
                    int textRight = iconX - 14;
                    int textLeft = !badgeRect.IsEmpty ? (badgeRect.Right + 12) : 16;
                    textStartX = textLeft;
                    textWidth = Math.Max(20, textRight - textLeft);
                }
                else
                {
                    int textLeft = iconX + iconBoxSize + 14;
                    int textRight = !badgeRect.IsEmpty ? (badgeRect.Left - 12) : (Width - 16);
                    textStartX = textLeft;
                    textWidth = Math.Max(20, textRight - textLeft);
                }

                int titleHeight = IsHero ? 44 : 40;
                int subtitleHeight = IsHero ? 24 : 22;
                int gap = 2;
                int totalTextHeight = titleHeight + gap + subtitleHeight;
                int titleY = (Height - totalTextHeight) / 2;
                int subtitleY = titleY + titleHeight + gap;

                var titleFont = FontManager.GetBlackFont(IsHero ? 13F : 11F);
                var subtitleFont = FontManager.GetFont(IsHero ? 9.5F : 8.5F, FontStyle.Regular);

                using var titleBrush = new SolidBrush(titleColor);
                using var subtitleBrush = new SolidBrush(subtitleColor);

                var textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = (isRtl ? StringFormatFlags.DirectionRightToLeft : 0) | StringFormatFlags.NoWrap
                };

                var titleRect = new Rectangle(textStartX, titleY, textWidth, titleHeight);
                if (!string.IsNullOrWhiteSpace(Title) && titleRect is { Width: > 0, Height: > 0 })
                {
                    try
                    {
                        g.DrawString(Title, titleFont, titleBrush, titleRect, textFormat);
                    }
                    catch
                    {
                        TextRenderer.DrawText(g, Title, titleFont, titleRect, titleColor,
                            (isRtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    }
                }

                var subtitleRect = new Rectangle(textStartX, subtitleY, textWidth, subtitleHeight);
                if (!string.IsNullOrWhiteSpace(Subtitle) && subtitleRect.Width > 0 && subtitleRect.Height > 0)
                {
                    try
                    {
                        g.DrawString(Subtitle, subtitleFont, subtitleBrush, subtitleRect, textFormat);
                    }
                    catch
                    {
                        TextRenderer.DrawText(g, Subtitle, subtitleFont, subtitleRect, subtitleColor,
                            (isRtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    }
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
