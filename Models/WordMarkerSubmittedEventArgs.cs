// Copyright (C) Neurosoft

namespace SpeechMarkupEditor.Models;

public class WordMarkerSubmittedEventArgs(string word, double startTime, double endTime, EarType earType)
{
    public string Word { get; } = word;
    public double StartTime { get; } = startTime;
    public double EndTime { get; } = endTime;
    public EarType EarType { get; } = earType;
}