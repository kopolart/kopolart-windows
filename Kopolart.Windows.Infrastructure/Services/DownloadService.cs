using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class DownloadService(IHttpClientFactory httpClientFactory) : IDownloadService
    {
        private readonly HttpClient _httpClient = httpClientFactory?.CreateClient("DefaultClient")
            ?? new HttpClient();

        /// <summary>
        /// Streams the response directly to disk and reports progress when requested.
        /// </summary>
        public async Task DownloadFileAsync(
            string url,
            string outputPath,
            IProgress<DownloadProgressReport>? progress = null,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            if (progress == null)
            {
                await source.CopyToAsync(file, cancellationToken);
                return;
            }

            var buffer = new byte[81920]; // 80 KB buffer for efficient I/O
            long totalRead = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;

                int percent = totalBytes.HasValue && totalBytes.Value > 0
                    ? Math.Clamp((int)((double)totalRead / totalBytes.Value * 100), 0, 100)
                    : 0;

                progress.Report(new DownloadProgressReport(totalRead, totalBytes, percent));
            }
        }
    }
}

