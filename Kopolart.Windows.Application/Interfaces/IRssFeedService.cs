using Kopolart.Windows.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IRssFeedService
    {
        Task<List<RssFeedItem>> FetchFeedAsync(CancellationToken cancellationToken = default);
        Task<RssFeedItem?> GetLatestNewPostAsync(CancellationToken cancellationToken = default);
        Task<List<RssFeedItem>> GetUnseenNewPostsAsync(CancellationToken cancellationToken = default);
        void MarkPostAsNotified(RssFeedItem post);
        bool CanNotifyNow();
        Task<Dictionary<string, int>> GetRecentCategoryCountsAsync(System.TimeSpan? window = null, CancellationToken cancellationToken = default);
    }
}
