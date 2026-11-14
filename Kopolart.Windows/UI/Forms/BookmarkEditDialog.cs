using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class BookmarkEditDialog : ThemedDialog
    {
        private readonly Label _lblTitle;
        private readonly TextBox _txtTitle;
        private readonly Button _btnSave;
        private readonly Button _btnCancel;

        public string BookmarkTitle => _txtTitle.Text.Trim();

        public BookmarkEditDialog(string currentTitle, bool isDark)
            : base(isDark)
        {
            Text = TextManager.GetString("EditBookmarkTitle");
            ClientSize = new Size(400, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            _lblTitle = new Label
            {
                Text = TextManager.GetString("NewBookmarkTitlePrompt"),
                Location = new Point(20, 20),
                Size = new Size(360, 25),
                Font = FontManager.CreateFont(9.5F, FontStyle.Regular)
            };

            _txtTitle = new TextBox
            {
                Text = currentTitle ?? string.Empty,
                Location = new Point(20, 50),
                Size = new Size(360, 26),
                Font = FontManager.CreateFont(9.5F, FontStyle.Regular)
            };

            _btnSave = new Button
            {
                Text = TextManager.GetString("Save"),
                DialogResult = DialogResult.OK,
                Location = new Point(290, 95),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                Font = FontManager.CreateFont(9F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;

            _btnCancel = new Button
            {
                Text = TextManager.GetString("Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(190, 95),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                Font = FontManager.CreateFont(9F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 1;

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;

            Controls.Add(_lblTitle);
            Controls.Add(_txtTitle);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);

            FontManager.ApplyCustomFont(this);
            InitializeTheme();
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                _lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                _txtTitle.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                _txtTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                _txtTitle.BorderStyle = BorderStyle.FixedSingle;
                _btnSave.BackColor = UIConstants.DarkMode.ThemeColor;
                _btnSave.ForeColor = Color.White;
                _btnCancel.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            }
            else
            {
                BackColor = Color.White;
                _lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
                _txtTitle.BackColor = Color.White;
                _txtTitle.ForeColor = Color.Black;
                _txtTitle.BorderStyle = BorderStyle.FixedSingle;
                _btnSave.BackColor = UIConstants.ThemeColor;
                _btnSave.ForeColor = Color.FromArgb(74, 59, 42);
                _btnCancel.ForeColor = Color.FromArgb(85, 85, 85);
                _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            }
        }
    }
}
