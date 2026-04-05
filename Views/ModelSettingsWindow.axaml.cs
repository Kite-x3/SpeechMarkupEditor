using Avalonia.Controls;

namespace SpeechMarkupEditor.Views;

public partial class ModelSettingsWindow : Window
{
    public ModelSettingsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
