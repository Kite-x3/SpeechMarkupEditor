// Copyright (C) Neurosoft

using System.IO;
using System.Threading.Tasks;

namespace SpeechMarkupEditor.Infrastructure.Audio;

public class FileAudioSourceProvider: IAudioSourceProvider
{
    private readonly string _filePath;

    public FileAudioSourceProvider(string filePath)
    {
        _filePath = filePath;
        DisplayName = Path.GetFileName(filePath);
        SourcePath = filePath;
    }

    public string DisplayName { get; }
    public string? SourcePath { get; }

    /// <summary>
    /// Открывает поток для чтения аудиофайла
    /// </summary>
    /// <returns>Поток с аудиоданными</returns>
    public Task<Stream> OpenAudioStreamAsync()
    {
        return Task.FromResult<Stream>(File.OpenRead(_filePath));
    }
}
