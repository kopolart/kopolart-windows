using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    public class CategoryBadgeLabel : Label
    {
        public ContentCategory Category { get; private set; } = ContentCategory.Other;
        private bool _isDark;
        private Color _borderColor = Color.Transparent;

        public CategoryBadgeLabel()
        {
            AutoSize = true;
            Padding = new Padding(10, 4, 10, 4);
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            TextAlign = ContentAlignment.MiddleCenter;
            DoubleBuffered = true;
            Cursor = Cursors.Default;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var baseSize = base.GetPreferredSize(proposedSize);
            return new Size(baseSize.Width + 14, Math.Max(baseSize.Height, 22));
        }

        public void SetCategory(ContentCategory cat, bool isDark)
        {
            Category = cat;
            _isDark = isDark;
            Text = GetCategoryDisplayName(cat);
            UpdateColors();
        }

        public void SetCategory(DownloadCategory cat, bool isDark)
        {
            SetCategory((ContentCategory)cat, isDark);
        }

        public void SetCategory(string categoryName, bool isDark)
        {
            SetCategory(ContentCategoryHelper.ParseCategory(categoryName), isDark);
        }

        public void UpdateTheme(bool isDark)
        {
            _isDark = isDark;
            UpdateColors();
        }

        private void UpdateColors()
        {
            var (bg, border, fg) = GetCategoryColors(Category, _isDark);
            BackColor = bg;
            ForeColor = fg;
            _borderColor = border;
            Invalidate();
        }

        public static string GetCategoryDisplayName(ContentCategory cat)
        {
            string key = cat switch
            {
                ContentCategory.Wallpaper => "BadgeWallpaper",
                ContentCategory.Post => "BadgePost",
                ContentCategory.Color => "BadgeColor",
                ContentCategory.Tools => "BadgeTools",
                _ => "BadgeOther"
            };
            return TextManager.GetString(key);
        }

        public static string GetCategoryDisplayName(DownloadCategory cat) => GetCategoryDisplayName((ContentCategory)cat);

        public static string GetCategoryDisplayName(string categoryName) => GetCategoryDisplayName(ContentCategoryHelper.ParseCategory(categoryName));

        public static (Color bg, Color border, Color fg) GetCategoryColors(ContentCategory cat, bool isDark)
        {
            if (isDark)
            {
                return cat switch
                {
                    ContentCategory.Wallpaper => (Color.FromArgb(48, 25, 65), Color.FromArgb(120, 60, 160), Color.FromArgb(192, 132, 252)), // Purple (#C084FC)
                    ContentCategory.Post => (Color.FromArgb(20, 45, 75), Color.FromArgb(50, 100, 165), Color.FromArgb(96, 165, 250)),      // Blue (#60A5FA)
                    ContentCategory.Color => (Color.FromArgb(60, 45, 18), Color.FromArgb(145, 95, 35), Color.FromArgb(251, 191, 36)),      // Orange/Amber (#FBBF24)
                    ContentCategory.Tools => (Color.FromArgb(18, 50, 36), Color.FromArgb(40, 115, 80), Color.FromArgb(52, 211, 153)),      // Emerald Green (#34D399)
                    _ => (Color.FromArgb(40, 45, 55), Color.FromArgb(80, 95, 115), Color.FromArgb(148, 163, 184))                         // Slate (#94A3B8)
                };
            }
            else
            {
                return cat switch
                {
                    ContentCategory.Wallpaper => (Color.FromArgb(250, 245, 255), Color.FromArgb(233, 213, 255), Color.FromArgb(147, 51, 234)), // Purple (#9333EA)
                    ContentCategory.Post => (Color.FromArgb(238, 246, 255), Color.FromArgb(186, 225, 255), Color.FromArgb(36, 156, 240)),     // Site Blue (#249CF0)
                    ContentCategory.Color => (Color.FromArgb(255, 248, 238), Color.FromArgb(254, 215, 170), Color.FromArgb(217, 119, 6)),      // Site Orange (#D97706)
                    ContentCategory.Tools => (Color.FromArgb(236, 253, 245), Color.FromArgb(167, 243, 208), Color.FromArgb(5, 150, 105)),      // Emerald Green (#059669)
                    _ => (Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(83, 94, 123))                          // Slate (#535E7B)
                };
            }
        }

        public static (Color bg, Color border, Color fg) GetCategoryColors(DownloadCategory cat, bool isDark)
        {
            return GetCategoryColors((ContentCategory)cat, isDark);
        }

        public static (Color bg, Color border, Color fg) GetCategoryColors(string categoryName, bool isDark)
        {
            return GetCategoryColors(ContentCategoryHelper.ParseCategory(categoryName), isDark);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var brush = new SolidBrush(BackColor))
            {
                using var path = GetRoundedRectangle(rect, 4);
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(_borderColor, 1);
                e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(bounds.Location, size);

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

