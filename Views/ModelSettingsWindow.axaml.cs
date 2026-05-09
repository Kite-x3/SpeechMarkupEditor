using Avalonia.Controls;
using SpeechMarkupEditor.Assets;

namespace SpeechMarkupEditor.Views;

public partial class ModelSettingsWindow : Window
{
    public ModelSettingsWindow()
    {
        InitializeComponent();
        WindowIconFactory.ApplyAppIcon(this);
    }

    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
