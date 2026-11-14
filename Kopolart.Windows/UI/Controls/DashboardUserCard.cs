using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    /// <summary>
    /// Interactive user account card on the Dashboard:
    /// - When logged in: shows a consistent dark-red border, welcoming "{Name} عزیز خوش آمدی!", clicking navigates to the user profile.
    /// - When guest: divided into two interactive halves: Login (Right) and Register (Left).
    /// - Cycles every 5 seconds to show today's Persian date in format "امروز دوشنبه ۲۲ فروردین".
    /// </summary>
    public class DashboardUserCard : Control
    {
        private UserInfo? _user;
        private bool _isDark;

        private bool _isCardHovered;
        private bool _isLoginHovered;
        private bool _isRegisterHovered;

        private Timer? _rotationTimer;
        private bool _isDateModeActive;
        private string _persianTodayDate = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UserInfo? User => _user;

        public bool IsLoggedIn => _user != null && _user.UserId > 0 && !string.IsNullOrWhiteSpace(_user.Name);

        public event EventHandler? UserProfileRequested;
        public event EventHandler? LoginRequested;
        public event EventHandler? RegisterRequested;

        public DashboardUserCard()
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

            _persianTodayDate = PersianDateHelper.GetPersianTodayDateString();

            _rotationTimer = new Timer { Interval = 5000 };
            _rotationTimer.Tick += OnRotationTimerTick;
            _rotationTimer.Start();
        }

        private void OnRotationTimerTick(object? sender, EventArgs e)
        {
            _persianTodayDate = PersianDateHelper.GetPersianTodayDateString();
            _isDateModeActive = !_isDateModeActive;
            _isCardHovered = false;
            _isLoginHovered = false;
            _isRegisterHovered = false;
            Invalidate();
        }

        public void SetUserInfo(UserInfo? user)
        {
            _user = user;
            _isDateModeActive = false;
            _isCardHovered = false;
            _isLoginHovered = false;
            _isRegisterHovered = false;
            Invalidate();
        }

        public void ApplyTheme(bool isDark)
        {
            _isDark = isDark;
            BackColor = isDark ? UIConstants.DarkMode.BrandBackground : UIConstants.BrandBackground;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = Cursors.Hand;

            if (_isDateModeActive || IsLoggedIn)
            {
                if (!_isCardHovered)
                {
                    _isCardHovered = true;
                    Invalidate();
                }
            }
            else
            {
                bool isRight = e.X >= Width / 2;
                if (isRight != _isLoginHovered || (!isRight) != _isRegisterHovered)
                {
                    _isLoginHovered = isRight;
                    _isRegisterHovered = !isRight;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isCardHovered = false;
            _isLoginHovered = false;
            _isRegisterHovered = false;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            if (_isDateModeActive)
            {
                if (IsLoggedIn)
                {
                    UserProfileRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    LoginRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (IsLoggedIn)
            {
                UserProfileRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var mousePos = PointToClient(Cursor.Position);
                if (mousePos.X >= Width / 2)
                {
                    LoginRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    RegisterRequested?.Invoke(this, EventArgs.Empty);
                }
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

            float borderThickness = IsLoggedIn ? 1.8F : 1.0F;
            float halfBorder = borderThickness / 2f;
            var bounds = new RectangleF(halfBorder, halfBorder, Width - borderThickness, Height - borderThickness);
            using var path = CreateRoundedRectangle(bounds, 12f);

            Color baseCardBg = _isDark ? UIConstants.DarkMode.CardBackgroundDark : UIConstants.CardBackgroundLight;
            Color hoverCardBg = _isDark ? UIConstants.DarkMode.CardBackgroundHoverDark : UIConstants.CardBackgroundHoverLight;
            Color standardBorder = _isDark ? UIConstants.DarkMode.CardBorderDark : UIConstants.CardBorderLight;

            if (_isDateModeActive)
            {
                // 1. Fill card background with hover highlight if hovered
                Color currentBg = _isCardHovered ? hoverCardBg : baseCardBg;
                using (var bgBrush = new SolidBrush(currentBg))
                {
                    g.FillPath(bgBrush, path);
                }

                // 2. Stroke with border (red if logged in, standard border if guest)
                Color borderColor = IsLoggedIn
                    ? (_isDark ? UIConstants.DarkRedDark : UIConstants.DarkRed)
                    : standardBorder;
                using (var borderPen = new Pen(borderColor, borderThickness))
                {
                    g.DrawPath(borderPen, path);
                }

                // 3. Draw Centered Persian Date
                Color accentColor = IsLoggedIn
                    ? (_isDark ? UIConstants.DarkRedDark : UIConstants.DarkRed)
                    : (_isDark ? Color.FromArgb(242, 169, 59) : UIConstants.BrandPrimary);
                DrawDateContent(g, accentColor);
            }
            else if (IsLoggedIn)
            {
                // 1. Fill card background with hover highlight if hovered
                Color currentBg = _isCardHovered ? hoverCardBg : baseCardBg;
                using (var bgBrush = new SolidBrush(currentBg))
                {
                    g.FillPath(bgBrush, path);
                }

                // 2. Stroke with consistent red color (equal width on all sides)
                Color redBorder = _isDark ? UIConstants.DarkRedDark : UIConstants.DarkRed;
                using (var borderPen = new Pen(redBorder, borderThickness))
                {
                    g.DrawPath(borderPen, path);
                }

                // 3. Draw Centered User Greeting
                DrawLoggedInContent(g, redBorder);
            }
            else
            {
                // 1. Base card background
                using (var bgBrush = new SolidBrush(baseCardBg))
                {
                    g.FillPath(bgBrush, path);
                }

                int midX = Width / 2;

                // 2. Draw hover highlights for halves if active
                if (_isLoginHovered || _isRegisterHovered)
                {
                    var oldClip = g.Clip;
                    using var pathRegion = new Region(path);
                    g.Clip = pathRegion;

                    using var halfHoverBrush = new SolidBrush(hoverCardBg);
                    if (_isLoginHovered)
                    {
                        g.FillRectangle(halfHoverBrush, midX, 0, Width - midX, Height);
                    }
                    else if (_isRegisterHovered)
                    {
                        g.FillRectangle(halfHoverBrush, 0, 0, midX, Height);
                    }

                    g.Clip = oldClip;
                }

                // 3. Standard card border (equal width on all sides)
                using (var borderPen = new Pen(standardBorder, borderThickness))
                {
                    g.DrawPath(borderPen, path);
                }

                // 4. Center vertical divider
                using (var divPen = new Pen(standardBorder, 1F))
                {
                    g.DrawLine(divPen, (float)midX, 14f, (float)midX, Height - 14f);
                }

                // 5. Draw Guest Halves (Right: Login, Left: Register in RTL)
                DrawGuestContent(g, midX);
            }
        }

        private void DrawLoggedInContent(Graphics g, Color accentColor)
        {
            string rawName = _user?.Name?.Trim() ?? string.Empty;
            string displayName = rawName.Length > 20 ? (rawName.Substring(0, 18) + "...") : rawName;
            string greeting = string.Format(AppConstants.Strings.UserWelcomeGreeting, displayName);

            Color titleColor = _isDark ? Color.FromArgb(249, 246, 240) : Color.FromArgb(32, 28, 24);

            using var titleFont = FontManager.CreateBlackFont(11.5F);
            var textSize = g.MeasureString(greeting, titleFont);

            int iconBoxSize = 42;
            int gap = 12;
            float totalW = iconBoxSize + gap + textSize.Width;

            float startX = (Width - totalW) / 2;
            // In Persian (RTL), icon appears to the right of the greeting text
            float iconX = startX + textSize.Width + gap;
            float iconY = (Height - iconBoxSize) / 2;

            var iconRect = new Rectangle((int)iconX, (int)iconY, iconBoxSize, iconBoxSize);

            // Icon background badge
            Color iconBoxBg = _isDark
                ? Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)
                : Color.FromArgb(22, accentColor.R, accentColor.G, accentColor.B);

            using (var iconBgBrush = new SolidBrush(iconBoxBg))
            using (var iconBgPath = CreateRoundedRectangle(iconRect, 10))
            {
                g.FillPath(iconBgBrush, iconBgPath);
            }

            // Draw Icon
            var iconFont = FontManager.GetIconFont(18F);
            using (var iconBrush = new SolidBrush(accentColor))
            {
                var iconSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(KopolArtFaIcons.User, iconFont, iconBrush, iconRect, iconSf);
            }

            // Draw Greeting Text
            var textRect = new RectangleF(startX, 0, textSize.Width + 8, Height);
            using (var textBrush = new SolidBrush(titleColor))
            {
                var textSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                };
                g.DrawString(greeting, titleFont, textBrush, textRect, textSf);
            }
        }

        private void DrawDateContent(Graphics g, Color accentColor)
        {
            if (string.IsNullOrWhiteSpace(_persianTodayDate))
            {
                _persianTodayDate = PersianDateHelper.GetPersianTodayDateString();
            }

            Color titleColor = _isDark ? Color.FromArgb(249, 246, 240) : Color.FromArgb(32, 28, 24);

            using var titleFont = FontManager.CreateBlackFont(11.5F);
            var textSize = g.MeasureString(_persianTodayDate, titleFont);

            int iconBoxSize = 42;
            int gap = 12;
            float totalW = iconBoxSize + gap + textSize.Width;

            float startX = (Width - totalW) / 2;
            // In Persian (RTL), icon appears to the right of the date text
            float iconX = startX + textSize.Width + gap;
            float iconY = (Height - iconBoxSize) / 2;

            var iconRect = new Rectangle((int)iconX, (int)iconY, iconBoxSize, iconBoxSize);

            // Icon background badge
            Color iconBoxBg = _isDark
                ? Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)
                : Color.FromArgb(22, accentColor.R, accentColor.G, accentColor.B);

            using (var iconBgBrush = new SolidBrush(iconBoxBg))
            using (var iconBgPath = CreateRoundedRectangle(iconRect, 10))
            {
                g.FillPath(iconBgBrush, iconBgPath);
            }

            // Draw Icon (Calendar)
            var iconFont = FontManager.GetIconFont(18F);
            using (var iconBrush = new SolidBrush(accentColor))
            {
                var iconSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(KopolArtFaIcons.Calendar, iconFont, iconBrush, iconRect, iconSf);
            }

            // Draw Date Text
            var textRect = new RectangleF(startX, 0, textSize.Width + 8, Height);
            using (var textBrush = new SolidBrush(titleColor))
            {
                var textSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                };
                g.DrawString(_persianTodayDate, titleFont, textBrush, textRect, textSf);
            }
        }

        private void DrawGuestContent(Graphics g, int midX)
        {
            Color titleColor = _isDark ? Color.FromArgb(249, 246, 240) : Color.FromArgb(32, 28, 24);
            Color subColor = _isDark ? Color.FromArgb(189, 181, 170) : Color.FromArgb(105, 95, 88);
            Color loginColor = _isDark ? Color.FromArgb(242, 169, 59) : UIConstants.BrandPrimary;
            Color registerColor = _isDark ? UIConstants.BrandSlateBlueDark : UIConstants.BrandSlateBlue;

            var titleFont = FontManager.GetBlackFont(11F);
            var subFont = FontManager.GetFont(8.5F, FontStyle.Regular);

            // 1. Right Half: Login (ورود)
            DrawHalfAction(
                g,
                startX: midX,
                halfWidth: Width - midX,
                iconGlyph: KopolArtFaIcons.Login,
                iconColor: loginColor,
                title: AppConstants.Strings.UserLoginText,
                subtitle: AppConstants.Strings.UserLoginDesc,
                titleFont: titleFont,
                subFont: subFont,
                titleColor: titleColor,
                subColor: subColor);

            // 2. Left Half: Register (ثبت نام)
            DrawHalfAction(
                g,
                startX: 0,
                halfWidth: midX,
                iconGlyph: KopolArtFaIcons.Edit,
                iconColor: registerColor,
                title: AppConstants.Strings.UserRegisterText,
                subtitle: AppConstants.Strings.UserRegisterDesc,
                titleFont: titleFont,
                subFont: subFont,
                titleColor: titleColor,
                subColor: subColor);
        }

        private void DrawHalfAction(
            Graphics g,
            int startX,
            int halfWidth,
            string iconGlyph,
            Color iconColor,
            string title,
            string subtitle,
            Font titleFont,
            Font subFont,
            Color titleColor,
            Color subColor)
        {
            int iconBoxSize = 42;
            int iconY = (Height - iconBoxSize) / 2;
            int iconX = startX + halfWidth - iconBoxSize - 20; // Near the right side of this half (RTL)
            var iconRect = new Rectangle(iconX, iconY, iconBoxSize, iconBoxSize);

            // Subtle background container for icon
            Color iconBoxBg = _isDark
                ? Color.FromArgb(28, iconColor.R, iconColor.G, iconColor.B)
                : Color.FromArgb(18, iconColor.R, iconColor.G, iconColor.B);

            using (var iconBgBrush = new SolidBrush(iconBoxBg))
            using (var iconBgPath = CreateRoundedRectangle(iconRect, 10))
            {
                g.FillPath(iconBgBrush, iconBgPath);
            }

            // Draw Icon
            var iconFont = FontManager.GetIconFont(17F);
            using (var iconBrush = new SolidBrush(iconColor))
            {
                var iconSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(iconGlyph, iconFont, iconBrush, iconRect, iconSf);
            }

            // Text coordinates: to the left of the icon (RTL)
            int textRight = iconX - 14;
            int textLeft = startX + 16;
            int textWidth = Math.Max(20, textRight - textLeft);

            int titleH = 40;
            int subH = 22;
            int gap = 2;
            int totalH = titleH + gap + subH;
            int titleY = (Height - totalH) / 2;
            int subY = titleY + titleH + gap;

            using (var titleBrush = new SolidBrush(titleColor))
            using (var subBrush = new SolidBrush(subColor))
            {
                var textSf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                };

                var titleRect = new Rectangle(textLeft, titleY, textWidth, titleH);
                g.DrawString(title, titleFont, titleBrush, titleRect, textSf);

                var subRect = new Rectangle(textLeft, subY, textWidth, subH);
                g.DrawString(subtitle, subFont, subBrush, subRect, textSf);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_rotationTimer != null)
                {
                    _rotationTimer.Stop();
                    _rotationTimer.Tick -= OnRotationTimerTick;
                    _rotationTimer.Dispose();
                    _rotationTimer = null;
                }
            }
            base.Dispose(disposing);
        }

        private static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2f;
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
