// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Audio;
using SpeechMarkupEditor.Infrastructure.Configuration;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Dialog;
using SpeechMarkupEditor.Services.RecognitionModels;
using SpeechMarkupEditor.Services.WordSeries;
using Vosk;

namespace SpeechMarkupEditor.Services.SpeechRecognition;

public class SpeechRecognitionService: ISpeechRecognitionService
{
    private readonly IWordSeriesService _wordSeriesService;
    private readonly IDialogService _dialogService;
    private readonly IRecognitionModelService _recognitionModelService;
    private readonly int _recognitionSampleRate;
    private readonly int _bufferSize;
    private Model? _model;
    private string? _loadedModelPath;
    private bool _modelInitialized;

    public SpeechRecognitionService(IWordSeriesService wordSeriesService, IDialogService dialogService,
        IOptions<AudioSettings> audioSettings, IRecognitionModelService recognitionModelService)
    {
        _dialogService = dialogService;
        _recognitionModelService = recognitionModelService;
        Vosk.Vosk.SetLogLevel(-1);
        _wordSeriesService = wordSeriesService;
        _recognitionSampleRate = audioSettings.Value.RecognitionSampleRate;
        _bufferSize = audioSettings.Value.BufferSize;
    }

    /// <summary>
    /// Проверка наличия модели распознания аудио
    /// </summary>
    /// <returns>Возвращает флаг наличия модели</returns>
    private bool EnsureModelInitialized()
    {
        var currentModel = _recognitionModelService.GetCurrentModel();
        var currentPath = currentModel?.Path;

        if (_modelInitialized && _model != null &&
            string.Equals(_loadedModelPath, currentPath, StringComparison.OrdinalIgnoreCase))
            return true;

        var possiblePaths = new List<string>();

        if (!string.IsNullOrEmpty(currentPath))
        {
            possiblePaths.Add(currentPath);
        }

        _model?.Dispose();
        _model = null;
        _loadedModelPath = null;
        _modelInitialized = false;

        foreach (string path in possiblePaths.Distinct())
        {
            try
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        _model = new Model(path);
                        _loadedModelPath = path;
                        _modelInitialized = true;
                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No access to path: {path}");
            }
        }

        string errorMessage = $"{Resources.SpeechModelNotFound}:\n{string.Join("\n", possiblePaths.Select(p => $"- {p}"))}";
        Dispatcher.UIThread.InvokeAsync(() =>
            _dialogService.ShowWarningAsync(errorMessage));

        return false;
    }

    /// <summary>
    /// Распознавание слов в аудио источнике
    /// </summary>
    /// <param name="sourceProvider">Источник аудио</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Возвращает список слов для левого и правого аудио каналов</returns>
    public async Task<WordsRecognitionResult> RecognizeAsync(IAudioSourceProvider sourceProvider, CancellationToken  cancellationToken = default)
    {
        if (!EnsureModelInitialized())
            return new WordsRecognitionResult
            {
                LeftChannelSeries = [],
                RightChannelSeries = []
            };

        var source = await sourceProvider.OpenAudioStreamAsync();
        Stream convertedStream = Ensure16KHz(source);

        await using var reader = new WaveFileReader(convertedStream);
        int sampleRate = reader.WaveFormat.SampleRate;
        int channels = reader.WaveFormat.Channels;

        var leftWords = new List<WordTimestamp>();
        var rightWords = new List<WordTimestamp>();

        var recLeft = new VoskRecognizer(_model!, sampleRate);
        recLeft.SetWords(true);

        VoskRecognizer? recRight = null;
        if (channels == 2)
        {
            recRight = new VoskRecognizer(_model!, sampleRate);
            recRight.SetWords(true);
        }

        byte[] buffer = new byte[_bufferSize];
        while (await reader.ReadAsync(buffer, 0, buffer.Length, cancellationToken) > 0)
        {
            if (channels == 1)
            {
                if (recLeft.AcceptWaveform(buffer, buffer.Length))
                {
                    string? result = recLeft.Result();
                    leftWords.AddRange(ExtractWords(result));
                }
            }
            else
            {
                int sampleCount = buffer.Length / 4;
                short[] leftSamples = new short[sampleCount];
                short[] rightSamples = new short[sampleCount];

                for (int i = 0, s = 0; i < buffer.Length - 3; i += 4, s++)
                {
                    leftSamples[s] = BitConverter.ToInt16(buffer, i);
                    rightSamples[s] = BitConverter.ToInt16(buffer, i + 2);
                }

                if (recLeft.AcceptWaveform(leftSamples, leftSamples.Length))
                {
                    string? result = recLeft.Result();
                    leftWords.AddRange(ExtractWords(result));
                }

                if (recRight!.AcceptWaveform(rightSamples, rightSamples.Length))
                {
                    string? result = recRight.Result();
                    rightWords.AddRange(ExtractWords(result));
                }
            }
        }

        leftWords.AddRange(ExtractWords(recLeft.FinalResult()));
        if (recRight!=null)
            rightWords.AddRange(ExtractWords(recRight.FinalResult()));

        if (channels == 1)
            rightWords.AddRange(leftWords);

        var leftSeries = _wordSeriesService.GroupWordsIntoSeries(leftWords);
        var rightSeries = _wordSeriesService.GroupWordsIntoSeries(rightWords);

        return new WordsRecognitionResult
        {
            LeftChannelSeries = ConvertToSeriesList(leftSeries),
            RightChannelSeries = ConvertToSeriesList(rightSeries)
        };
    }

    private List<Series> ConvertToSeriesList(List<List<WordTimestamp>> wordSeries)
    {
        return wordSeries.Select((series, index) =>
        {
            var newSeries = new Series { SeriesNumber = index + 1 };
            foreach (var word in series)
            {
                newSeries.AddWord(word);
            }
            return newSeries;
        }).ToList();
    }


    /// <summary>
    /// Преобразует аудиопоток к частоте 16kHz
    /// </summary>
    /// <param name="inputStream"></param>
    /// <returns></returns>
    private Stream Ensure16KHz(Stream inputStream)
    {
        using var reader = new WaveFileReader(inputStream);
        var desiredFormat = new WaveFormat(_recognitionSampleRate, reader.WaveFormat.Channels);
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
        try
        {
            using (var resampler = new MediaFoundationResampler(reader, desiredFormat))
            {
                resampler.ResamplerQuality = 60;
                WaveFileWriter.CreateWaveFile(tempPath, resampler);
            }

            return new FileStream(tempPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                _bufferSize,
                FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            throw;
        }
    }

    /// <summary>
    /// Извлекает слова из JSON результата распознавания
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    private List<WordTimestamp> ExtractWords(string json)
    {
        var result = new List<WordTimestamp>();

        if (string.IsNullOrWhiteSpace(json))
            return result;

        var jObj = JsonNode.Parse(json);
        var words = jObj["result"];

        if (words == null)
            return result;

        foreach (var word in words.AsArray())
        {
            result.Add(new WordTimestamp
            (
                word["word"]?.ToString(),
                (double)word["start"],
                (double)word["end"]
            ));
        }

        return result;
    }
}
