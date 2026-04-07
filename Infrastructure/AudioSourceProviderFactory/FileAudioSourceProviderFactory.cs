﻿// Copyright (C) Neurosoft

using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Audio;

namespace SpeechMarkupEditor.Infrastructure.AudioSourceProviderFactory;

public class FileAudioSourceProviderFactory : IAudioSourceProviderFactory
{
    private readonly IStorageProvider _storageProvider;

    public FileAudioSourceProviderFactory(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    /// <summary>
    /// Создает источник аудио через диалог выбора файла
    /// </summary>
    /// <returns>Провайдер аудио-источника или null, если файл не выбран</returns>
    public async Task<IAudioSourceProvider?> CreateSourceAsync()
    {
        try
        {
            var file = await _storageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions()
                {
                    AllowMultiple = false,
                    Title = Resources.SelectWavFile,
                    FileTypeFilter =
                    [
                        new FilePickerFileType($"WAV {Resources.Files}")
                        {
                            Patterns = ["*.wav"]
                        }
                    ]
                });

            if (file.Count > 0 && file[0] is { } chosenFile)
            {
                return new FileAudioSourceProvider(chosenFile.Path.LocalPath);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating audio source: {ex.Message}");
            throw;
        }
    }

    public IAudioSourceProvider? CreateSourceFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        return new FileAudioSourceProvider(filePath);
    }
}
