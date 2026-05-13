// Copyright (C) Neurosoft

using System;
using System.Threading.Tasks;
using SpeechMarkupEditor.Infrastructure.Audio;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.Audio;

public interface IAudioService
{
    bool IsPlaying { get; }
    Task Initialize(IAudioSourceProvider SourceProvider);
    void PlaySegment(WordTimestamp word);
    void PlayOrPause();
    void Stop();
    void SetVolume(double volume);
    void Seek(double time);
    void Dispose();
    void ToggleLeftChannel();
    void ToggleRightChannel();
    bool IsLeftChannelActive { get; }
    bool IsRightChannelActive { get; }
    bool IsStereoAudio { get; }
    event EventHandler<double>? PlaybackPositionUpdated;
    event EventHandler<double>? TotalTimeChanged;
    event EventHandler<bool>? PlaybackStateChanged;
}