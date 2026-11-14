using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IOnlineUsersService
    {
        Task<int?> GetOnlineUsersCountAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    }
}
