using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls.NavBar
{
    /// <summary>
    /// Modern, integrated Omnibox (Address &amp; Search Bar) control for the top navigation bar.
    /// Displays the current active URL with an HTTPS security indicator, enables direct
    /// URL navigation, site searching, one-click copy, and responsive sizing.
    /// </summary>
    public class NavBarOmniboxControl : Control
    {
        private readonly TextBox _txtUrl;
        private readonly Label _lblSecurity;
        private readonly Label _btnAction;
        private readonly ToolTip _toolTip;

        private bool _isDark;
        private bool _isFocused;
        private bool _isHovered;
        private bool _isActionHovered;
        private string _currentUrl = string.Empty;
        private bool _isSecure = true;

        public event EventHandler? EditingStarted;

        public NavBarOmniboxControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;
            Cursor = Cursors.Default;
            RightToLeft = RightToLeft.No;

            _toolTip = new ToolTip
            {
                ShowAlways = true,
                InitialDelay = 400
            };

            // Security / Lock icon (Left side)
            _lblSecurity = new Label
            {
                Text = "\uE72E", // Segoe MDL2 Assets Lock icon
                Font = new Font("Segoe MDL2 Assets", 8.5F, FontStyle.Regular),
                Size = new Size(18, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Default,
                BackColor = Color.Transparent
            };
            _toolTip.SetToolTip(_lblSecurity, "اتصال امن (HTTPS)");

            // URL Display Box (ReadOnly, using Segoe UI for standard Western URL typography & balanced baseline)
            _txtUrl = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = CreateUrlFont(),
                RightToLeft = RightToLeft.No,
                TextAlign = HorizontalAlignment.Left,
                Multiline = false,
                ReadOnly = true,
                Cursor = Cursors.Default
            };
            _txtUrl.Enter += OnTextEnter;
            _txtUrl.Leave += OnTextLeave;
            _txtUrl.Click += (s, e) => _txtUrl.SelectAll();

            // Action button (Right side: Copy icon)
            _btnAction = new Label
            {
                Text = "\uE8C8", // Segoe MDL2 Assets Copy icon
                Font = new Font("Segoe MDL2 Assets", 8.5F, FontStyle.Regular),
                Size = new Size(18, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            _btnAction.MouseEnter += (s, e) => { _isActionHovered = true; UpdateActionAppearance(); };
            _btnAction.MouseLeave += (s, e) => { _isActionHovered = false; UpdateActionAppearance(); };
            _btnAction.Click += OnActionClick;
            _toolTip.SetToolTip(_btnAction, "کپی پیوند");

            Controls.Add(_lblSecurity);
            Controls.Add(_txtUrl);
            Controls.Add(_btnAction);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            Click += (s, e) => { _txtUrl.Focus(); };

            // Set size and theme only after all controls exist
            Size = new Size(280, 26);
            ApplyDarkMode(false);
            LayoutChildren();
        }

        private static Font CreateUrlFont()
        {
            try
            {
                return new Font("Segoe UI", 8.5F, FontStyle.Regular);
            }
            catch
            {
                return new Font(FontFamily.GenericSansSerif, 8.5F, FontStyle.Regular);
            }
        }

        private void LayoutChildren()
        {
            if (_lblSecurity == null || _btnAction == null || _txtUrl == null)
                return;

            int h = ClientSize.Height;
            int w = ClientSize.Width;
            if (h <= 0 || w <= 0) return;

            // Security icon on left
            _lblSecurity.Location = new Point(6, Math.Max(0, (h - _lblSecurity.Height) / 2));

            // Action button on right
            _btnAction.Location = new Point(Math.Max(0, w - _btnAction.Width - 6), Math.Max(0, (h - _btnAction.Height) / 2));

            // Text box in middle (vertically centered with 1px optical baseline correction)
            int textX = _lblSecurity.Right + 4;
            int textW = Math.Max(10, _btnAction.Left - textX - 4);
            int textH = _txtUrl.PreferredHeight;
            int textY = Math.Max(0, (h - textH) / 2-1);
            _txtUrl.SetBounds(textX, textY, textW, textH);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        private void OnTextEnter(object? sender, EventArgs e)
        {
            _isFocused = true;
            _txtUrl.SelectAll();
            Invalidate();
            EditingStarted?.Invoke(this, EventArgs.Empty);
        }

        private void OnTextLeave(object? sender, EventArgs e)
        {
            _isFocused = false;
            Invalidate();
        }

        private void UpdateActionState()
        {
            if (_btnAction == null || _toolTip == null) return;
            _btnAction.Text = "\uE8C8"; // Segoe MDL2 Assets Copy
            _toolTip.SetToolTip(_btnAction, "کپی پیوند");
            UpdateActionAppearance();
        }

        private void UpdateActionAppearance()
        {
            if (_btnAction == null) return;

            if (SystemAccessibility.IsHighContrast)
            {
                _btnAction.ForeColor = _isActionHovered ? SystemColors.Highlight : SystemColors.ControlText;
            }
            else if (_isDark)
            {
                _btnAction.ForeColor = _isActionHovered ? Color.White : Color.FromArgb(170, 170, 170);
            }
            else
            {
                _btnAction.ForeColor = _isActionHovered ? UIConstants.BrandPrimary : Color.FromArgb(110, 110, 110);
            }
        }

        private void OnActionClick(object? sender, EventArgs e)
        {
            var textToCopy = !string.IsNullOrWhiteSpace(_txtUrl.Text) ? _txtUrl.Text.Trim() : _currentUrl;
            if (!string.IsNullOrWhiteSpace(textToCopy))
            {
                try
                {
                    Clipboard.SetText(textToCopy);
                    _toolTip.Show("پیوند کپی شد ✓", _btnAction, 0, -25, 1200);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NavBarOmniboxControl] Failed to copy link to clipboard: {ex.Message}");
                }
            }
        }

        public void UpdateCurrentUrl(string? url, bool isDashboard)
        {
            if (_txtUrl == null) return;

            if (isDashboard)
            {
                _currentUrl = "https://kopolart.ir";
                _isSecure = true;
                _txtUrl.Text = "https://kopolart.ir";
                UpdateSecurityAppearance();
                UpdateActionState();
                return;
            }

            if (string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _currentUrl = url;
            _isSecure = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            _txtUrl.Text = url;
            UpdateSecurityAppearance();
            UpdateActionState();
        }

        private void UpdateSecurityAppearance()
        {
            if (_lblSecurity == null || _toolTip == null) return;

            if (_isSecure)
            {
                _lblSecurity.Text = "\uE72E"; // Lock
                _lblSecurity.ForeColor = SystemAccessibility.IsHighContrast
                    ? SystemColors.ControlText
                    : (_isDark ? UIConstants.LiveGreenDark : UIConstants.LiveGreenLight);
                _toolTip.SetToolTip(_lblSecurity, "اتصال امن (HTTPS)");
            }
            else
            {
                _lblSecurity.Text = "\uE774"; // Globe / Insecure
                _lblSecurity.ForeColor = SystemAccessibility.IsHighContrast
                    ? SystemColors.GrayText
                    : (_isDark ? Color.FromArgb(180, 180, 180) : Color.FromArgb(120, 120, 120));
                _toolTip.SetToolTip(_lblSecurity, "کپل‌آرت");
            }
        }

        public void AdjustWidth(int parentWidth)
        {
            int newWidth;
            if (parentWidth < 800)
            {
                newWidth = 190;
            }
            else if (parentWidth < 1000)
            {
                newWidth = 240;
            }
            else if (parentWidth < 1250)
            {
                newWidth = 300;
            }
            else
            {
                newWidth = 360;
            }

            if (Width != newWidth)
            {
                Width = newWidth;
                LayoutChildren();
                Invalidate();
            }
        }

        public void ApplyDarkMode(bool isDark)
        {
            _isDark = isDark;
            if (_txtUrl == null) return;

            bool highContrast = SystemAccessibility.IsHighContrast;

            Color bg = highContrast
                ? SystemColors.Window
                : (isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight);
            Color fg = highContrast
                ? SystemColors.WindowText
                : (isDark ? UIConstants.DarkMode.MenuStripForeColor : UIConstants.TextThemeColor);

            _txtUrl.BackColor = bg;
            _txtUrl.ForeColor = fg;

            UpdateSecurityAppearance();
            UpdateActionAppearance();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            using var path = CreateRoundedRectanglePath(bounds, 6);

            bool highContrast = SystemAccessibility.IsHighContrast;

            // Fill background
            Color bg = highContrast
                ? SystemColors.Window
                : (_isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight);
            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Draw border. The system accent (or the high-contrast highlight) is used for
            // keyboard focus so the ring stays visible and matches the user's selection color.
            Color borderColor;
            if (_isFocused)
            {
                if (highContrast)
                {
                    borderColor = SystemColors.Highlight;
                }
                else if (SystemAccessibility.AreKeyboardCuesVisible)
                {
                    borderColor = SystemAccessibility.GetAccent(_isDark ? UIConstants.DarkMode.BrandPrimary : UIConstants.BrandPrimary);
                }
                else
                {
                    borderColor = _isDark ? UIConstants.DarkMode.BrandPrimary : UIConstants.BrandPrimary;
                }
            }
            else if (_isHovered)
            {
                borderColor = highContrast
                    ? SystemColors.ControlText
                    : (_isDark ? Color.FromArgb(110, 105, 100) : Color.FromArgb(190, 175, 160));
            }
            else
            {
                borderColor = highContrast
                    ? SystemColors.ControlDark
                    : (_isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight);
            }

            float stroke = _isFocused
                ? Math.Max(1.5F, SystemAccessibility.FocusBorderWidth)
                : 1.0F;
            using var pen = new Pen(borderColor, stroke);
            e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
