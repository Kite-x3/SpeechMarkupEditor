// Copyright (C) Neurosoft

using System;
using Avalonia.Threading;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.Wave.SampleProviders;
using SpeechMarkupEditor.Infrastructure.Audio;

namespace SpeechMarkupEditor.Services.Audio;

public class AudioService : IAudioService
{
    private WaveOutEvent? _outputDevice;
    private WaveStream? _audioStream;
    private DispatcherTimer? _playbackTimer;
    private ISampleProvider? _currentProvider;
    private StereoToMonoSampleProvider? _stereoToMono;

    /// <summary>
    /// Указывает, активен ли левый аудиоканал в данный момент
    /// </summary>
    public bool IsLeftChannelActive { get; private set; } = true;

    /// <summary>
    /// Указывает, активен ли правый аудиоканал в данный момент
    /// </summary>
    public bool IsRightChannelActive { get; private set; } = true;

    /// <summary>
    /// Определяет, является ли текущий аудиофайл стерео (true) или моно (false)
    /// </summary>
    public bool IsStereoAudio { get; private set; } = false;

    /// <summary>
    /// Флаг состояние воспроизведения
    /// </summary>
    public bool IsPlaying => _outputDevice?.PlaybackState == PlaybackState.Playing;

    /// <summary>
    /// Событие обновления текущей позиции воспроизведения
    /// </summary>
    public event EventHandler<double>? PlaybackPositionUpdated;

    /// <summary>
    /// Событие изменения общей длительности трека
    /// </summary>
    public event EventHandler<double>? TotalTimeChanged;

    /// <summary>
    /// Событие изменения флага воспроизведения
    /// </summary>
    public event EventHandler<bool>? PlaybackStateChanged;

    /// <summary>
    /// Инициализирует аудио плеер для работы с указанным файлом
    /// </summary>
    public async Task Initialize(IAudioSourceProvider sourceProvider)
    {
        Dispose();
        var stream = await sourceProvider.OpenAudioStreamAsync();
        _audioStream = new WaveFileReader(stream);
        var sampleProvider = _audioStream.ToSampleProvider();
        IsStereoAudio = sampleProvider.WaveFormat.Channels == 2;
        if (IsStereoAudio)
        {
            _stereoToMono  = new StereoToMonoSampleProvider(sampleProvider)
            {
                LeftVolume = GetChannelVolume(IsLeftChannelActive),
                RightVolume = GetChannelVolume(IsRightChannelActive)
            };
            _currentProvider = _stereoToMono;
        }
        else
        {
            _currentProvider = sampleProvider;
        }

        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_currentProvider);

        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _playbackTimer.Tick += (s, e) =>
        {
            if (_audioStream != null)
                PlaybackPositionUpdated?.Invoke(this, _audioStream.CurrentTime.TotalSeconds);
        };

        TotalTimeChanged?.Invoke(this, _audioStream.TotalTime.TotalSeconds);
    }

    private float GetChannelVolume(bool active)
        => active ? 1.0f : 0.0f;

    /// <summary>
    /// Включение/выключение левого канала
    /// </summary>
    public void ToggleLeftChannel()
    {
        if (!IsStereoAudio)
            return;

        IsLeftChannelActive = !IsLeftChannelActive;
        UpdateChannelMix();
    }

    /// <summary>
    /// Включение/выключение правого канала
    /// </summary>
    public void ToggleRightChannel()
    {
        if (!IsStereoAudio)
            return;

        IsRightChannelActive = !IsRightChannelActive;
        UpdateChannelMix();
    }

    /// <summary>
    /// Функция переключения левого и правого каналов для стерео файлов
    /// </summary>
    private void UpdateChannelMix()
    {
        if (_stereoToMono  == null)
            return;

        bool wasPlaying = IsPlaying;
        if (wasPlaying)
            _outputDevice?.Stop();

        _stereoToMono.LeftVolume = GetChannelVolume(IsLeftChannelActive);
        _stereoToMono.RightVolume = GetChannelVolume(IsRightChannelActive);

        if (wasPlaying)
            _outputDevice?.Play();
    }

    /// <summary>
    /// Запускает воспроизведение в случае если оно не запущенно
    /// и ставит на паузу в случае если оно запущено
    /// </summary>
    public void PlayOrPause()
    {
        if (_audioStream == null || _outputDevice == null)
            return;

        if (IsPlaying)
        {
            _outputDevice.Stop();
            _playbackTimer?.Stop();
        }
        else
        {
            _playbackTimer?.Start();
            _outputDevice.Play();
        }

        PlaybackStateChanged?.Invoke(this, IsPlaying);
    }

    /// <summary>
    /// Останавливает воспроизведение и возвращается к 0
    /// </summary>
    public void Stop()
    {
        if (_outputDevice!= null && IsPlaying)
            _outputDevice.Stop();

        _playbackTimer?.Stop();
        if (_audioStream != null)
            _audioStream.CurrentTime = TimeSpan.Zero;

        PlaybackStateChanged?.Invoke(this, IsPlaying);
        PlaybackPositionUpdated?.Invoke(this, 0);
    }

    /// <summary>
    /// Устанавливает громкость воспроизведения
    /// </summary>
    /// <param name="volume">Значение от 0 до 1</param>
    public void SetVolume(double volume)
    {
        if (_outputDevice != null)
        {
            _outputDevice.Volume = (float)Math.Clamp(volume, 0, 1);
        }
    }

    /// <summary>
    /// Устанавливает тайминг текущего воспроизведения
    /// </summary>
    /// <param name="time">Время в секундах</param>
    public void Seek(double time)
    {
        if (_audioStream != null)
        {
            _audioStream.CurrentTime = TimeSpan.FromSeconds(time);
            PlaybackPositionUpdated?.Invoke(this, time);
        }
    }

    /// <summary>
    /// Сбрасывает текущий файл, таймер и WaveOutEvent
    /// </summary>
    public void Dispose()
    {
        IsLeftChannelActive = true;
        IsRightChannelActive = true;
        _outputDevice?.Stop();
        _outputDevice?.Dispose();
        _outputDevice = null;

        _audioStream?.Dispose();
        _audioStream = null;

        _playbackTimer?.Stop();
        _playbackTimer = null;
    }

}