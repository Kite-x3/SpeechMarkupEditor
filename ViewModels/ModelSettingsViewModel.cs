// Copyright (C) Neurosoft

using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Dialog;
using SpeechMarkupEditor.Services.RecognitionModels;

namespace SpeechMarkupEditor.ViewModels;

public partial class ModelSettingsViewModel : ObservableObject
{
    private readonly IRecognitionModelService _recognitionModelService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<RecognitionModelDefinition> _models = [];

    [ObservableProperty]
    private string _newModelName = Resources.VoskModelName;

    [ObservableProperty]
    private string _newModelPath = string.Empty;

    public ModelSettingsViewModel(IRecognitionModelService recognitionModelService, IDialogService dialogService)
    {
        _recognitionModelService = recognitionModelService;
        _dialogService = dialogService;
        Reload();
    }

    [RelayCommand]
    private async Task BrowsePath()
    {
        var folder = await _dialogService.ShowOpenFolderDialogAsync(Resources.SelectVoskModelFolder);
        if (string.IsNullOrWhiteSpace(folder))
            return;

        NewModelPath = folder;
        if (string.IsNullOrWhiteSpace(NewModelName) || NewModelName == Resources.VoskModelName)
        {
            NewModelName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
    }

    [RelayCommand]
    private async Task AddModel()
    {
        if (string.IsNullOrWhiteSpace(NewModelName) || string.IsNullOrWhiteSpace(NewModelPath))
        {
            await _dialogService.ShowWarningAsync(Resources.SpecifyModelNameAndPath);
            return;
        }

        if (!Directory.Exists(NewModelPath))
        {
            await _dialogService.ShowWarningAsync(Resources.ModelPathDoesNotExist);
            return;
        }

        await _recognitionModelService.AddModelAsync(NewModelName.Trim(), NewModelPath.Trim());
        Reload();
    }

    [RelayCommand]
    private async Task SetCurrent(RecognitionModelDefinition? model)
    {
        if (model == null)
            return;

        await _recognitionModelService.SetCurrentModelAsync(model.Path);
        Reload();
    }

    [RelayCommand]
    private async Task DeleteModel(RecognitionModelDefinition? model)
    {
        if (model == null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.Warning,
            string.Format(Resources.DeleteModelConfirmationFormat, model.Name),
            Resources.Delete,
            Resources.Cancel);

        if (!confirmed)
            return;

        await _recognitionModelService.DeleteModelAsync(model.Path);
        Reload();
    }

    private void Reload()
    {
        Models = new ObservableCollection<RecognitionModelDefinition>(
            _recognitionModelService.GetModels());
    }
}

