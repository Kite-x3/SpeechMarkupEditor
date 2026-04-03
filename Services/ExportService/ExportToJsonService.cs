// Copyright (C) Neurosoft

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Dialog;
using System.Threading.Tasks;
using SpeechMarkupEditor.Assets;

namespace SpeechMarkupEditor.Services.ExportService;

public class ExportToJsonService : IExportService
{
    private readonly IDialogService _dialogService;

    public ExportToJsonService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task ExportAsync(ObservableCollection<Series> leftSeries,
        ObservableCollection<Series> rightSeries, string fileName)
    {
        if (leftSeries == null && rightSeries == null)
            throw new ArgumentNullException(nameof(leftSeries));

        try
        {
            var exportData = new
            {
                FileName = fileName,
                LeftChannel = leftSeries,
                RightChannel = rightSeries
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            string json = JsonSerializer.Serialize(exportData, options);

            string? filePath = await _dialogService.ShowSaveFileDialogAsync(
                Resources.ExportDialogTitle,
                $"JSON {Resources.Files} (*.json)|*.json");

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            await File.WriteAllTextAsync(filePath, json);

            await _dialogService.ShowSuccessAsync(Resources.ExportSuccess);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}