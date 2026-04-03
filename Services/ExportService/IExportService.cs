// Copyright (C) Neurosoft

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.ExportService;

public interface IExportService
{
    Task ExportAsync(ObservableCollection<Series> leftSeries,
        ObservableCollection<Series> rightSeries, string fileName);
}