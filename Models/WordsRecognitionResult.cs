// Copyright (C) Neurosoft

using System.Collections.Generic;
using System.Linq;

namespace SpeechMarkupEditor.Models;

public class WordsRecognitionResult
{
    public List<Series> LeftChannelSeries { get; set; } = [];
    public List<Series> RightChannelSeries { get; set; } = [];
}