using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class UpdatesHistoryDialog : ThemedDialog
    {
        private readonly IUpdateService _updateService;
        private readonly Func<Task>? _onCheckUpdateManual;

        private Label lblTitle = null!;
        private Label lblCurrentVersion = null!;
        private ListView lstUpdates = null!;
        private Label lblNotesHeader = null!;
        private TextBox txtNotes = null!;
        private Label lblStatus = null!;
        private Button btnCheckUpdate = null!;
        private Button btnClose = null!;
        private List<UpdateReleaseItem> _releases = new();

        public UpdatesHistoryDialog(IUpdateService updateService, bool isDark, Func<Task>? onCheckUpdateManual = null)
            : base(isDark)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _onCheckUpdateManual = onCheckUpdateManual;

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            InitializeTheme();
            _ = LoadHistoryAsync();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblCurrentVersion = new Label();
            lstUpdates = new ListView();
            lblNotesHeader = new Label();
            txtNotes = new TextBox();
            lblStatus = new Label();
            btnCheckUpdate = new Button();
            btnClose = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(680, 500);
            MinimumSize = new Size(600, 420);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("UpdatesHistoryTitle");

            // Title (Top right in RTL)
            lblTitle.Text = TextManager.GetString("UpdatesHistoryTitle");
            lblTitle.Font = FontManager.CreateBlackFont(14F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(680 - 20 - lblTitle.PreferredWidth, 15);

            // Current Version Label (Top left in RTL)
            string curVer = System.Windows.Forms.Application.ProductVersion;
            lblCurrentVersion.Text = string.Format(TextManager.GetString("UpdatesHistoryCurrentVersion"), PersianNumberHelper.ToPersianDigits(curVer));
            lblCurrentVersion.Font = FontManager.CreateFont(9.5F, FontStyle.Bold);
            lblCurrentVersion.AutoSize = true;
            lblCurrentVersion.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblCurrentVersion.Location = new Point(20, 18);

            // Status label for loading or errors
            lblStatus.Text = TextManager.GetString("UpdatesHistoryLoading");
            lblStatus.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(20, 50);
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // List of updates (Right column in RTL)
            lstUpdates.Location = new Point(340, 50);
            lstUpdates.Size = new Size(320, 385);
            lstUpdates.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lstUpdates.View = View.Details;
            lstUpdates.FullRowSelect = true;
            lstUpdates.MultiSelect = false;
            lstUpdates.GridLines = true;
            lstUpdates.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lstUpdates.Columns.Add(TextManager.GetString("UpdatesHistoryColVersion"), 130);
            lstUpdates.Columns.Add(TextManager.GetString("UpdatesHistoryColDate"), 170);
            lstUpdates.SelectedIndexChanged += OnSelectedIndexChanged;

            // Notes header (Left column in RTL)
            lblNotesHeader.Text = TextManager.GetString("UpdatesHistoryNotesHeader");
            lblNotesHeader.Font = FontManager.CreateBlackFont(10.5F);
            lblNotesHeader.AutoSize = true;
            lblNotesHeader.Location = new Point(20, 50);
            lblNotesHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // Notes textbox (Left column in RTL)
            txtNotes.Multiline = true;
            txtNotes.ReadOnly = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;
            txtNotes.Location = new Point(20, 80);
            txtNotes.Size = new Size(305, 355);
            txtNotes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtNotes.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);

            // Check Update button
            btnCheckUpdate.Text = TextManager.GetString("UpdatesHistoryCheckUpdate");
            btnCheckUpdate.Size = new Size(130, 34);
            btnCheckUpdate.Location = new Point(530, 450);
            btnCheckUpdate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCheckUpdate.Cursor = Cursors.Hand;
            btnCheckUpdate.FlatStyle = FlatStyle.Flat;
            btnCheckUpdate.FlatAppearance.BorderSize = 0;
            btnCheckUpdate.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnCheckUpdate.Click += async (s, e) =>
            {
                if (_onCheckUpdateManual != null)
                {
                    Close();
                    await _onCheckUpdateManual();
                }
                else
                {
                    await LoadHistoryAsync();
                }
            };

            // Close button
            btnClose.Text = TextManager.GetString("Close");
            btnClose.Size = new Size(100, 34);
            btnClose.Location = new Point(20, 450);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblTitle);
            Controls.Add(lblCurrentVersion);
            Controls.Add(lblStatus);
            Controls.Add(lstUpdates);
            Controls.Add(lblNotesHeader);
            Controls.Add(txtNotes);
            Controls.Add(btnCheckUpdate);
            Controls.Add(btnClose);

            AcceptButton = btnClose;
            CancelButton = btnClose;

            ResumeLayout(false);
        }

        private async Task LoadHistoryAsync()
        {
            lblStatus.Visible = true;
            lblStatus.Text = TextManager.GetString("UpdatesHistoryLoading");
            lstUpdates.Items.Clear();
            txtNotes.Text = string.Empty;

            try
            {
                _releases = await _updateService.GetUpdateHistoryAsync();

                if (_releases == null || _releases.Count == 0)
                {
                    lblStatus.Visible = true;
                    lblStatus.Text = TextManager.GetString("UpdatesHistoryError");
                    return;
                }

                lblStatus.Visible = false;
                string curVer = System.Windows.Forms.Application.ProductVersion;

                foreach (var release in _releases)
                {
                    bool isCurrent = string.Equals(release.Version.Trim(), curVer.Trim(), StringComparison.OrdinalIgnoreCase);
                    string verTitle = $"نسخه {PersianNumberHelper.ToPersianDigits(release.Version)}";
                    if (isCurrent)
                    {
                        verTitle += $" {TextManager.GetString("UpdatesHistoryYourVersion")}";
                    }

                    var item = new ListViewItem(verTitle);
                    item.SubItems.Add(release.PersianDate);
                    item.Tag = release;

                    if (isCurrent)
                    {
                        item.Font = FontManager.CreateFont(9F, FontStyle.Bold);
                        item.ForeColor = IsDark ? UIConstants.DarkMode.ThemeColor : UIConstants.ThemeColor;
                    }

                    lstUpdates.Items.Add(item);
                }

                if (lstUpdates.Items.Count > 0)
                {
                    lstUpdates.Items[0].Selected = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Visible = true;
                lblStatus.Text = $"{TextManager.GetString("UpdatesHistoryError")}: {ex.Message}";
            }
        }

        private void OnSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstUpdates.SelectedItems.Count == 0)
                return;

            if (lstUpdates.SelectedItems[0].Tag is UpdateReleaseItem release)
            {
                string notes = release.ReleaseNotes?
                    .Replace("\\r\\n", "\n")
                    .Replace("\\n", "\n")
                    .Replace("\r\n", "\n")
                    .Replace("\n", Environment.NewLine) ?? string.Empty;

                txtNotes.Text = notes;
            }
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;

                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblCurrentVersion.ForeColor = UIConstants.DarkMode.ThemeColor;
                lblStatus.ForeColor = Color.FromArgb(180, 180, 180);
                lblNotesHeader.ForeColor = UIConstants.DarkMode.ThemeColor;

                lstUpdates.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lstUpdates.ForeColor = Color.FromArgb(220, 220, 220);
                lstUpdates.BorderStyle = BorderStyle.FixedSingle;

                txtNotes.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                txtNotes.ForeColor = Color.FromArgb(220, 220, 220);
                txtNotes.BorderStyle = BorderStyle.FixedSingle;

                btnCheckUpdate.BackColor = UIConstants.DarkMode.ThemeColor;
                btnCheckUpdate.ForeColor = Color.White;

                btnClose.BackColor = Color.FromArgb(60, 60, 60);
                btnClose.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
            }
            else
            {
                BackColor = Color.White;

                lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
                lblCurrentVersion.ForeColor = UIConstants.ThemeColor;
                lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
                lblNotesHeader.ForeColor = Color.FromArgb(74, 59, 42);

                lstUpdates.BackColor = Color.FromArgb(248, 248, 248);
                lstUpdates.ForeColor = Color.FromArgb(51, 51, 51);
                lstUpdates.BorderStyle = BorderStyle.FixedSingle;

                txtNotes.BackColor = Color.FromArgb(248, 248, 248);
                txtNotes.ForeColor = Color.FromArgb(51, 51, 51);
                txtNotes.BorderStyle = BorderStyle.FixedSingle;

                btnCheckUpdate.BackColor = UIConstants.ThemeColor;
                btnCheckUpdate.ForeColor = Color.FromArgb(74, 59, 42);

                btnClose.BackColor = Color.FromArgb(240, 240, 240);
                btnClose.ForeColor = Color.FromArgb(85, 85, 85);
            }
        }
    }
}
