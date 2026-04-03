// Copyright (C) Neurosoft

using CommunityToolkit.Mvvm.Messaging.Messages;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Messages;

public class WordMarkerDialogRequestMessage : AsyncRequestMessage<WordMarkerSubmittedEventArgs?>
{
    public WordMarkerDialogRequestMessage(double startTime)
    {
        StartTime = startTime;
    }

    public double StartTime { get; }
}