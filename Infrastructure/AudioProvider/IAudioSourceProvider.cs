// Copyright (C) Neurosoft

using System.IO;
using System.Threading.Tasks;

namespace SpeechMarkupEditor.Infrastructure.Audio;

public interface IAudioSourceProvider
{
    string DisplayName { get; }
    Task<Stream> OpenAudioStreamAsync();
}