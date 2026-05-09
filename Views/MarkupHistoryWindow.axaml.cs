using Avalonia.Controls;
using Avalonia.Input;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.ViewModels;

namespace SpeechMarkupEditor.Views;

public partial class MarkupHistoryWindow : Window
{
    public MarkupHistoryEntrySummary? SelectedEntry { get; private set; }

    public MarkupHistoryWindow()
    {
        InitializeComponent();
        WindowIconFactory.ApplyAppIcon(this);
    }

    private void OpenButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenSelectedEntry();
    }

    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private void HistoryListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        OpenSelectedEntry();
    }

    private void OpenSelectedEntry()
    {
        if (DataContext is not MarkupHistoryViewModel viewModel || viewModel.SelectedEntry == null)
            return;

        SelectedEntry = viewModel.SelectedEntry;
        Close(SelectedEntry);
    }
}
