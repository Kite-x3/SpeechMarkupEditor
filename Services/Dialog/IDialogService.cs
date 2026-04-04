// Copyright (C) Neurosoft

using System.Threading.Tasks;
using Avalonia.Controls;

namespace SpeechMarkupEditor.Services.Dialog;

public interface IDialogService
{
    Task ShowDialogAsync(string title, string message, WindowIcon? icon = null);
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText, WindowIcon? icon = null);
    Task ShowWarningAsync(string message);
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task<string?> ShowSaveFileDialogAsync(string title, string filters);
}
