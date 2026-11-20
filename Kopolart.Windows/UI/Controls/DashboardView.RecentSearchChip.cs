using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    public partial class DashboardView : UserControl
    {

        internal sealed class DashboardRecentSearchChip : Control
        {
            private bool _isDark;
            private bool _isHovered;
            private bool _isDeleteHovered;
            private Rectangle _deleteRect;

            public string Query { get; }

            public event EventHandler<string>? ChipClicked;
            public event EventHandler<string>? DeleteClicked;

            public DashboardRecentSearchChip(string query, bool isDark)
            {
                Query = query ?? string.Empty;
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
                Height = 28;

                var f = FontManager.GetFont(9.5f, FontStyle.Regular);
                var textSize = TextRenderer.MeasureText(Query, f);
                Width = Math.Max(74, textSize.Width + 40);

                // Explicitly initialize delete button rect in constructor
                _deleteRect = new Rectangle(5, (Height - 18) / 2, 18, 18);
            }

            public void ApplyTheme(bool isDark)
            {
                _isDark = isDark;
                Invalidate();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                _deleteRect = new Rectangle(5, (Height - 18) / 2, 18, 18);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                bool wasDeleteHovered = _isDeleteHovered;
                _isDeleteHovered = _deleteRect.Contains(e.Location);
                if (wasDeleteHovered != _isDeleteHovered)
                {
                    Invalidate();
                }
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
                _isDeleteHovered = false;
                Invalidate();
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button == MouseButtons.Left)
                {
                    if (_deleteRect.Contains(e.Location))
                    {
                        DeleteClicked?.Invoke(this, Query);
                    }
                    else
                    {
                        ChipClicked?.Invoke(this, Query);
                    }
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Color parentBg = _isDark ? UIConstants.DarkMode.BrandBackground : UIConstants.BrandBackground;
                g.Clear(parentBg);

                int w = Width;
                int h = Height;
                if (w <= 0 || h <= 0) return;

                var chipRect = new RectangleF(0.5f, 0.5f, w - 1f, h - 1f);
                using var path = CreateRoundedRectangle(chipRect, 10f);

                bool highContrast = SystemAccessibility.IsHighContrast;

                Color chipBg = highContrast
                    ? SystemColors.Window
                    : _isDark
                        ? (_isHovered ? UIConstants.DarkMode.CardBackgroundHoverDark : UIConstants.DarkMode.CardBackgroundDark)
                        : (_isHovered ? UIConstants.CardBackgroundHoverLight : UIConstants.CardBackgroundLight);

                Color chipBorder = highContrast
                    ? SystemColors.ControlDark
                    : _isHovered
                        ? (_isDark ? UIConstants.DarkMode.BrandPrimary : UIConstants.BrandPrimary)
                        : (_isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight);

                using (var bgBrush = new SolidBrush(chipBg))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(chipBorder, 1f))
                {
                    g.DrawPath(borderPen, path);
                }

                var textFont = FontManager.GetFont(9.5f, FontStyle.Regular);
                Color textCol = highContrast ? SystemColors.WindowText : _isDark ? Color.FromArgb(249, 246, 240) : Color.FromArgb(32, 28, 24);
                int textX = _deleteRect.Right + 4;
                int textW = Math.Max(10, w - textX - 8);
                var textRect = new Rectangle(textX, 0, textW, h);

                TextRenderer.DrawText(
                    g,
                    Query,
                    textFont,
                    textRect,
                    textCol,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

                if (_isDeleteHovered)
                {
                    using var delBgBrush = new SolidBrush(Color.FromArgb(40, 220, 50, 50));
                    g.FillEllipse(delBgBrush, _deleteRect);
                }

                Color delIconCol = highContrast
                    ? SystemColors.WindowText
                    : _isDeleteHovered
                        ? Color.FromArgb(230, 80, 80)
                        : (_isDark ? Color.FromArgb(189, 181, 170) : Color.FromArgb(105, 95, 88));

                var delIconFont = FontManager.GetIconFont(8.5f);
                using var delBrush = new SolidBrush(delIconCol);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                try
                {
                    g.DrawString(KopolArtFaIcons.Cancel, delIconFont, delBrush, _deleteRect, sf);
                }
                catch
                {
                    TextRenderer.DrawText(g, "×", textFont, _deleteRect, delIconCol,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }

            private static GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
            {
                var path = new GraphicsPath();
                float d = radius * 2f;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

    }
}
