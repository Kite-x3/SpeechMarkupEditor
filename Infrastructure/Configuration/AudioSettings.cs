// Copyright (C) Neurosoft

namespace SpeechMarkupEditor.Infrastructure.Configuration;

public class AudioSettings
{
    public int RecognitionSampleRate { get; set; }
    public int BufferSize { get; set; }
    public double PauseThreshold { get; set; }
}