using Kopolart.Windows.Domain.Models;
using System.Collections.Generic;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IBookmarkImportExportService
    {
        void ExportToFile(string filePath, IReadOnlyList<BookmarkItem> bookmarks);
        string ExportToHtml(IReadOnlyList<BookmarkItem> bookmarks);
        string ExportToJson(IReadOnlyList<BookmarkItem> bookmarks);
        List<BookmarkItem> ImportFromFile(string filePath);
        List<BookmarkItem> ImportFromContent(string content, bool isJson);
    }
}
