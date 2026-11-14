using Kopolart.Windows.Services;
using Kopolart.Windows.UI.Constants;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    /// <summary>
    /// Lightweight semi-transparent overlay with animated blinking dots.
    /// Shown during in-app page navigation.
    /// </summary>
    public class NavigationLoadingOverlay : UserControl
    {
        private Timer? _animTimer;
        private Timer? _safetyTimer;
        private bool _hiding;
        private bool _isDarkMode;
        private PictureBox _logo;

        private readonly AnimatedDotsRenderer _dotsRenderer = new(dotDiameter: 14, dotSpacing: 12);

        public NavigationLoadingOverlay()
        {
            SetStyle(
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);

            BackColor = Color.FromArgb(180,
                UIConstants.ThemeColor.R,
                UIConstants.ThemeColor.G,
                UIConstants.ThemeColor.B);

            Visible = false;
            Dock = DockStyle.Fill;

            // Logo (compact, centered)
            _logo = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Visible = false
            };
            Controls.Add(_logo);
            Resize += (s, ev) => LayoutOverlay();

            _animTimer = new Timer { Interval = 180 };
            _animTimer.Tick += OnAnimTick;

            _safetyTimer = new Timer { Interval = 8000 };
            _safetyTimer.Tick += (s, e) => HideOverlay();
        }

        private IAssetService? _assetService;

        public void SetAssetService(IAssetService assetService)
        {
            _assetService = assetService;
            UpdateThemeLogo();
        }

        private void UpdateThemeLogo()
        {
            if (_assetService != null)
            {
                try
                {
                    var logo = _assetService.LoadLogo(_isDarkMode);
                    if (logo != null)
                    {
                        SetLogo((Image)logo.Clone());
                    }
                }
                catch
                {
                    // Ignore asset loading error in UI
                }
            }
        }

        public void SetLogo(Image logo)
        {
            if (_logo.Image != null)
            {
                var old = _logo.Image;
                _logo.Image = logo;
                old.Dispose();
            }
            else
            {
                _logo.Image = logo;
            }
            _logo.Visible = true;
            LayoutOverlay();
        }

        /// <summary>
        /// Switches the overlay between dark and light theme.
        /// </summary>
        public void ApplyDarkMode(bool isDark)
        {
            _isDarkMode = isDark;
            _dotsRenderer.SetDarkMode(isDark);
            UpdateThemeLogo();

            if (isDark)
            {
                BackColor = UIConstants.DarkMode.OverlayBackColor;
            }
            else
            {
                BackColor = Color.FromArgb(180,
                    UIConstants.ThemeColor.R,
                    UIConstants.ThemeColor.G,
                    UIConstants.ThemeColor.B);
            }
        }

        private void LayoutOverlay()
        {
            if (Width <= 0 || Height <= 0) return;

            int maxLogoSize = LogicalToDeviceUnits(190);
            int spacing = LogicalToDeviceUnits(16);
            var dotsSize = _dotsRenderer.GetPanelSize(this);
            int dotsHeight = dotsSize.Height;

            // Compute logo size preserving aspect ratio
            int logoW = maxLogoSize, logoH = maxLogoSize;
            if (_logo.Image is { Height: > 0 })
            {
                var fitted = AnimatedDotsRenderer.FitLogoSize(_logo.Image, maxLogoSize, maxLogoSize);
                logoW = fitted.Width;
                logoH = fitted.Height;
            }

            int totalHeight = (_logo.Visible ? logoH + spacing : 0) + dotsHeight;
            int topY = (Height - totalHeight) / 2;

            if (_logo.Visible)
            {
                _logo.Size = new Size(logoW, logoH);
                _logo.Location = new Point((Width - logoW) / 2, topY);
            }
        }

        private void OnAnimTick(object? sender, EventArgs e)
        {
            _dotsRenderer.Tick();
            // Repaint only the dots strip — invalidating the whole content-area-sized
            // overlay every 180ms wasted paint work on a per-pixel-alpha surface.
            Invalidate(GetDotsBounds());
        }

        /// <summary>
        /// The strip occupied by the animated dots (below the logo, or centered without one).
        /// </summary>
        private Rectangle GetDotsBounds()
        {
            int spacing = LogicalToDeviceUnits(16);
            var dotsSize = _dotsRenderer.GetPanelSize(this);
            int dotsBlockH = dotsSize.Height;
            int logoBottom = _logo.Visible ? _logo.Bottom + spacing : (Height - dotsBlockH) / 2;

            return new Rectangle(0, logoBottom, Width, dotsBlockH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Visible) return;

            _dotsRenderer.Paint(e.Graphics, this, GetDotsBounds());
        }

        public void ShowOverlay()
        {
            if (_hiding) return;

            _dotsRenderer.Reset();
            Visible = true;
            BringToFront();
            _animTimer?.Start();
            _safetyTimer?.Start();
        }

        public void HideOverlay()
        {
            _hiding = true;
            _animTimer?.Stop();
            _safetyTimer?.Stop();
            Visible = false;
            _hiding = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _safetyTimer?.Stop();
                _safetyTimer?.Dispose();

                // Dispose owned logo image clone
                if (_logo.Image != null)
                {
                    _logo.Image.Dispose();
                    _logo.Image = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}

