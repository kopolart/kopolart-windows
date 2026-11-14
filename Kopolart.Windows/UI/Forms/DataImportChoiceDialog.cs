using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class DataImportChoiceDialog : ThemedDialog
    {
        private readonly DataExportBundle _bundle;

        private Label lblTitle = null!;
        private Panel pnlDetailsCard = null!;
        private Label lblDetailsDate = null!;
        private Label lblDetailsVersion = null!;
        private Label lblDetailsStats = null!;

        private Label lblChoicePrompt = null!;
        private RadioButton rbReplaceAll = null!;
        private Label lblReplaceAllDesc = null!;
        private RadioButton rbMerge = null!;
        private Label lblMergeDesc = null!;

        private Button btnConfirm = null!;
        private Button btnCancel = null!;

        public DataImportMode SelectedMode => rbReplaceAll.Checked ? DataImportMode.ReplaceAll : DataImportMode.Merge;

        public DataImportChoiceDialog(DataExportBundle bundle, bool isDark)
            : base(isDark)
        {
            _bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            InitializeTheme();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            pnlDetailsCard = new Panel();
            lblDetailsDate = new Label();
            lblDetailsVersion = new Label();
            lblDetailsStats = new Label();

            lblChoicePrompt = new Label();
            rbReplaceAll = new RadioButton();
            lblReplaceAllDesc = new Label();
            rbMerge = new RadioButton();
            lblMergeDesc = new Label();

            btnConfirm = new Button();
            btnCancel = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(480, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("ImportDataTitle");
            BackColor = Color.White;

            // 1. Title Label
            lblTitle.Text = TextManager.GetString("ImportDataTitle");
            lblTitle.Font = FontManager.CreateBlackFont(11.5F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(480 - 20 - lblTitle.PreferredWidth, 16);

            // 2. Details Card Panel
            pnlDetailsCard.Location = new Point(20, 48);
            pnlDetailsCard.Size = new Size(440, 85);
            pnlDetailsCard.BorderStyle = BorderStyle.FixedSingle;

            var localTime = _bundle.ExportedAt.ToLocalTime();
            lblDetailsDate.Text = $"{TextManager.GetString("ImportDate")} {localTime:yyyy-MM-dd HH:mm}";
            lblDetailsDate.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lblDetailsDate.Location = new Point(12, 10);
            lblDetailsDate.Size = new Size(414, 20);

            var verText = string.IsNullOrWhiteSpace(_bundle.AppVersion) ? "1.0.0" : _bundle.AppVersion;
            lblDetailsVersion.Text = $"{TextManager.GetString("ImportAppVersion")} {verText}";
            lblDetailsVersion.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lblDetailsVersion.Location = new Point(12, 32);
            lblDetailsVersion.Size = new Size(414, 20);

            int bCount = _bundle.Data.Bookmarks?.Count ?? 0;
            int hCount = _bundle.Data.NavigationHistory?.Count ?? 0;
            int dCount = _bundle.Data.DownloadHistory?.Count ?? 0;
            lblDetailsStats.Text = string.Format(TextManager.GetString("ImportCounts"), bCount, hCount, dCount);
            lblDetailsStats.Font = FontManager.CreateFont(9F, FontStyle.Bold);
            lblDetailsStats.Location = new Point(12, 54);
            lblDetailsStats.Size = new Size(414, 20);

            pnlDetailsCard.Controls.Add(lblDetailsDate);
            pnlDetailsCard.Controls.Add(lblDetailsVersion);
            pnlDetailsCard.Controls.Add(lblDetailsStats);

            // 3. Choice Prompt
            lblChoicePrompt.Text = TextManager.GetString("ImportDataPrompt");
            lblChoicePrompt.Font = FontManager.CreateBlackFont(9.5F);
            lblChoicePrompt.Location = new Point(20, 145);
            lblChoicePrompt.Size = new Size(440, 22);

            // 4. Option: Replace All
            rbReplaceAll.Text = TextManager.GetString("ImportModeReplace");
            rbReplaceAll.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            rbReplaceAll.Location = new Point(20, 172);
            rbReplaceAll.Size = new Size(440, 26);
            rbReplaceAll.Checked = true;
            rbReplaceAll.Cursor = Cursors.Hand;

            lblReplaceAllDesc.Text = TextManager.GetString("ImportModeReplaceDesc");
            lblReplaceAllDesc.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            lblReplaceAllDesc.Location = new Point(42, 200);
            lblReplaceAllDesc.Size = new Size(418, 42);

            // 5. Option: Merge
            rbMerge.Text = TextManager.GetString("ImportModeMerge");
            rbMerge.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            rbMerge.Location = new Point(20, 248);
            rbMerge.Size = new Size(440, 26);
            rbMerge.Cursor = Cursors.Hand;

            lblMergeDesc.Text = TextManager.GetString("ImportModeMergeDesc");
            lblMergeDesc.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            lblMergeDesc.Location = new Point(42, 276);
            lblMergeDesc.Size = new Size(418, 42);

            // 6. Action Buttons: Confirm on right, Cancel on left
            btnConfirm.Text = TextManager.GetString("ImportConfirmButton");
            btnConfirm.DialogResult = DialogResult.OK;
            btnConfirm.Size = new Size(130, 36);
            btnConfirm.Location = new Point(330, 355);
            btnConfirm.Cursor = Cursors.Hand;
            btnConfirm.FlatStyle = FlatStyle.Flat;
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            btnCancel.Text = TextManager.GetString("ImportCancelButton");
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Size = new Size(100, 36);
            btnCancel.Location = new Point(218, 355);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            Controls.Add(lblTitle);
            Controls.Add(pnlDetailsCard);
            Controls.Add(lblChoicePrompt);
            Controls.Add(rbReplaceAll);
            Controls.Add(lblReplaceAllDesc);
            Controls.Add(rbMerge);
            Controls.Add(lblMergeDesc);
            Controls.Add(btnConfirm);
            Controls.Add(btnCancel);

            AcceptButton = btnConfirm;
            CancelButton = btnCancel;

            ResumeLayout(false);
            PerformLayout();
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                lblTitle.ForeColor = UIConstants.DarkMode.ThemeColor;
                pnlDetailsCard.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lblDetailsDate.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblDetailsVersion.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblDetailsStats.ForeColor = UIConstants.DarkMode.ThemeColor;

                lblChoicePrompt.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                rbReplaceAll.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblReplaceAllDesc.ForeColor = Color.FromArgb(180, 180, 180);
                rbMerge.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblMergeDesc.ForeColor = Color.FromArgb(180, 180, 180);

                btnConfirm.BackColor = UIConstants.DarkMode.ThemeColor;
                btnConfirm.ForeColor = Color.White;

                btnCancel.BackColor = Color.FromArgb(60, 60, 60);
                btnCancel.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
            }
            else
            {
                BackColor = Color.White;
                ForeColor = Color.FromArgb(30, 30, 30);

                lblTitle.ForeColor = Color.FromArgb(74, 59, 42);
                pnlDetailsCard.BackColor = Color.FromArgb(250, 248, 245);
                lblDetailsDate.ForeColor = Color.FromArgb(70, 70, 70);
                lblDetailsVersion.ForeColor = Color.FromArgb(70, 70, 70);
                lblDetailsStats.ForeColor = Color.FromArgb(140, 80, 20);

                lblChoicePrompt.ForeColor = Color.FromArgb(50, 50, 50);
                rbReplaceAll.ForeColor = Color.FromArgb(30, 30, 30);
                lblReplaceAllDesc.ForeColor = Color.FromArgb(100, 100, 100);
                rbMerge.ForeColor = Color.FromArgb(30, 30, 30);
                lblMergeDesc.ForeColor = Color.FromArgb(100, 100, 100);

                btnConfirm.BackColor = UIConstants.ThemeColor;
                btnConfirm.ForeColor = Color.FromArgb(74, 59, 42);

                btnCancel.BackColor = Color.FromArgb(235, 235, 235);
                btnCancel.ForeColor = Color.FromArgb(70, 70, 70);
            }
        }
    }
}

