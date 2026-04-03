// Copyright (C) Neurosoft

using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Messages;

public class WordMarkerSubmittedMessage(WordMarkerSubmittedEventArgs args)
{
    public WordMarkerSubmittedEventArgs Args { get; } = args;
}