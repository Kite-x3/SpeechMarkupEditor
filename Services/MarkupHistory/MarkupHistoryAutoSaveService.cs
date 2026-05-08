using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.MarkupHistory;

public sealed class MarkupHistoryAutoSaveService : IMarkupHistoryAutoSaveService, IDisposable
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(10);

    private readonly IMarkupHistoryService _markupHistoryService;
    private readonly object _sync = new();
    private CancellationTokenSource? _saveCts;
    private PendingSaveRequest? _pendingSave;

    public MarkupHistoryAutoSaveService(IMarkupHistoryService markupHistoryService)
    {
        _markupHistoryService = markupHistoryService;
    }

    public void ScheduleSave(string fileName, IReadOnlyList<Series> leftSeries, IReadOnlyList<Series> rightSeries, string? sourcePath = null)
    {
        CancellationTokenSource cancellationTokenSource;

        lock (_sync)
        {
            _pendingSave = new PendingSaveRequest(fileName, leftSeries, rightSeries, sourcePath);
            _saveCts?.Cancel();
            _saveCts?.Dispose();
            _saveCts = new CancellationTokenSource();
            cancellationTokenSource = _saveCts;
        }

        _ = DebouncedSaveAsync(cancellationTokenSource.Token);
    }

    public async Task FlushPendingSaveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PendingSaveRequest? pendingSave;
        lock (_sync)
        {
            pendingSave = _pendingSave;
            _pendingSave = null;
        }

        if (pendingSave == null)
            return;

        await _markupHistoryService.SaveAsync(
            pendingSave.FileName,
            pendingSave.LeftSeries,
            pendingSave.RightSeries,
            pendingSave.SourcePath);
    }

    public async Task SaveNowAsync(string fileName, IReadOnlyList<Series> leftSeries, IReadOnlyList<Series> rightSeries, string? sourcePath = null, CancellationToken cancellationToken = default)
    {
        CancelPendingSave();
        cancellationToken.ThrowIfCancellationRequested();
        await _markupHistoryService.SaveAsync(fileName, leftSeries, rightSeries, sourcePath);
    }

    public void CancelPendingSave()
    {
        lock (_sync)
        {
            _pendingSave = null;
            _saveCts?.Cancel();
            _saveCts?.Dispose();
            _saveCts = null;
        }
    }

    public void Dispose()
    {
        CancelPendingSave();
    }

    private async Task DebouncedSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DebounceDelay, cancellationToken);
            await FlushPendingSaveAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer edit restarted the debounce window.
        }
    }

    private sealed record PendingSaveRequest(
        string FileName,
        IReadOnlyList<Series> LeftSeries,
        IReadOnlyList<Series> RightSeries,
        string? SourcePath);
}
