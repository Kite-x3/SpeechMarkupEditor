// Copyright (C) Neurosoft

using SpeechMarkupEditor.Assets;

namespace SpeechMarkupEditor.Models;

public class RecognitionModelDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Engine { get; set; } = Resources.VoskEngineName;
    public string Path { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}
