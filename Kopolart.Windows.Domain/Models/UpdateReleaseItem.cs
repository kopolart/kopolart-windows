using Kopolart.Windows.Domain.Helpers;

namespace Kopolart.Windows.Domain.Models
{
    /// <summary>
    /// Represents an individual release version entry with release notes and date.
    /// Resides in Domain layer to adhere to Clean Architecture rules.
    /// </summary>
    public class UpdateReleaseItem
    {
        public string Version { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>
        /// Persian (Solar Hijri) formatted date with month name (e.g. "۵ مهر ۱۴۰۵").
        /// </summary>
        public string PersianDate => PersianDateHelper.ToPersianDateString(Date);

        /// <summary>
        /// True if this entry matches the currently installed app version.
        /// </summary>
        public bool IsCurrentVersion { get; set; }
    }
}
