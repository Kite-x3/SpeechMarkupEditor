// Copyright (C) Neurosoft

using System;
using Avalonia.Threading;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.Wave.SampleProviders;
using SpeechMarkupEditor.Infrastructure.Audio;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.Audio;

public class AudioService : IAudioService
{
    private WaveOutEvent? _outputDevice;
    private WaveStream? _audioStream;
    private DispatcherTimer? _playbackTimer;
    private ISampleProvider? _currentProvider;
    private StereoToMonoSampleProvider? _stereoToMono;
    private DispatcherTimer? _segmentTimer;
    private double _segmentEndTime;
    private double _segmentStartTime;
    private bool _isSegmentMode;
    private WordTimestamp? _currentSegmentWord;
    private bool _savedLeftChannelState;
    private bool _savedRightChannelState;

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

    public void PlaySegment(WordTimestamp word)
    {
        if (_audioStream == null || _outputDevice == null)
            return;

        if (_isSegmentMode &&
            Math.Abs(_segmentStartTime - word.StartTime) < 0.01 &&
            Math.Abs(_segmentEndTime - word.EndTime) < 0.01)
        {
            ToggleSegmentPause();
            return;
        }

        StartNewSegment(word);
    }

    private float GetChannelVolume(bool active)
        => active ? 1.0f : 0.0f;

    /// <summary>
    /// Включение/выключение левого канала
    /// </summary>
    public void ToggleLeftChannel()
    {
        if (!IsStereoAudio || _isSegmentMode)
            return;

        IsLeftChannelActive = !IsLeftChannelActive;
        UpdateChannelMix();
    }

    /// <summary>
    /// Включение/выключение правого канала
    /// </summary>
    public void ToggleRightChannel()
    {
        if (!IsStereoAudio || _isSegmentMode)
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

        if (_isSegmentMode)
        {
            StopSegmentInternal();
            _playbackTimer?.Start();
            _outputDevice.Play();
        }
        else if (IsPlaying)
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
        if (_isSegmentMode)
            StopSegmentInternal();
        else
        {
            if (_outputDevice != null && IsPlaying)
                _outputDevice.Stop();

            _playbackTimer?.Stop();
            if (_audioStream != null)
                _audioStream.CurrentTime = TimeSpan.Zero;
        }

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
        _isSegmentMode = false;
        _segmentTimer?.Stop();

        _audioStream?.Dispose();
        _audioStream = null;

        _playbackTimer?.Stop();
        _playbackTimer = null;
    }

    private void SegmentTimer_Tick(object? sender, EventArgs e)
    {
        if (_audioStream == null || _outputDevice == null)
            return;

        double current = _audioStream.CurrentTime.TotalSeconds;
        PlaybackPositionUpdated?.Invoke(this, current);

        if (current >= _segmentEndTime)
        {
            StopSegment();
        }
    }

    private void StopSegment()
    {
        StopSegmentInternal();
        if (_audioStream != null)
            _audioStream.CurrentTime = TimeSpan.FromSeconds(_segmentStartTime);

        PlaybackPositionUpdated?.Invoke(this, _segmentStartTime);
    }

    private void StopSegmentInternal()
    {
        if (_currentSegmentWord != null)
        {
            _currentSegmentWord.IsPlaying = false;
            _currentSegmentWord = null;
        }

        _segmentTimer?.Stop();
        if (_outputDevice != null && IsPlaying)
            _outputDevice.Stop();

        _playbackTimer?.Stop();
        _isSegmentMode = false;

        RestoreChannelState();

        PlaybackStateChanged?.Invoke(this, false);
    }

    private void StartNewSegment(WordTimestamp word)
    {
        StopSegmentInternal();

        _currentSegmentWord = word;
        _segmentStartTime = word.StartTime;
        _segmentEndTime = word.EndTime;
        _isSegmentMode = true;

        ApplyWordChannel(word);
        Seek(_segmentStartTime);

        word.IsPlaying = true;
        _outputDevice?.Play();
        _playbackTimer?.Start();

        PlaybackStateChanged?.Invoke(this, true);

        StartSegmentTimer();
    }

    private void ApplyWordChannel(WordTimestamp word)
    {
        if (_stereoToMono == null)
            return;

        _savedLeftChannelState = IsLeftChannelActive;
        _savedRightChannelState = IsRightChannelActive;
        bool leftEnabled = false;
        bool rightEnabled = false;

        switch (word.Channel)
        {
            case EarType.Left:
                leftEnabled = true;
                break;

            case EarType.Right:
                rightEnabled = true;
                break;

            case EarType.NonDichotic:
            default:
                leftEnabled = true;
                rightEnabled = true;
                break;
        }

        leftEnabled &= IsLeftChannelActive;
        rightEnabled &= IsRightChannelActive;

        _stereoToMono.LeftVolume = GetChannelVolume(leftEnabled);
        _stereoToMono.RightVolume = GetChannelVolume(rightEnabled);
    }

    private void RestoreChannelState()
    {
        if (_stereoToMono == null)
            return;

        _stereoToMono.LeftVolume = GetChannelVolume(_savedLeftChannelState);
        _stereoToMono.RightVolume = GetChannelVolume(_savedRightChannelState);
    }

    private void ToggleSegmentPause()
    {
        if (_outputDevice == null)
            return;

        if (IsPlaying)
        {
            _outputDevice.Stop();
            _playbackTimer?.Stop();

            if (_currentSegmentWord != null)
                _currentSegmentWord.IsPlaying = false;
        }
        else
        {
            _outputDevice.Play();
            _playbackTimer?.Start();

            if (_currentSegmentWord != null)
                _currentSegmentWord.IsPlaying = true;
        }

        PlaybackStateChanged?.Invoke(this, IsPlaying);
    }

    private void StartSegmentTimer()
    {
        _segmentTimer?.Stop();
        _segmentTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1)
        };
        _segmentTimer.Tick += SegmentTimer_Tick;
        _segmentTimer.Start();
    }
}