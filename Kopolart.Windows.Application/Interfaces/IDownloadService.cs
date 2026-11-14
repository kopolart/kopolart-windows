using Kopolart.Windows.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IDownloadService
    {
        Task DownloadFileAsync(
            string url,
            string outputPath,
            IProgress<DownloadProgressReport>? progress = null,
            CancellationToken cancellationToken = default);
    }
}

