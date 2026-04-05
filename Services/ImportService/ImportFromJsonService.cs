// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Dialog;

namespace SpeechMarkupEditor.Services.ImportService;

public class ImportFromJsonService : IImportService
{
    private readonly IDialogService _dialogService;

    public ImportFromJsonService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<ImportedMarkup?> ImportAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync(Resources.ImportMarkupDialogTitle, Resources.JsonFilesFilter);
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var imported = JsonSerializer.Deserialize<ImportedMarkupDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (imported == null)
                throw new InvalidOperationException(Resources.InvalidMarkupFileFormat);

            return new ImportedMarkup
            {
                FileName = string.IsNullOrWhiteSpace(imported.FileName)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : imported.FileName,
                LeftChannel = MapSeries(imported.LeftChannel),
                RightChannel = MapSeries(imported.RightChannel)
            };
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(string.Format(Resources.ImportMarkupFailedFormat, ex.Message));
            return null;
        }
    }

    private static ObservableCollection<Series> MapSeries(List<SeriesDto>? source)
    {
        var result = new ObservableCollection<Series>();
        if (source == null)
            return result;

        var orderedSeries = source
            .Select(MapSeries)
            .Where(series => series.Words.Count > 0)
            .OrderBy(series => series.Words[0].StartTime)
            .ToList();

        for (var i = 0; i < orderedSeries.Count; i++)
        {
            orderedSeries[i].SeriesNumber = i + 1;
            result.Add(orderedSeries[i]);
        }

        return result;
    }

    private static Series MapSeries(SeriesDto seriesDto)
    {
        var series = new Series();
        foreach (var word in (seriesDto.Words ?? [])
                     .OrderBy(word => word.StartTime)
                     .ThenBy(word => word.Word, StringComparer.Ordinal))
        {
            series.AddWord(new WordTimestamp(word.Word ?? string.Empty, word.StartTime, word.EndTime));
        }

        return series;
    }

    private sealed class ImportedMarkupDto
    {
        public string? FileName { get; set; }
        public List<SeriesDto>? LeftChannel { get; set; }
        public List<SeriesDto>? RightChannel { get; set; }
    }

    private sealed class SeriesDto
    {
        public List<WordTimestampDto>? Words { get; set; }
    }

    private sealed class WordTimestampDto
    {
        public string? Word { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }
}

