using System.Text.Json.Serialization;

namespace Kopolart.Windows.Domain.Models
{
    /// <summary>
    /// Model representing online users info fetched from https://kopolart.ir/api/topbar-info.
    /// Extraneous fields like 'text' are ignored by design.
    /// </summary>
    public class OnlineUsersInfo
    {
        [JsonPropertyName("online")]
        public int Online { get; set; }
    }
}
