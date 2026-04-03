// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeechMarkupEditor.Controls;
using SpeechMarkupEditor.Infrastructure.Audio;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.AudioVisualization;

public interface IAudioVisualizationService
{
    event EventHandler<WaveformEventArgs>? WaveformUpdated;
    void Initialize(WaveformControl control);
    Task UpdateVisualizationAsync(IAudioSourceProvider sourceProvider);
}