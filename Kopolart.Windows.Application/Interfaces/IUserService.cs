using Kopolart.Windows.Domain.Models;
using System;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IUserService
    {
        UserInfo? CurrentUser { get; }
        bool IsLoggedIn { get; }
        string? GetDisplayName(int maxLength = 20);
        Task<UserInfo?> RefreshUserInfoAsync(object? webView);
        void ClearUserInfo();
        event Action<UserInfo?>? UserInfoChanged;
    }
}
