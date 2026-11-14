using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.UI.Forms;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    public class DataBackupCoordinator
    {
        private readonly Form _parentForm;
        private readonly IDataBackupService _backupService;
        private readonly ISettingsManager _settingsManager;
        private readonly UsageTrackerService _usageTracker;
        private readonly Func<bool> _isDarkModeProvider;
        private readonly Action<bool>? _onThemeChanged;
        private readonly Action? _onBookmarksChanged;

        public DataBackupCoordinator(
            Form parentForm,
            IDataBackupService backupService,
            ISettingsManager settingsManager,
            UsageTrackerService usageTracker,
            Func<bool> isDarkModeProvider,
            Action<bool>? onThemeChanged = null,
            Action? onBookmarksChanged = null)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _usageTracker = usageTracker ?? throw new ArgumentNullException(nameof(usageTracker));
            _isDarkModeProvider = isDarkModeProvider ?? throw new ArgumentNullException(nameof(isDarkModeProvider));
            _onThemeChanged = onThemeChanged;
            _onBookmarksChanged = onBookmarksChanged;
        }

        public async Task ExportDataAsync()
        {
            var isDark = _isDarkModeProvider();

            // Commit active running session before snapshotting
            _usageTracker.CommitElapsed(DateTime.Now);
            _settingsManager.Flush();

            using var sfd = new SaveFileDialog
            {
                Title = TextManager.GetString("MenuExportData"),
                Filter = "JSON Backup Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"kopolart_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (sfd.ShowDialog(_parentForm) != DialogResult.OK)
                return;

            try
            {
                string appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
                bool exportSuccess = await _backupService.ExportToFileAsync(sfd.FileName, appVersion);
                if (exportSuccess)
                {
                    CustomMessageBox.Show(
                        _parentForm,
                        TextManager.GetString("ExportSuccessMessage"),
                        TextManager.GetString("MenuExportData"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        isDark);
                }
                else
                {
                    CustomMessageBox.Show(
                        _parentForm,
                        TextManager.GetString("ExportFailedMessage"),
                        TextManager.GetString("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        isDark);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    _parentForm,
                    ex.Message,
                    TextManager.GetString("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    isDark);
            }
        }

        public async Task ImportDataAsync()
        {
            var isDark = _isDarkModeProvider();

            using var ofd = new OpenFileDialog
            {
                Title = TextManager.GetString("MenuImportData"),
                Filter = "JSON Backup Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog(_parentForm) != DialogResult.OK)
                return;

            var preview = await _backupService.PreviewBackupFileAsync(ofd.FileName);
            if (!preview.IsValid || preview.Bundle == null)
            {
                CustomMessageBox.Show(
                    _parentForm,
                    preview.ErrorMessage ?? TextManager.GetString("ImportInvalidFile"),
                    TextManager.GetString("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    isDark);
                return;
            }

            using var choiceDialog = new DataImportChoiceDialog(preview.Bundle, isDark);
            if (choiceDialog.ShowDialog(_parentForm) != DialogResult.OK)
                return;

            var selectedMode = choiceDialog.SelectedMode;
            var summary = await _backupService.ImportFromFileAsync(ofd.FileName, selectedMode);

            if (summary.Success)
            {
                var newTheme = _settingsManager.GetIsDarkMode();

                if (newTheme != isDark)
                {
                    _onThemeChanged?.Invoke(newTheme);
                }

                // Sync Windows Registry auto-start
                _settingsManager.SetAutoStartEnabled(_settingsManager.GetAutoStartEnabled());

                _onBookmarksChanged?.Invoke();

                CustomMessageBox.Show(
                    _parentForm,
                    string.Format(TextManager.GetString("ImportSuccess"), summary.BookmarksCount, summary.HistoryCount, summary.DownloadsCount),
                    TextManager.GetString("Success"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    newTheme);
            }
            else
            {
                CustomMessageBox.Show(
                    _parentForm,
                    string.Format(TextManager.GetString("ImportFailed"), summary.ErrorMessage),
                    TextManager.GetString("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    isDark);
            }
        }
    }
}

