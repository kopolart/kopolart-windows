using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class ExitDialog : ThemedDialog
    {
        private Button btnYes = null!;
        private Button btnNo = null!;
        private CheckBox chkDontAskAgain = null!;
        private CheckBox chkMinimize = null!;
        private Label lblMessage = null!;

        public bool DontAskAgain => chkDontAskAgain.Checked;
        public bool MinimizeToTray => chkMinimize.Checked;

        public ExitDialog(bool isDark = false, bool defaultMinimize = true)
            : base(isDark)
        {
            InitializeComponent(defaultMinimize);
            FontManager.ApplyCustomFont(this);
            InitializeTheme();
        }

        private void InitializeComponent(bool defaultMinimize)
        {
            btnYes = new Button();
            btnNo = new Button();
            chkDontAskAgain = new CheckBox();
            chkMinimize = new CheckBox();
            lblMessage = new Label();
            SuspendLayout();

            // RTL Configuration
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            // Form
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(360, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("MenuExit");
            BackColor = Color.White;

            // Label
            lblMessage.Text = TextManager.GetString("ExitPromptMessage");
            lblMessage.AutoSize = false;
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            lblMessage.Font = FontManager.CreateBlackFont(11F);
            lblMessage.Location = new Point(20, 15);
            lblMessage.Size = new Size(320, 40);

            // Checkboxes Container layout (stacked vertically)
            chkMinimize.Text = TextManager.GetString("MinimizeToTrayPrompt");
            chkMinimize.AutoSize = false;
            chkMinimize.CheckAlign = ContentAlignment.MiddleRight;
            chkMinimize.TextAlign = ContentAlignment.MiddleRight;
            chkMinimize.Checked = defaultMinimize;
            chkMinimize.Location = new Point(20, 65);
            chkMinimize.Size = new Size(320, 24);
            chkMinimize.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            chkDontAskAgain.Text = TextManager.GetString("DontAskAgain");
            chkDontAskAgain.AutoSize = false;
            chkDontAskAgain.CheckAlign = ContentAlignment.MiddleRight;
            chkDontAskAgain.TextAlign = ContentAlignment.MiddleRight;
            chkDontAskAgain.Location = new Point(20, 95);
            chkDontAskAgain.Size = new Size(320, 24);
            chkDontAskAgain.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            // Buttons: Yes on right, No on left
            btnYes.Text = TextManager.GetString("Yes");
            btnYes.DialogResult = DialogResult.Yes;
            btnYes.Size = new Size(95, 32);
            btnYes.Location = new Point(190, 145);
            btnYes.Cursor = Cursors.Hand;
            btnYes.FlatStyle = FlatStyle.Flat;
            btnYes.FlatAppearance.BorderSize = 0;
            btnYes.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            btnNo.Text = TextManager.GetString("No");
            btnNo.DialogResult = DialogResult.No;
            btnNo.Size = new Size(95, 32);
            btnNo.Location = new Point(75, 145);
            btnNo.Cursor = Cursors.Hand;
            btnNo.FlatStyle = FlatStyle.Flat;
            btnNo.FlatAppearance.BorderSize = 0;
            btnNo.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            // Controls
            Controls.Add(lblMessage);
            Controls.Add(chkMinimize);
            Controls.Add(chkDontAskAgain);
            Controls.Add(btnYes);
            Controls.Add(btnNo);

            AcceptButton = btnYes;
            CancelButton = btnNo;

            ResumeLayout(false);
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblMessage.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkMinimize.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                chkDontAskAgain.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnYes.BackColor = UIConstants.DarkMode.ThemeColor;
                btnYes.ForeColor = Color.White;
                btnNo.BackColor = Color.FromArgb(60, 60, 60);
                btnNo.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
            }
            else
            {
                BackColor = Color.White;
                lblMessage.ForeColor = Color.FromArgb(51, 51, 51);
                chkMinimize.ForeColor = Color.FromArgb(85, 85, 85);
                chkDontAskAgain.ForeColor = Color.FromArgb(85, 85, 85);

                // Use app theme color for Yes button, light gray for No button
                btnYes.BackColor = UIConstants.ThemeColor;
                btnYes.ForeColor = Color.FromArgb(74, 59, 42);
                btnNo.BackColor = Color.FromArgb(240, 240, 240);
                btnNo.ForeColor = Color.FromArgb(85, 85, 85);
            }
        }
    }
}


