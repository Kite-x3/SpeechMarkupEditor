using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.MarkupHistory;

public interface IMarkupHistoryAutoSaveService
{
    void ScheduleSave(string fileName, IReadOnlyList<Series> leftSeries, IReadOnlyList<Series> rightSeries, string? sourcePath = null);
    Task FlushPendingSaveAsync(CancellationToken cancellationToken = default);
    Task SaveNowAsync(string fileName, IReadOnlyList<Series> leftSeries, IReadOnlyList<Series> rightSeries, string? sourcePath = null, CancellationToken cancellationToken = default);
    void CancelPendingSave();
}
