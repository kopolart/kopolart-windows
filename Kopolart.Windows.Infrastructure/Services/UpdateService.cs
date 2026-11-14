using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Infrastructure.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdateService>? _logger;

        public UpdateService(IHttpClientFactory httpClientFactory, ILogger<UpdateService>? logger = null)
        {
            _httpClient = httpClientFactory.CreateClient("DefaultClient");
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously fetches update info from the remote JSON file.
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(AppConstants.Urls.UpdateJsonUrl, cancellationToken);
                return ParseManifest(response, currentVersion);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Update check failed");
                return null;
            }
        }

        /// <summary>
        /// Fetches the full list of update release history from the remote manifest.
        /// </summary>
        public async Task<List<UpdateReleaseItem>> GetUpdateHistoryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(AppConstants.Urls.UpdateJsonUrl, cancellationToken);
                var info = ParseManifest(response, null);
                return info?.Updates ?? new List<UpdateReleaseItem>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch update history");
                return new List<UpdateReleaseItem>();
            }
        }

        private UpdateInfo? ParseManifest(string json, string? currentVersion)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var info = new UpdateInfo();

            if (root.TryGetProperty("version", out var versionProp))
            {
                info.Version = versionProp.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("url", out var urlProp))
            {
                info.Url = urlProp.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("date", out var topDateProp))
            {
                info.Date = topDateProp.GetString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(info.Url))
            {
                info.Url = AppConstants.Urls.FallbackDownloadUrl;
            }

            Version.TryParse(info.Version, out var latestVersion);
            Version.TryParse(currentVersion ?? string.Empty, out var localVersion);

            bool hasCumulativeUpdates = false;

            if (root.TryGetProperty("updates", out var updatesProp) && updatesProp.ValueKind == JsonValueKind.Array)
            {
                var parsedItems = new List<(Version ver, string rawVer, string date, string notes)>();

                foreach (var element in updatesProp.EnumerateArray())
                {
                    if (element.TryGetProperty("version", out var nodeVerProp))
                    {
                        string nodeVerStr = nodeVerProp.GetString() ?? string.Empty;
                        string dateStr = string.Empty;
                        if (element.TryGetProperty("date", out var dateProp))
                        {
                            dateStr = dateProp.GetString() ?? string.Empty;
                        }

                        string notesText = string.Empty;
                        if (element.TryGetProperty("releaseNotes", out var notesProp))
                        {
                            if (notesProp.ValueKind == JsonValueKind.Object)
                            {
                                if (notesProp.TryGetProperty("fa", out var faProp)) notesText = faProp.GetString() ?? string.Empty;
                                else if (notesProp.TryGetProperty("notes", out var nProp)) notesText = nProp.GetString() ?? string.Empty;
                            }
                            else if (notesProp.ValueKind == JsonValueKind.String)
                            {
                                notesText = notesProp.GetString() ?? string.Empty;
                            }
                        }

                        Version.TryParse(nodeVerStr, out var parsedVer);
                        parsedItems.Add((parsedVer ?? new Version(0, 0), nodeVerStr, dateStr, notesText));

                        info.Updates.Add(new UpdateReleaseItem
                        {
                            Version = nodeVerStr,
                            Date = dateStr,
                            ReleaseNotes = notesText,
                            IsCurrentVersion = !string.IsNullOrWhiteSpace(currentVersion) &&
                                               string.Equals(nodeVerStr.Trim(), currentVersion.Trim(), StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }

                // If top-level date was not set, use the date of the first/latest release
                if (string.IsNullOrWhiteSpace(info.Date) && info.Updates.Count > 0)
                {
                    info.Date = info.Updates[0].Date;
                }

                parsedItems.Sort((x, y) => y.ver.CompareTo(x.ver));

                var sbNotes = new System.Text.StringBuilder();

                if (localVersion != null && localVersion > new Version(0, 0))
                {
                    foreach (var update in parsedItems)
                    {
                        if (update.ver > localVersion && (latestVersion == null || update.ver <= latestVersion))
                        {
                            hasCumulativeUpdates = true;

                            if (sbNotes.Length > 0) sbNotes.Append("\r\n\r\n");

                            string pDate = PersianDateHelper.ToPersianDateString(update.date);
                            string dateSuffix = !string.IsNullOrWhiteSpace(pDate) ? $" ({pDate})" : string.Empty;

                            sbNotes.Append($"نسخه {update.rawVer}{dateSuffix}:\r\n{update.notes}");
                        }
                    }
                }

                if (hasCumulativeUpdates)
                {
                    info.ReleaseNotes = sbNotes.ToString();
                }
            }

            if (!hasCumulativeUpdates)
            {
                if (root.TryGetProperty("releaseNotes", out var notesProp))
                {
                    if (notesProp.ValueKind == JsonValueKind.Object)
                    {
                        if (notesProp.TryGetProperty("fa", out var faProp)) info.ReleaseNotes = faProp.GetString() ?? string.Empty;
                        else if (notesProp.TryGetProperty("notes", out var nProp)) info.ReleaseNotes = nProp.GetString() ?? string.Empty;
                    }
                    else if (notesProp.ValueKind == JsonValueKind.String)
                    {
                        info.ReleaseNotes = notesProp.GetString() ?? string.Empty;
                    }
                }
                else if (root.TryGetProperty("notes", out var directNotesProp))
                {
                    info.ReleaseNotes = directNotesProp.GetString() ?? string.Empty;
                }
            }

            return info;
        }
    }
}
