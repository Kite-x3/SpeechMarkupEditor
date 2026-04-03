// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using Avalonia;
using SpeechMarkupEditor.Infrastructure.Audio;

namespace SpeechMarkupEditor.Models;

public class WaveformEventArgs : EventArgs
{
    public List<Point> Points { get; set; } =  new List<Point>();
}