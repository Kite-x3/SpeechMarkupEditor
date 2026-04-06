// Copyright (C) Neurosoft

using System.Collections.Generic;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Infrastructure.Configuration;

public class ModelSettings
{
    public string ModelPath { get; set; } = string.Empty;
    public List<string> AvailableModels { get; set; } = [];
    public List<RecognitionModelSettingsItem> Models { get; set; } = [];
}


