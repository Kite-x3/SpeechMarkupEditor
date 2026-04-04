// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Options;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Configuration;

namespace SpeechMarkupEditor.Services.Dialog;

public class DialogService : IDialogService
{
    private static Window? MainWindow =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    private readonly Lazy<WindowIcon?> _warningIcon;
    private readonly Lazy<WindowIcon?> _errorIcon;
    private readonly Lazy<WindowIcon?> _successIcon;

    public DialogService(IOptions<DialogSettings> settings)
    {
        _warningIcon = new Lazy<WindowIcon?>(() => LoadIcon(settings.Value.WarningIconPath));
        _errorIcon = new Lazy<WindowIcon?>(() => LoadIcon(settings.Value.ErrorIconPath));
        _successIcon = new Lazy<WindowIcon>(() => LoadIcon(settings.Value.SuccessIconPath));
    }

    private static WindowIcon? LoadIcon(string uri)
    {
        try
        {
            using var bitmap = new Bitmap(AssetLoader.Open(new Uri(uri)));
            return new WindowIcon(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static FilePickerFileType[] ParseFileFilters(string filters)
    {
        string[] filterParts = filters.Split('|');
        var fileTypes = new List<FilePickerFileType>();

        for (int i = 0; i < filterParts.Length; i += 2)
        {
            if (i + 1 >= filterParts.Length)
                break;

            var fileType = new FilePickerFileType(filterParts[i])
            {
                Patterns = filterParts[i + 1].Split(';')
            };

            fileTypes.Add(fileType);
        }

        return fileTypes.ToArray();
    }

    public async Task ShowDialogAsync(string title, string message, WindowIcon? icon = null)
    {
        if (MainWindow is null)
            return;

        var button = new Button
        {
            Content = "OK",
            FontSize = 14,
            MinWidth = 70,
            MinHeight = 30,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 18)
        };

        var dialog = new Window
        {
            Icon = icon,
            Title = title,
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = message, FontSize = 16, Margin = new Thickness(48, 32),
                        HorizontalAlignment = HorizontalAlignment.Center },
                    button
                }
            },

            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        button.Click += (e, sender) => dialog.Close();

        await dialog.ShowDialog(MainWindow);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText,
        WindowIcon? icon = null)
    {
        if (MainWindow is null)
            return false;

        var result = false;

        var confirmButton = new Button
        {
            Content = confirmText,
            FontSize = 14,
            MinWidth = 110,
            MinHeight = 34,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#C75A5A")),
            Foreground = Avalonia.Media.Brushes.White,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var cancelButton = new Button
        {
            Content = cancelText,
            FontSize = 14,
            MinWidth = 110,
            MinHeight = 34,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var dialog = new Window
        {
            Icon = icon,
            Title = title,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        FontSize = 16,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        MaxWidth = 420
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children =
                        {
                            cancelButton,
                            confirmButton
                        }
                    }
                }
            },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        confirmButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(MainWindow);
        return result;
    }

    public async Task ShowWarningAsync(string message)
    {
        await ShowDialogAsync(Resources.Warning, message, _warningIcon.Value);
    }

    public async Task ShowErrorAsync(string message)
    {
        await ShowDialogAsync(Resources.Error, message, _errorIcon.Value);
    }

    public async Task ShowSuccessAsync(string message)
    {
        await ShowDialogAsync(Resources.Success, message, _successIcon.Value);
    }

    public async Task<string?> ShowSaveFileDialogAsync(string title, string filters)
    {
        if (MainWindow is null) return null;

        try
        {
            var options = new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = "speech_markup",
                FileTypeChoices = ParseFileFilters(filters),
                ShowOverwritePrompt = true
            };

            var result = await MainWindow.StorageProvider.SaveFilePickerAsync(options);
            return result?.TryGetLocalPath();
        }
        catch (Exception ex)
        {
            await ShowDialogAsync(Resources.Error, $"{Resources.FileSaveError}: {ex.Message}", _errorIcon.Value);
            return null;
        }
    }
}
