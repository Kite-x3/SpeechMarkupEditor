// Copyright (C) Neurosoft

using System.Collections.ObjectModel;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.ImportService;

public class ImportedMarkup
{
    public string FileName { get; set; } = string.Empty;
    public ObservableCollection<Series> LeftChannel { get; set; } = [];
    public ObservableCollection<Series> RightChannel { get; set; } = [];
}
