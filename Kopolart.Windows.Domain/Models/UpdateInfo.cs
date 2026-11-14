namespace Kopolart.Windows.Domain.Models
{
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public System.Collections.Generic.List<UpdateReleaseItem> Updates { get; set; } = new();

        /// <summary>
        /// True when this update's version is newer than the given current version.
        /// Single home for the comparison that was previously copy-pasted at every
        /// call site; returns false when either version fails to parse.
        /// </summary>
        public bool IsNewerThan(string currentVersion)
        {
            if (string.IsNullOrWhiteSpace(Version))
            {
                return false;
            }

            if (!System.Version.TryParse(Version, out var remote) ||
                !System.Version.TryParse(currentVersion, out var local))
            {
                return false;
            }

            return remote > local;
        }
    }
}
