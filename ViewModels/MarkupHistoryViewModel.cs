using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Dialog;
using SpeechMarkupEditor.Services.MarkupHistory;

namespace SpeechMarkupEditor.ViewModels;

public partial class MarkupHistoryViewModel : ObservableObject
{
    private readonly IMarkupHistoryService _markupHistoryService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<MarkupHistoryEntrySummary> _entries = [];

    [ObservableProperty]
    private MarkupHistoryEntrySummary? _selectedEntry;

    public MarkupHistoryViewModel(IMarkupHistoryService markupHistoryService, IDialogService dialogService)
    {
        _markupHistoryService = markupHistoryService;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (SelectedEntry == null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            Assets.Resources.Warning,
            string.Format(Assets.Resources.DeleteHistoryEntryConfirmationFormat, SelectedEntry.FileName),
            Assets.Resources.Delete,
            Assets.Resources.Cancel);

        if (!confirmed)
            return;

        await _markupHistoryService.DeleteAsync(SelectedEntry.Id);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        if (Entries.Count == 0)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            Assets.Resources.Warning,
            Assets.Resources.ClearHistoryConfirmation,
            Assets.Resources.ClearHistory,
            Assets.Resources.Cancel);

        if (!confirmed)
            return;

        await _markupHistoryService.ClearAsync();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        Entries = new ObservableCollection<MarkupHistoryEntrySummary>(
            await _markupHistoryService.GetHistoryAsync());

        SelectedEntry = Entries.Count > 0 ? Entries[0] : null;
    }
}
