using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class MilestoneAlert : ThemedDialog
    {
        private readonly string _message;

        private Label lblTitle = null!;
        private Label lblMessage = null!;
        private Button btnOk = null!;

        public MilestoneAlert(bool isDark, string message, string titleKey = "MilestoneTitle")
            : base(isDark)
        {
            _message = message;

            InitializeComponent();

            if (titleKey != "MilestoneTitle")
            {
                string customTitle = TextManager.GetString(titleKey);
                Text = customTitle;
                lblTitle.Text = customTitle;
                lblTitle.Location = new Point(ClientSize.Width - 20 - lblTitle.PreferredWidth, 20);
            }

            FontManager.ApplyCustomFont(this);
            InitializeTheme();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblMessage = new Label();
            btnOk = new Button();

            SuspendLayout();

            // RTL Configuration
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            // Form properties
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(420, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("MilestoneTitle");

            // Title label
            lblTitle.Text = TextManager.GetString("MilestoneTitle");
            lblTitle.Font = FontManager.CreateBlackFont(14F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(420 - 20 - lblTitle.PreferredWidth, 20);

            // Message label
            lblMessage.Text = _message;
            lblMessage.Location = new Point(20, 60);
            lblMessage.Size = new Size(380, 75);
            lblMessage.Font = FontManager.CreateFont(10.5F, FontStyle.Regular);
            lblMessage.RightToLeft = RightToLeft.Yes;
            lblMessage.TextAlign = ContentAlignment.TopLeft;

            // OK button
            btnOk.Text = TextManager.GetString("MilestoneThanks");
            btnOk.Size = new Size(110, 34);
            btnOk.Location = new Point(155, 145);
            btnOk.Cursor = Cursors.Hand;
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            btnOk.Click += BtnOk_Click;

            // Add controls
            Controls.Add(lblTitle);
            Controls.Add(lblMessage);
            Controls.Add(btnOk);

            AcceptButton = btnOk;

            ResumeLayout(false);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.ThemeColor;
                lblMessage.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnOk.BackColor = UIConstants.DarkMode.ThemeColor;
                btnOk.ForeColor = Color.White;
            }
            else
            {
                BackColor = Color.White;
                lblTitle.ForeColor = Color.FromArgb(74, 59, 42); // Theme text color
                lblMessage.ForeColor = Color.FromArgb(60, 60, 60);
                btnOk.BackColor = UIConstants.ThemeColor;
                btnOk.ForeColor = Color.FromArgb(74, 59, 42);
            }
        }
    }
}


