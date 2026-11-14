namespace Kopolart.Windows.Domain.Models
{
    /// <summary>
    /// Formatted text of a new-post notification. Kept in the domain layer so both the toast
    /// notifier and the tray-balloon fallback render exactly the same content.
    /// </summary>
    public class PostNotificationContent
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        /// <summary>Post that should open when the notification is clicked.</summary>
        public string PrimaryUrl { get; set; } = string.Empty;
    }
}
