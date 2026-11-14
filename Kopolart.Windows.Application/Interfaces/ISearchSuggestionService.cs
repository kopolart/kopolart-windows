using Kopolart.Windows.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface ISearchSuggestionService
    {
        Task<List<SearchSuggestionItem>> GetSuggestionsAsync(string term, SearchCategoryType category, CancellationToken cancellationToken = default);
    }
}
