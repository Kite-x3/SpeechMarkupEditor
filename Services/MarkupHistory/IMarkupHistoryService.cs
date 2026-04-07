using System.Collections.Generic;
using System.Threading.Tasks;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.ImportService;

namespace SpeechMarkupEditor.Services.MarkupHistory;

public interface IMarkupHistoryService
{
    Task SaveAsync(string fileName, IReadOnlyList<Series> leftSeries, IReadOnlyList<Series> rightSeries, string? sourcePath = null);
    Task<IReadOnlyList<MarkupHistoryEntrySummary>> GetHistoryAsync();
    Task<ImportedMarkup?> LoadAsync(long id);
    Task DeleteAsync(long id);
    Task ClearAsync();
}
