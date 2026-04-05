// Copyright (C) Neurosoft

using System.Collections.Generic;
using SpeechMarkupEditor.Assets;

namespace SpeechMarkupEditor.Infrastructure.Configuration;

public class ModelSettings
{
    public string ModelPath { get; set; } = string.Empty;
    public List<RecognitionModelSettingsItem> Models { get; set; } = [];
}

public class RecognitionModelSettingsItem
{
    public string Name { get; set; } = string.Empty;
    public string Engine { get; set; } = Resources.VoskEngineName;
    public string Path { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}
