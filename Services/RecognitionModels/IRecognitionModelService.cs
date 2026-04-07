// Copyright (C) Neurosoft

using System.Collections.Generic;
using System.Threading.Tasks;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.RecognitionModels;

public interface IRecognitionModelService
{
    IReadOnlyList<RecognitionModelDefinition> GetModels();
    RecognitionModelDefinition? GetCurrentModel();
    Task AddModelAsync(string name, string path);
    Task SetCurrentModelAsync(string path);
    Task DeleteModelAsync(string path);
}
