// Copyright (C) Neurosoft

using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using SpeechMarkupEditor.Infrastructure.Audio;

namespace SpeechMarkupEditor.Infrastructure.AudioSourceProviderFactory;

public interface IAudioSourceProviderFactory
{
    Task<IAudioSourceProvider?> CreateSourceAsync();
}