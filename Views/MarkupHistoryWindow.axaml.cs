using Avalonia.Controls;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.ViewModels;

namespace SpeechMarkupEditor.Views;

public partial class MarkupHistoryWindow : Window
{
    public MarkupHistoryEntrySummary? SelectedEntry { get; private set; }

    public MarkupHistoryWindow()
    {
        InitializeComponent();
    }

    private void OpenButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MarkupHistoryViewModel viewModel)
        {
            SelectedEntry = viewModel.SelectedEntry;
            Close(SelectedEntry);
        }
    }

    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}
