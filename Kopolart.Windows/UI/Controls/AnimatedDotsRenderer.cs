using Kopolart.Windows.UI.Constants;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    /// <summary>
    /// Shared animated dots renderer used by both LoaderView and NavigationLoadingOverlay.
    /// Handles the 3-dot blinking animation with DPI-aware rendering.
    /// </summary>
    public class AnimatedDotsRenderer
    {
        private int _activeDot;
        private int _tickCounter;

        private readonly int _dotDiameter;
        private readonly int _dotSpacing;

        private static readonly Color[] LightDotColors =
        [
            UIConstants.DotColor1,
            UIConstants.DotColor2,
            UIConstants.DotColor3
        ];

        private static readonly Color[] DarkDotColors =
        [
            UIConstants.DarkMode.DotColor1,
            UIConstants.DarkMode.DotColor2,
            UIConstants.DarkMode.DotColor3
        ];

        private Color[] _activeDotColors = LightDotColors;

        /// <summary>
        /// Creates a renderer with the specified dot size.
        /// </summary>
        /// <param name="dotDiameter">Logical (unscaled) dot diameter.</param>
        /// <param name="dotSpacing">Logical (unscaled) spacing between dots.</param>
        public AnimatedDotsRenderer(int dotDiameter = 16, int dotSpacing = 14)
        {
            _dotDiameter = dotDiameter;
            _dotSpacing = dotSpacing;
        }

        /// <summary>
        /// Advances the active dot. Call this on each animation tick.
        /// Returns true if the dot index changed (i.e. a repaint is needed).
        /// </summary>
        /// <param name="advanceEveryNTicks">How many ticks before advancing the dot (e.g. 4 for ~200ms at 50ms interval).</param>
        public bool Tick(int advanceEveryNTicks = 1)
        {
            _tickCounter++;
            if (_tickCounter % advanceEveryNTicks == 0)
            {
                _activeDot = (_activeDot + 1) % 3;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the animation state.
        /// </summary>
        public void Reset()
        {
            _activeDot = 0;
            _tickCounter = 0;
        }

        /// <summary>
        /// Switches between light and dark dot color palettes.
        /// </summary>
        public void SetDarkMode(bool isDark)
        {
            _activeDotColors = isDark ? DarkDotColors : LightDotColors;
        }

        /// <summary>
        /// Returns the DPI-scaled panel size needed to contain the dots.
        /// </summary>
        public Size GetPanelSize(Control hostControl)
        {
            int scaledDotDiameter = hostControl.LogicalToDeviceUnits(_dotDiameter);
            int scaledDotSpacing = hostControl.LogicalToDeviceUnits(_dotSpacing);
            return new Size(scaledDotDiameter * 3 + scaledDotSpacing * 2 + 4, scaledDotDiameter + 4);
        }

        /// <summary>
        /// Paints the three dots within the given area using the host control for DPI scaling.
        /// </summary>
        public void Paint(Graphics g, Control hostControl, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int scaledDotDiameter = hostControl.LogicalToDeviceUnits(_dotDiameter);
            int scaledDotSpacing = hostControl.LogicalToDeviceUnits(_dotSpacing);

            int totalWidth = scaledDotDiameter * 3 + scaledDotSpacing * 2;
            int startX = bounds.X + (bounds.Width - totalWidth) / 2;
            int startY = bounds.Y + (bounds.Height - scaledDotDiameter) / 2;

            for (int i = 0; i < 3; i++)
            {
                int x = startX + i * (scaledDotDiameter + scaledDotSpacing);
                var baseColor = _activeDotColors[i];

                if (i == _activeDot)
                {
                    // Active: full-size, fully opaque
                    using var brush = new SolidBrush(baseColor);
                    g.FillEllipse(brush, x, startY, scaledDotDiameter, scaledDotDiameter);
                }
                else
                {
                    // Inactive: smaller + dimmed
                    int shrink = hostControl.LogicalToDeviceUnits(3);
                    var dimmed = Color.FromArgb(90, baseColor.R, baseColor.G, baseColor.B);
                    using var brush = new SolidBrush(dimmed);
                    g.FillEllipse(brush,
                        x + shrink, startY + shrink,
                        scaledDotDiameter - shrink * 2, scaledDotDiameter - shrink * 2);
                }
            }
        }

        /// <summary>
        /// Computes a logo size that fits within the given max dimensions while preserving aspect ratio.
        /// </summary>
        public static Size FitLogoSize(Image? image, int maxWidth, int maxHeight)
        {
            if (image == null || image.Height <= 0)
                return new Size(maxWidth, maxHeight);

            double aspect = (double)image.Width / image.Height;

            int targetWidth = maxWidth;
            int targetHeight = maxHeight;

            if (aspect > 0.01)
            {
                int heightFromWidth = (int)Math.Round(targetWidth / aspect);
                if (heightFromWidth <= targetHeight)
                {
                    targetHeight = heightFromWidth;
                }
                else
                {
                    targetWidth = (int)Math.Round(targetHeight * aspect);
                }
            }

            return new Size(targetWidth, targetHeight);
        }
    }
}

