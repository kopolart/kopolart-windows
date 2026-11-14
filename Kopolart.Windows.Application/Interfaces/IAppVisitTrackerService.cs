using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IAppVisitTrackerService
    {
        Task TrackBrowseVisitAsync(object? webView, string? url);
        Task TrackVisitAsync(string visitId);
    }
}
