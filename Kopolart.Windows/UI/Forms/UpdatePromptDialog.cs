using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class UpdatePromptDialog : ThemedDialog
    {
        private readonly string _version;
        private readonly string _releaseNotes;
        private readonly string _releaseDate;

        private Label lblTitle = null!;
        private Label lblMessage = null!;
        private Label lblNotesHeader = null!;
        private TextBox txtNotes = null!;
        private Button btnUpdate = null!;
        private Button btnCancel = null!;

        public UpdatePromptDialog(string version, string releaseNotes, bool isDark, string? releaseDate = null)
            : base(isDark)
        {
            _version = version;
            _releaseNotes = releaseNotes;
            _releaseDate = releaseDate ?? string.Empty;

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            InitializeTheme();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblMessage = new Label();
            lblNotesHeader = new Label();
            txtNotes = new TextBox();
            btnUpdate = new Button();
            btnCancel = new Button();

            SuspendLayout();

            // RTL Layout
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            // Form properties
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(420, 290);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("UpdatePromptTitle");

            // Title (Right-aligned in RTL)
            lblTitle.Text = TextManager.GetString("UpdatePromptTitle");
            lblTitle.Font = FontManager.CreateBlackFont(12F);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(420 - 20 - lblTitle.PreferredWidth, 15);

            // Message
            string verDisplay = PersianNumberHelper.ToPersianDigits(_version);
            string msg = string.Format(TextManager.GetString("UpdatePromptMessage"), verDisplay);
            if (!string.IsNullOrWhiteSpace(_releaseDate))
            {
                string pDate = PersianDateHelper.ToPersianDateString(_releaseDate);
                if (!string.IsNullOrWhiteSpace(pDate))
                {
                    msg += $" (تاریخ انتشار: {pDate})";
                }
            }
            lblMessage.Text = msg;
            lblMessage.Location = new Point(20, 45);
            lblMessage.Size = new Size(380, 42);
            lblMessage.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);

            // Release Notes Header (Right-aligned in RTL)
            lblNotesHeader.Text = TextManager.GetString("UpdateReleaseNotes");
            lblNotesHeader.Font = FontManager.CreateBlackFont(9.5F);
            lblNotesHeader.AutoSize = true;
            lblNotesHeader.Location = new Point(420 - 20 - lblNotesHeader.PreferredWidth, 95);

            // Release Notes Textbox (Scrollable and read-only)
            txtNotes.Multiline = true;
            txtNotes.ReadOnly = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;
            txtNotes.Location = new Point(20, 120);
            txtNotes.Size = new Size(380, 100);
            txtNotes.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            
            // Format newlines safely to CRLF (\r\n) for Windows Forms TextBox
            txtNotes.Text = _releaseNotes?
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\r\n", "\n")
                .Replace("\n", Environment.NewLine) ?? string.Empty;

            // Update button (Right in RTL)
            btnUpdate.Text = TextManager.GetString("UpdateYes");
            btnUpdate.DialogResult = DialogResult.Yes;
            btnUpdate.Size = new Size(110, 32);
            btnUpdate.Location = new Point(290, 240);
            btnUpdate.Cursor = Cursors.Hand;
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            // Cancel button (Left of Update)
            btnCancel.Text = TextManager.GetString("UpdateNo");
            btnCancel.DialogResult = DialogResult.No;
            btnCancel.Size = new Size(110, 32);
            btnCancel.Location = new Point(168, 240);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            // Add controls
            Controls.Add(lblTitle);
            Controls.Add(lblMessage);
            Controls.Add(lblNotesHeader);
            Controls.Add(txtNotes);
            Controls.Add(btnUpdate);
            Controls.Add(btnCancel);

            AcceptButton = btnUpdate;
            CancelButton = btnCancel;

            ResumeLayout(false);
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblMessage.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblNotesHeader.ForeColor = UIConstants.DarkMode.ThemeColor;
                txtNotes.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                txtNotes.ForeColor = Color.FromArgb(220, 220, 220);
                txtNotes.BorderStyle = BorderStyle.None;

                btnUpdate.BackColor = UIConstants.DarkMode.ThemeColor;
                btnUpdate.ForeColor = Color.White;

                btnCancel.BackColor = Color.FromArgb(60, 60, 60);
                btnCancel.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                return;
            }

            BackColor = Color.White;
            lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
            lblMessage.ForeColor = Color.FromArgb(85, 85, 85);
            lblNotesHeader.ForeColor = Color.FromArgb(74, 59, 42);
            txtNotes.BackColor = Color.FromArgb(248, 248, 248);
            txtNotes.ForeColor = Color.FromArgb(51, 51, 51);
            txtNotes.BorderStyle = BorderStyle.None;

            btnUpdate.BackColor = UIConstants.ThemeColor;
            btnUpdate.ForeColor = Color.FromArgb(74, 59, 42);

            btnCancel.BackColor = Color.FromArgb(240, 240, 240);
            btnCancel.ForeColor = Color.FromArgb(85, 85, 85);
        }


    }
}


