using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class ShortcutsDialog : ThemedDialog
    {
        private Label lblTitle = null!;
        private ListView lstShortcuts = null!;
        private Button btnClose = null!;

        public ShortcutsDialog(bool isDark)
            : base(isDark)
        {
            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            PopulateShortcuts();
            InitializeTheme();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lstShortcuts = new ListView();
            btnClose = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(460, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("ShortcutsTitle");

            lblTitle.Text = TextManager.GetString("ShortcutsTitle");
            lblTitle.Font = FontManager.CreateBlackFont(14F);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(460 - 20 - lblTitle.PreferredWidth, 15);

            lstShortcuts.Location = new Point(20, 55);
            lstShortcuts.Size = new Size(420, 300);
            lstShortcuts.View = View.Details;
            lstShortcuts.FullRowSelect = true;
            lstShortcuts.GridLines = true;
            lstShortcuts.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lstShortcuts.Columns.Add(TextManager.GetString("ShortcutsColShortcut"), 150);
            lstShortcuts.Columns.Add(TextManager.GetString("ShortcutsColDescription"), 265);

            btnClose.Text = TextManager.GetString("Close");
            btnClose.Size = new Size(100, 35);
            btnClose.Location = new Point(20, 370);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblTitle);
            Controls.Add(lstShortcuts);
            Controls.Add(btnClose);

            AcceptButton = btnClose;

            ResumeLayout(false);
        }

        private void PopulateShortcuts()
        {
            var shortcuts = new (string key, string desc)[]
            {
                ("Ctrl + F", TextManager.GetString("ShortcutFind")),
                ("Ctrl + D", TextManager.GetString("ShortcutBookmark")),
                ("Ctrl + H", TextManager.GetString("ShortcutHistory")),
                ("Ctrl + J", TextManager.GetString("MenuDownloads")),
                ("Ctrl + R / F5", TextManager.GetString("ShortcutReload")),
                ("Ctrl + Shift + R", TextManager.GetString("ShortcutHardReload")),
                ("Alt + Left", TextManager.GetString("ShortcutBack")),
                ("Alt + Right", TextManager.GetString("ShortcutForward")),
                ("Alt + Home", TextManager.GetString("ShortcutDashboard")),
                ("Escape (Esc)", TextManager.GetString("ShortcutCloseFind"))
            };

            foreach (var (key, desc) in shortcuts)
            {
                var item = new ListViewItem(key);
                item.SubItems.Add(desc);
                lstShortcuts.Items.Add(item);
            }

            if (lstShortcuts.Columns.Count > 1)
            {
                lstShortcuts.Columns[1].Width = -2;
            }
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lstShortcuts.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lstShortcuts.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnClose.BackColor = UIConstants.DarkMode.ThemeColor;
                btnClose.ForeColor = Color.White;
            }
            else
            {
                BackColor = Color.White;
                lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
                lstShortcuts.BackColor = Color.White;
                lstShortcuts.ForeColor = Color.Black;
                btnClose.BackColor = UIConstants.ThemeColor;
                btnClose.ForeColor = Color.FromArgb(74, 59, 42);
            }
        }
    }
}

