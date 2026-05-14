// Copyright (C) Neurosoft

using System;
using NAudio.Wave;

namespace SpeechMarkupEditor.Services.Audio;

public sealed class StereoChannelSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;

    public StereoChannelSampleProvider(ISampleProvider source)
    {
        if (source.WaveFormat.Channels != 2)
            throw new ArgumentException("Source must be stereo");

        _source = source;
    }

    public bool LeftEnabled { get; set; } = true;

    public bool RightEnabled { get; set; } = true;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        for (int i = offset; i < offset + samplesRead; i += 2)
        {
            if (!LeftEnabled)
                buffer[i] = 0f;

            if (i + 1 < offset + samplesRead && !RightEnabled)
                buffer[i + 1] = 0f;
        }

        return samplesRead;
    }
}