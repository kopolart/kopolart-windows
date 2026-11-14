using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Models;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IWebViewService _webViewService;
        private UserInfo? _currentUser;

        public UserInfo? CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null && _currentUser.UserId > 0 && !string.IsNullOrWhiteSpace(_currentUser.Name);

        public event Action<UserInfo?>? UserInfoChanged;

        public UserService(IWebViewService webViewService)
        {
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
        }

        public string? GetDisplayName(int maxLength = 20)
        {
            if (_currentUser == null || string.IsNullOrWhiteSpace(_currentUser.Name))
            {
                return null;
            }

            string name = _currentUser.Name.Trim();
            if (name.Length <= maxLength)
            {
                return name;
            }

            int keepLength = Math.Max(1, maxLength - 2);
            return name.Substring(0, keepLength) + "...";
        }

        public async Task<UserInfo?> RefreshUserInfoAsync(object? webView)
        {
            try
            {
                string? rawCookie = await _webViewService.GetCookieValueAsync(
                    webView,
                    AppConstants.Cookies.ClientUserInfo,
                    AppConstants.Urls.MainUrl);

                if (string.IsNullOrWhiteSpace(rawCookie))
                {
                    UpdateCurrentUser(null);
                    return null;
                }

                var parsedUser = ParseUserCookie(rawCookie);
                UpdateCurrentUser(parsedUser);
                return _currentUser;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserService] Error refreshing user info: {ex.Message}");
                return _currentUser;
            }
        }

        public void ClearUserInfo()
        {
            UpdateCurrentUser(null);
        }

        private void UpdateCurrentUser(UserInfo? newUser)
        {
            bool changed = false;
            if (_currentUser == null && newUser != null)
            {
                changed = true;
            }
            else if (_currentUser != null && newUser == null)
            {
                changed = true;
            }
            else if (_currentUser != null && newUser != null)
            {
                changed = (_currentUser.UserId != newUser.UserId || !string.Equals(_currentUser.Name, newUser.Name, StringComparison.Ordinal));
            }

            if (changed)
            {
                _currentUser = newUser;
                UserInfoChanged?.Invoke(_currentUser);
            }
        }

        public static UserInfo? ParseUserCookie(string rawCookie)
        {
            if (string.IsNullOrWhiteSpace(rawCookie))
            {
                return null;
            }

            string decoded = rawCookie.Trim();

            // Handle potential double/triple URL-encoding (e.g. %257B -> %7B -> {)
            for (int i = 0; i < 3 && decoded.Contains('%'); i++)
            {
                try
                {
                    string next = Uri.UnescapeDataString(decoded);
                    if (next == decoded) break;
                    decoded = next;
                }
                catch
                {
                    break;
                }
            }

            try
            {
                using var doc = JsonDocument.Parse(decoded);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                long userId = 0;
                string name = string.Empty;

                foreach (var prop in root.EnumerateObject())
                {
                    string propName = prop.Name;
                    if (userId == 0 && (
                        string.Equals(propName, "userId", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(propName, "id", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(propName, "user_id", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt64(out var idNum))
                        {
                            userId = idNum;
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.String && long.TryParse(prop.Value.GetString(), out var idParsed))
                        {
                            userId = idParsed;
                        }
                    }
                    else if (string.IsNullOrEmpty(name) && (
                        string.Equals(propName, "name", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(propName, "userName", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(propName, "display_name", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(propName, "displayName", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            name = prop.Value.GetString() ?? string.Empty;
                        }
                    }
                }

                if (userId > 0 && !string.IsNullOrWhiteSpace(name))
                {
                    return new UserInfo
                    {
                        UserId = userId,
                        Name = name.Trim()
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserService] JSON parse failed: {ex.Message}");
            }

            return null;
        }
    }
}
