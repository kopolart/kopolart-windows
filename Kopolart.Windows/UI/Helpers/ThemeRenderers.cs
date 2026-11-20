using System.Drawing;
using System.Windows.Forms;
using Kopolart.Windows.UI.Constants;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Static class to store the state of update notification alerts.
    /// </summary>
    public static class MenuUpdateNotifier
    {
        public static bool IsUpdateAvailable { get; set; }
        public static bool BlinkState { get; set; }
    }

    /// <summary>
    /// Custom renderer for dark-themed menu strip.
    /// </summary>
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (SystemAccessibility.IsHighContrast)
            {
                e.TextColor = !e.Item.Enabled
                    ? SystemColors.GrayText
                    : e.Item.Selected ? SystemColors.HighlightText : SystemColors.ControlText;
            }
            else if (e.Item.Name == "projectOrderToolStripMenuItem")
            {
                e.TextColor = e.Item.ForeColor;
            }
            else if (!e.Item.Enabled)
            {
                e.TextColor = UIConstants.DarkMode.TextDisabledColor;
            }
            else
            {
                e.TextColor = UIConstants.DarkMode.MenuStripForeColor;
            }
            base.OnRenderItemText(e);

            if (MenuUpdateNotifier.IsUpdateAvailable && 
                MenuUpdateNotifier.BlinkState && 
                (e.Item.Name == "helpToolStripMenuItem" || e.Item.Name == "updatesToolStripMenuItem"))
            {
                using (var brush = new SolidBrush(UIConstants.AlertDotColor))
                {
                    int circleSize = 8;
                    int circleX;
                    if (e.Item.RightToLeft == RightToLeft.Yes)
                    {
                        circleX = e.TextRectangle.Left + 2;
                    }
                    else
                    {
                        circleX = e.TextRectangle.Right - circleSize - 2;
                    }
                    int circleY = e.TextRectangle.Top + (e.TextRectangle.Height - circleSize) / 2;

                    var oldSmoothingMode = e.Graphics.SmoothingMode;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(brush, circleX, circleY, circleSize, circleSize);
                    e.Graphics.SmoothingMode = oldSmoothingMode;
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (e.Item.IsOnDropDown)
            {
                base.OnRenderSeparator(e);
                return;
            }

            var bounds = e.Item.ContentRectangle;
            int x = bounds.X + bounds.Width / 2;
            int top = bounds.Y + 4;
            int bottom = bounds.Bottom - 4;

            using var pen = new Pen(SystemAccessibility.IsHighContrast ? SystemColors.ControlDark : UIConstants.DarkMode.MenuBorder);
            e.Graphics.DrawLine(pen, x, top, x, bottom);
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        private static Color DarkBg => SystemAccessibility.IsHighContrast
            ? SystemColors.Control
            : UIConstants.DarkMode.MenuStripBackColor;

        private static Color Highlight => SystemAccessibility.IsHighContrast
            ? SystemColors.Highlight
            : UIConstants.DarkMode.MenuHighlight;

        private static Color Border => SystemAccessibility.IsHighContrast
            ? SystemColors.ControlDark
            : UIConstants.DarkMode.MenuBorder;

        private static Color ButtonPressed => SystemAccessibility.IsHighContrast
            ? SystemColors.Highlight
            : UIConstants.DarkMode.MenuButtonPressed;

        private static Color DropDownBackground => SystemAccessibility.IsHighContrast
            ? SystemColors.Window
            : UIConstants.DarkMode.CardBackgroundDark;

        public override Color MenuItemSelected => Highlight;
        public override Color MenuItemSelectedGradientBegin => Highlight;
        public override Color MenuItemSelectedGradientEnd => Highlight;
        public override Color MenuItemPressedGradientBegin => DarkBg;
        public override Color MenuItemPressedGradientEnd => DarkBg;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Border;
        public override Color ToolStripDropDownBackground => DropDownBackground;
        public override Color ImageMarginGradientBegin => DropDownBackground;
        public override Color ImageMarginGradientMiddle => DropDownBackground;
        public override Color ImageMarginGradientEnd => DropDownBackground;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => DropDownBackground;

        // Navigation buttons hover states
        public override Color ButtonSelectedGradientBegin => Highlight;
        public override Color ButtonSelectedGradientEnd => Highlight;
        public override Color ButtonSelectedHighlight => Highlight;
        public override Color ButtonSelectedBorder => Border;

        public override Color ButtonPressedGradientBegin => ButtonPressed;
        public override Color ButtonPressedGradientEnd => ButtonPressed;
        public override Color ButtonPressedHighlight => ButtonPressed;
        public override Color ButtonPressedBorder => Border;
    }

    /// <summary>
    /// Custom renderer for light-themed menu strip using Kopolart cream hover accent.
    /// </summary>
    public class LightMenuRenderer : ToolStripProfessionalRenderer
    {
        public LightMenuRenderer() : base(new LightColorTable())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (SystemAccessibility.IsHighContrast)
            {
                e.TextColor = !e.Item.Enabled
                    ? SystemColors.GrayText
                    : e.Item.Selected ? SystemColors.HighlightText : SystemColors.ControlText;
            }
            else if (e.Item.Name == "projectOrderToolStripMenuItem" || (e.Item.ForeColor != Color.Empty && e.Item.ForeColor != SystemColors.ControlText))
            {
                e.TextColor = e.Item.ForeColor;
            }
            else if (!e.Item.Enabled)
            {
                e.TextColor = UIConstants.LightMode.TextDisabledColor;
            }
            else
            {
                e.TextColor = SystemColors.ControlText;
            }
            base.OnRenderItemText(e);

            if (MenuUpdateNotifier.IsUpdateAvailable && 
                MenuUpdateNotifier.BlinkState && 
                (e.Item.Name == "helpToolStripMenuItem" || e.Item.Name == "updatesToolStripMenuItem"))
            {
                using (var brush = new SolidBrush(UIConstants.AlertDotColor))
                {
                    int circleSize = 8;
                    int circleX;
                    if (e.Item.RightToLeft == RightToLeft.Yes)
                    {
                        circleX = e.TextRectangle.Left + 2;
                    }
                    else
                    {
                        circleX = e.TextRectangle.Right - circleSize - 2;
                    }
                    int circleY = e.TextRectangle.Top + (e.TextRectangle.Height - circleSize) / 2;

                    var oldSmoothingMode = e.Graphics.SmoothingMode;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(brush, circleX, circleY, circleSize, circleSize);
                    e.Graphics.SmoothingMode = oldSmoothingMode;
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (e.Item.IsOnDropDown)
            {
                base.OnRenderSeparator(e);
                return;
            }

            var bounds = e.Item.ContentRectangle;
            int x = bounds.X + bounds.Width / 2;
            int top = bounds.Y + 4;
            int bottom = bounds.Bottom - 4;

            using var pen = new Pen(SystemAccessibility.IsHighContrast ? SystemColors.ControlDark : UIConstants.LightMode.MenuBorder);
            e.Graphics.DrawLine(pen, x, top, x, bottom);
        }
    }

    public class LightColorTable : ProfessionalColorTable
    {
        private static Color Highlight => SystemAccessibility.IsHighContrast
            ? SystemColors.Highlight
            : UIConstants.LightMode.MenuHighlight;

        private static Color LightBg => SystemAccessibility.IsHighContrast
            ? SystemColors.Control
            : UIConstants.NavBarLightBg;

        private static Color Border => SystemAccessibility.IsHighContrast
            ? SystemColors.ControlDark
            : UIConstants.LightMode.MenuBorder;

        private static Color ButtonPressed => SystemAccessibility.IsHighContrast
            ? SystemColors.Highlight
            : UIConstants.LightMode.MenuButtonPressed;

        private static Color DropDownBackground => SystemAccessibility.IsHighContrast
            ? SystemColors.Window
            : UIConstants.CardBackgroundLight;

        public override Color MenuItemSelected => Highlight;
        public override Color MenuItemSelectedGradientBegin => Highlight;
        public override Color MenuItemSelectedGradientEnd => Highlight;
        public override Color MenuItemPressedGradientBegin => LightBg;
        public override Color MenuItemPressedGradientEnd => LightBg;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Border;
        public override Color ToolStripDropDownBackground => DropDownBackground;
        public override Color ImageMarginGradientBegin => DropDownBackground;
        public override Color ImageMarginGradientMiddle => DropDownBackground;
        public override Color ImageMarginGradientEnd => DropDownBackground;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => DropDownBackground;

        // Navigation buttons hover states using Cream color
        public override Color ButtonSelectedGradientBegin => Highlight;
        public override Color ButtonSelectedGradientEnd => Highlight;
        public override Color ButtonSelectedHighlight => Highlight;
        public override Color ButtonSelectedBorder => Border;

        public override Color ButtonPressedGradientBegin => ButtonPressed;
        public override Color ButtonPressedGradientEnd => ButtonPressed;
        public override Color ButtonPressedHighlight => ButtonPressed;
        public override Color ButtonPressedBorder => Border;
    }
}
