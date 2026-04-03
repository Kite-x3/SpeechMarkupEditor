// Copyright (C) Neurosoft

using System.Threading;
using System.Threading.Tasks;
using SpeechMarkupEditor.Infrastructure.Audio;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.SpeechRecognition;

public interface ISpeechRecognitionService
{
    Task<WordsRecognitionResult> RecognizeAsync(IAudioSourceProvider sourceProvider, CancellationToken cancellationToken = default);
}