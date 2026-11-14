using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class CustomMessageBox : ThemedDialog
    {
        private readonly string _text;
        private readonly string _caption;
        private readonly MessageBoxButtons _buttons;
        private readonly MessageBoxIcon _icon;

        private Label lblMessage = null!;
        private Panel pnlButtons = null!;
        private Panel pnlIcon = null!;

        private CustomMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, bool isDark)
            : base(isDark)
        {
            _text = text;
            _caption = caption;
            _buttons = buttons;
            _icon = icon;

            InitializeComponent();
            InitializeTheme();
            FontManager.ApplyCustomFont(this);
        }



        private void InitializeComponent()
        {
            SuspendLayout();

            // Set RTL layout for Persian
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            // Form properties
            Text = _caption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            // Setup Fonts using the actual custom font family to ensure precise text measurement
            var messageFont = FontManager.CreateFont(10F, FontStyle.Regular);
            var buttonFont = FontManager.CreateFont(9.5F, FontStyle.Bold);

            // Determine Icon visibility
            bool hasIcon = _icon != MessageBoxIcon.None;

            // Calculate sizing dynamically
            int horizontalPadding = 40;
            int iconAreaWidth = hasIcon ? 60 : 0;
            int maxTextWidth = 450;
            int minFormWidth = 380;

            Size textSize = TextRenderer.MeasureText(_text, messageFont, new Size(maxTextWidth, 0), TextFormatFlags.WordBreak | TextFormatFlags.RightToLeft);

            int calculatedWidth = textSize.Width + iconAreaWidth + horizontalPadding + 30;
            int formWidth = Math.Max(minFormWidth, calculatedWidth);
            formWidth = Math.Min(formWidth, 600); // cap maximum width

            // Add extra height buffer (24px) for Persian font descenders
            int textHeight = textSize.Height + 24; 
            int iconHeight = hasIcon ? 40 : 0;
            int contentHeight = Math.Max(textHeight, iconHeight) + 40; // text padding
            int buttonAreaHeight = 60;
            int formHeight = contentHeight + buttonAreaHeight;

            ClientSize = new Size(formWidth, formHeight);

            // Add Icon Panel on the right in RTL Cartesian coordinates
            if (hasIcon)
            {
                pnlIcon = new Panel
                {
                    Size = new Size(40, 40),
                    Location = new Point(formWidth - 20 - 40, 20),
                    BackColor = Color.Transparent
                };
                pnlIcon.Paint += PnlIcon_Paint;
                Controls.Add(pnlIcon);
            }

            // Message Label positioning
            int labelX = 20;
            int labelWidth = formWidth - (hasIcon ? 85 : 40);

            lblMessage = new Label
            {
                Text = _text,
                Font = messageFont,
                Location = new Point(labelX, 22),
                Size = new Size(labelWidth, textHeight),
                BackColor = Color.Transparent,
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.TopLeft
            };
            Controls.Add(lblMessage);

            // Bottom Buttons Panel
            pnlButtons = new Panel
            {
                Size = new Size(formWidth, buttonAreaHeight),
                Location = new Point(0, formHeight - buttonAreaHeight),
                BackColor = Color.Transparent
            };
            Controls.Add(pnlButtons);

            // Add buttons dynamically based on MessageBoxButtons enum
            CreateButtons(buttonFont);

            ResumeLayout(false);
        }

        private void CreateButtons(Font buttonFont)
        {
            var layoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(15, 10, 15, 10),
                RightToLeft = RightToLeft.No
            };

            layoutPanel.ControlAdded += (s, e) => {
                if (e.Control is Button btn)
                {
                    btn.Margin = new Padding(6, 0, 6, 0);
                }
            };

            pnlButtons.Controls.Add(layoutPanel);

            // Persian button text
            string okText = TextManager.GetString("OK");
            if (string.IsNullOrEmpty(okText) || okText == "OK") okText = "تایید";

            string cancelText = TextManager.GetString("Cancel");
            if (string.IsNullOrEmpty(cancelText)) cancelText = "لغو";

            string yesText = TextManager.GetString("Yes");
            if (string.IsNullOrEmpty(yesText)) yesText = "بله";

            string noText = TextManager.GetString("No");
            if (string.IsNullOrEmpty(noText)) noText = "خیر";

            switch (_buttons)
            {
                case MessageBoxButtons.OK:
                    var btnOk = CreateButton(okText, DialogResult.OK, buttonFont, isPrimary: true);
                    layoutPanel.Controls.Add(btnOk);
                    AcceptButton = btnOk;
                    CancelButton = btnOk;
                    break;

                case MessageBoxButtons.OKCancel:
                    var btnOk2 = CreateButton(okText, DialogResult.OK, buttonFont, isPrimary: true);
                    var btnCancel = CreateButton(cancelText, DialogResult.Cancel, buttonFont, isPrimary: false);
                    layoutPanel.Controls.Add(btnOk2);
                    layoutPanel.Controls.Add(btnCancel);
                    AcceptButton = btnOk2;
                    CancelButton = btnCancel;
                    break;

                case MessageBoxButtons.YesNo:
                    var btnYes = CreateButton(yesText, DialogResult.Yes, buttonFont, isPrimary: true);
                    var btnNo = CreateButton(noText, DialogResult.No, buttonFont, isPrimary: false);
                    layoutPanel.Controls.Add(btnYes);
                    layoutPanel.Controls.Add(btnNo);
                    AcceptButton = btnYes;
                    CancelButton = btnNo;
                    break;

                case MessageBoxButtons.YesNoCancel:
                    var btnYes2 = CreateButton(yesText, DialogResult.Yes, buttonFont, isPrimary: true);
                    var btnNo2 = CreateButton(noText, DialogResult.No, buttonFont, isPrimary: false);
                    var btnCancel2 = CreateButton(cancelText, DialogResult.Cancel, buttonFont, isPrimary: false);
                    layoutPanel.Controls.Add(btnYes2);
                    layoutPanel.Controls.Add(btnNo2);
                    layoutPanel.Controls.Add(btnCancel2);
                    AcceptButton = btnYes2;
                    CancelButton = btnCancel2;
                    break;
            }
        }

        private Button CreateButton(string text, DialogResult result, Font font, bool isPrimary)
        {
            var btn = new Button
            {
                Text = text,
                DialogResult = result,
                Font = font,
                Size = new Size(88, 32),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lblMessage.ForeColor = Color.FromArgb(249, 246, 240);
                pnlButtons.BackColor = Color.FromArgb(36, 34, 32);

                foreach (Control ctrl in pnlButtons.Controls)
                {
                    if (ctrl is FlowLayoutPanel layoutPanel)
                    {
                        foreach (Control subCtrl in layoutPanel.Controls)
                        {
                            if (subCtrl is Button btn)
                            {
                                bool isPrimary = btn.DialogResult == DialogResult.OK || btn.DialogResult == DialogResult.Yes;
                                if (isPrimary)
                                {
                                    btn.BackColor = UIConstants.DarkMode.ThemeColor;
                                    btn.ForeColor = Color.White;
                                }
                                else
                                {
                                    btn.BackColor = Color.FromArgb(65, 65, 65);
                                    btn.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                BackColor = Color.White;
                lblMessage.ForeColor = Color.FromArgb(32, 28, 24);
                pnlButtons.BackColor = Color.FromArgb(248, 248, 248);

                foreach (Control ctrl in pnlButtons.Controls)
                {
                    if (ctrl is FlowLayoutPanel layoutPanel)
                    {
                        foreach (Control subCtrl in layoutPanel.Controls)
                        {
                            if (subCtrl is Button btn)
                            {
                                bool isPrimary = btn.DialogResult == DialogResult.OK || btn.DialogResult == DialogResult.Yes;
                                if (isPrimary)
                                {
                                    btn.BackColor = UIConstants.ThemeColor;
                                    btn.ForeColor = Color.FromArgb(74, 59, 42);
                                }
                                else
                                {
                                    btn.BackColor = Color.FromArgb(225, 225, 225);
                                    btn.ForeColor = Color.FromArgb(85, 85, 85);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PnlIcon_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(2, 2, 36, 36);

            switch (_icon)
            {
                case MessageBoxIcon.Error:
                    // Soft Red Circle
                    using (var brush = new SolidBrush(Color.FromArgb(231, 76, 60)))
                    {
                        g.FillEllipse(brush, rect);
                    }
                    // Clean White X
                    using (var pen = new Pen(Color.White, 3f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        float offset = 11f;
                        g.DrawLine(pen, rect.Left + offset, rect.Top + offset, rect.Right - offset, rect.Bottom - offset);
                        g.DrawLine(pen, rect.Right - offset, rect.Top + offset, rect.Left + offset, rect.Bottom - offset);
                    }
                    break;

                case MessageBoxIcon.Warning:
                    // Amber Triangle with rounded corners style
                    using (var brush = new SolidBrush(Color.FromArgb(241, 196, 15)))
                    {
                        PointF[] points = {
                            new PointF(rect.Left + rect.Width / 2f, rect.Top + 2),
                            new PointF(rect.Left + 2, rect.Bottom - 2),
                            new PointF(rect.Right - 2, rect.Bottom - 2)
                        };
                        g.FillPolygon(brush, points);
                    }
                    // Exclamation Mark
                    using (var pen = new Pen(Color.FromArgb(40, 40, 40), 3f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        float centerX = rect.Left + rect.Width / 2f;
                        g.DrawLine(pen, centerX, rect.Top + 11, centerX, rect.Top + 22);
                    }
                    using (var brush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                    {
                        float centerX = rect.Left + rect.Width / 2f;
                        g.FillEllipse(brush, centerX - 1.75f, rect.Bottom - 10, 3.5f, 3.5f);
                    }
                    break;

                case MessageBoxIcon.Information:
                    // Soft Blue Circle
                    using (var brush = new SolidBrush(Color.FromArgb(52, 152, 219)))
                    {
                        g.FillEllipse(brush, rect);
                    }
                    // "i" info glyph
                    float infoX = rect.Left + rect.Width / 2f;
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(brush, infoX - 1.75f, rect.Top + 9, 3.5f, 3.5f);
                    }
                    using (var pen = new Pen(Color.White, 3f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawLine(pen, infoX, rect.Top + 15, infoX, rect.Bottom - 10);
                    }
                    break;

                case MessageBoxIcon.Question:
                    // Soft Teal Circle
                    using (var brush = new SolidBrush(Color.FromArgb(26, 188, 156)))
                    {
                        g.FillEllipse(brush, rect);
                    }
                    // Clean Question Mark String centered
                    using (var font = new Font("Segoe UI", 16F, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.White))
                    {
                        string q = "?";
                        SizeF size = g.MeasureString(q, font);
                        g.DrawString(q, font, brush, rect.Left + (rect.Width - size.Width) / 2f + 1f, rect.Top + (rect.Height - size.Height) / 2f);
                    }
                    break;
            }
        }

        #region Public Show Methods (Static API)

        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, bool isDark)
        {
            using (var dialog = new CustomMessageBox(text, caption, buttons, icon, isDark))
            {
                if (owner != null)
                {
                    return dialog.ShowDialog(owner);
                }
                else
                {
                    return dialog.ShowDialog();
                }
            }
        }

        #endregion
    }
}


