// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using SpeechMarkupEditor.Controls;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using NAudio.Wave;
using SpeechMarkupEditor.Infrastructure.Audio;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.AudioVisualization;

public class AudioVisualizationService : IAudioVisualizationService
{
    private WaveformControl _waveformControl;
    private List<float> _samples = new();
    public event EventHandler<WaveformEventArgs>? WaveformUpdated;

    public void Initialize(WaveformControl control)
    {
        _waveformControl = control;
        _waveformControl.SizeChanged+= OnControlSizeChanged;
    }

    /// <summary>
    /// Обработчик изменения размера waveform
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="sizeChangedEventArgs"></param>
    private void OnControlSizeChanged(object? sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        double width = sizeChangedEventArgs.NewSize.Width;
        double height = sizeChangedEventArgs.NewSize.Height;
        var points = CalculateWaveformPoints(_samples, width, height);
        _waveformControl.InitializeAsync(points);
        WaveformUpdated?.Invoke(this, new WaveformEventArgs { Points = points });
    }

    /// <summary>
    /// Обновляет визуализацию waveform для указанного источника аудио
    /// </summary>
    /// <param name="sourceProvider">Источник аудио</param>
    public async Task UpdateVisualizationAsync(IAudioSourceProvider sourceProvider)
    {
        _samples = await GetSamplesFromAudioAsync(sourceProvider);
        double width = _waveformControl.Bounds.Width;
        double height = _waveformControl.Bounds.Height;
        var points = CalculateWaveformPoints(_samples, width, height);
        await _waveformControl.InitializeAsync(points);
        WaveformUpdated?.Invoke(this, new WaveformEventArgs { Points = points });
    }

    /// <summary>
    /// Извлекает аудиосемплы из указанного источника
    /// </summary>
    /// <param name="sourceProvider">Источник аудиоданных</param>
    /// <returns>Список семплов</returns>
    private async Task<List<float>> GetSamplesFromAudioAsync(IAudioSourceProvider sourceProvider)
    {
        var samples = new List<float>();
        await using var stream = await sourceProvider.OpenAudioStreamAsync();
        var reader = new WaveFileReader(stream);
        var sampleProvider = reader.ToSampleProvider();
        float[] sampleBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
        int samplesRead;

        while ((samplesRead = sampleProvider.Read(sampleBuffer, 0, sampleBuffer.Length)) > 0)
        {
            ProcessSamples(sampleBuffer, samplesRead, reader.WaveFormat.Channels, samples);
        }

        return samples;
    }

    /// <summary>
    /// Обработка сэмплов - усреднение по каналам
    /// </summary>
    /// <param name="buffer">Буфер сэмплов</param>
    /// <param name="samplesRead">Количество считанных сэмплов</param>
    /// <param name="channels">Количество каналов</param>
    /// <param name="samples">Усредненные звуковые семплы</param>
    private void ProcessSamples(float[] buffer, int samplesRead, int channels, List<float> samples)
    {
        for (int i = 0; i < samplesRead; i += channels)
        {
            float sample = 0;
            for (int j = 0; j < channels; j++)
            {
                if (i + j < samplesRead)
                {
                    sample += buffer[i + j];
                }
            }

            sample /= channels;
            samples.Add(sample);
        }
    }

    /// <summary>
    /// Рассчитывает точки для отрисовки waveform на основе аудиосемплов
    /// </summary>
    /// <param name="samples">Список аудиосемплов</param>
    /// <param name="width">Ширина области отрисовки</param>
    /// <param name="height">Высота области отрисовки</param>
    /// <returns>Список точек для отрисовки waveform</returns>
    private List<Point> CalculateWaveformPoints(List<float> samples, double width, double height)
    {
        var points = new List<Point>();

        if (samples.Count == 0 || width <= 0 || height <= 0)
            return points;

        int pointsCount = (int)width / 4;
        if (pointsCount == 0)
            pointsCount = 1;

        int samplesPerPoint = samples.Count / pointsCount;
        if (samplesPerPoint == 0) samplesPerPoint = 1;

        for (int x = 0; x < pointsCount; x++)
        {
            int startSample = x * samplesPerPoint;
            if (startSample >= samples.Count) continue;

            int endSample = (x + 1) * samplesPerPoint;
            endSample = Math.Min(endSample, samples.Count - 1);

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = startSample; i < endSample; i++)
            {
                float sample = samples[i];
                min = Math.Min(min, sample);
                max = Math.Max(max, sample);
            }

            double y1 = height / 2 - min * height / 2 - 2;
            double y2 = height / 2 - max * height / 2 + 2;

            points.Add(new Point(x * (width / pointsCount), y1));
            points.Add(new Point(x * (width / pointsCount), y2));
        }

        return points;
    }
}