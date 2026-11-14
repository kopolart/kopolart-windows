using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kopolart.Windows.Application.Services
{
    public class BookmarkImportExportService : IBookmarkImportExportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public void ExportToFile(string filePath, IReadOnlyList<BookmarkItem> bookmarks)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                string json = ExportToJson(bookmarks);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            else
            {
                string html = ExportToHtml(bookmarks);
                File.WriteAllText(filePath, html, Encoding.UTF8);
            }
        }

        public string ExportToJson(IReadOnlyList<BookmarkItem> bookmarks)
        {
            return JsonSerializer.Serialize(bookmarks, JsonOptions);
        }

        public string ExportToHtml(IReadOnlyList<BookmarkItem> bookmarks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
            sb.AppendLine("<!-- This is an automatically generated file. -->");
            sb.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
            sb.AppendLine("<TITLE>Bookmarks</TITLE>");
            sb.AppendLine("<H1>Bookmarks</H1>");
            sb.AppendLine("<DL><p>");
            sb.AppendLine("    <DT><H3>Kopolart Bookmarks</H3>");
            sb.AppendLine("    <DL><p>");
            if (bookmarks != null)
            {
                foreach (var b in bookmarks)
                {
                    long unixTime = ((DateTimeOffset)b.DateAdded).ToUnixTimeSeconds();
                    string safeTitle = WebUtility.HtmlEncode(b.Title);
                    string safeUrl = WebUtility.HtmlEncode(b.Url);
                    sb.AppendLine($"        <DT><A HREF=\"{safeUrl}\" ADD_DATE=\"{unixTime}\">{safeTitle}</A>");
                }
            }
            sb.AppendLine("    </DL><p>");
            sb.AppendLine("</DL><p>");
            return sb.ToString();
        }

        public List<BookmarkItem> ImportFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<BookmarkItem>();

            string content = File.ReadAllText(filePath, Encoding.UTF8);
            bool isJson = filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
            return ImportFromContent(content, isJson);
        }

        public List<BookmarkItem> ImportFromContent(string content, bool isJson)
        {
            var result = new List<BookmarkItem>();
            if (string.IsNullOrWhiteSpace(content))
                return result;

            if (isJson)
            {
                var items = JsonSerializer.Deserialize<List<BookmarkItem>>(content);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Url))
                        {
                            result.Add(new BookmarkItem
                            {
                                Title = string.IsNullOrWhiteSpace(item.Title) ? item.Url : item.Title,
                                Url = item.Url,
                                Category = string.IsNullOrWhiteSpace(item.Category) ? BookmarkItem.DetermineCategory(item.Url) : item.Category,
                                DateAdded = item.DateAdded == default ? DateTime.Now : item.DateAdded
                            });
                        }
                    }
                }
            }
            else
            {
                var matches = Regex.Matches(
                    content,
                    @"<a\s+(?:[^>]*?\s+)?href=[""'](?<url>https?://[^""']+)[""'][^>]*>(?<title>[^<]+)</a>",
                    RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    string url = WebUtility.HtmlDecode(match.Groups["url"].Value);
                    string title = WebUtility.HtmlDecode(match.Groups["title"].Value.Trim());

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        result.Add(new BookmarkItem
                        {
                            Title = string.IsNullOrWhiteSpace(title) ? url : title,
                            Url = url,
                            Category = BookmarkItem.DetermineCategory(url),
                            DateAdded = DateTime.Now
                        });
                    }
                }
            }

            return result;
        }
    }
}
