using Kopolart.Windows.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// Fetches update info from the remote manifest, or null when the check fails.
        /// Use <see cref="UpdateInfo.IsNewerThan"/> to decide whether to offer the update.
        /// </summary>
        Task<UpdateInfo?> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches the full list of update release history from the remote manifest.
        /// </summary>
        Task<System.Collections.Generic.List<UpdateReleaseItem>> GetUpdateHistoryAsync(CancellationToken cancellationToken = default);
    }
}
