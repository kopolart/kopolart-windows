using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Maps the app's brand palette onto the Windows high-contrast system palette.
    /// Only <see cref="SystemColors"/> pairs are used so foreground and background always
    /// stay readable, whatever high-contrast theme the user picked.
    /// </summary>
    public static class HighContrastTheme
    {
        private sealed class OriginalColors
        {
            public OriginalColors(Color back, Color fore)
            {
                Back = back;
                Fore = fore;
            }

            public Color Back { get; }

            public Color Fore { get; }
        }

        // Remembers the colors the app had before high contrast was applied, so turning it
        // off restores the real theme instead of leaving system colors behind.
        private static readonly ConditionalWeakTable<Control, OriginalColors> Snapshot = new();

        public static Color Window => SystemColors.Window;

        public static Color WindowText => SystemColors.WindowText;

        public static Color Control => SystemColors.Control;

        public static Color ControlText => SystemColors.ControlText;

        public static Color Border => SystemColors.ControlDark;

        public static Color Highlight => SystemColors.Highlight;

        public static Color HighlightText => SystemColors.HighlightText;

        public static Color GrayText => SystemColors.GrayText;

        /// <summary>
        /// Applies system colors to <paramref name="root"/> and every descendant while high
        /// contrast is enabled. Does nothing otherwise; call <see cref="Restore"/> before
        /// re-applying the normal theme so the previous colors come back.
        /// </summary>
        public static void Apply(Control root)
        {
            if (root == null)
            {
                return;
            }

            if (SystemAccessibility.IsHighContrast)
            {
                ApplyCore(root, Control);
            }

            root.Invalidate(true);
        }

        /// <summary>
        /// Puts back the colors each control had before <see cref="Apply"/> changed them.
        /// Safe to call when high contrast was never applied.
        /// </summary>
        public static void Restore(Control root)
        {
            if (root == null)
            {
                return;
            }

            RestoreCore(root);
        }

        private static void ApplyCore(Control control, Color inheritedBack)
        {
            // Menu strips and toolbars are painted by the theme renderers, which read the
            // high-contrast palette directly. Setting BackColor here would only fight them.
            if (control is ToolStrip)
            {
                return;
            }

            if (!Snapshot.TryGetValue(control, out _))
            {
                Snapshot.Add(control, new OriginalColors(control.BackColor, control.ForeColor));
            }

            Color back;
            Color fore;

            switch (control)
            {
                case TextBoxBase:
                case ListBox:
                case ListView:
                case ComboBox:
                case TreeView:
                case DataGridView:
                    back = Window;
                    fore = WindowText;
                    break;

                case TabPage:
                    back = Window;
                    fore = WindowText;
                    break;

                case ProgressBar:
                case PictureBox:
                case TrackBar:
                    back = inheritedBack;
                    fore = ControlText;
                    break;

                default:
                    back = inheritedBack;
                    fore = ControlText;
                    break;
            }

            control.BackColor = back;
            control.ForeColor = fore;

            foreach (Control child in control.Controls)
            {
                ApplyCore(child, back);
            }
        }

        private static void RestoreCore(Control control)
        {
            if (Snapshot.TryGetValue(control, out OriginalColors? original))
            {
                control.BackColor = original.Back;
                control.ForeColor = original.Fore;
                Snapshot.Remove(control);
            }

            foreach (Control child in control.Controls)
            {
                RestoreCore(child);
            }
        }
    }
}
