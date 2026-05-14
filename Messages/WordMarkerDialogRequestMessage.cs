// Copyright (C) Neurosoft

using CommunityToolkit.Mvvm.Messaging.Messages;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Messages;

public class WordMarkerDialogRequestMessage : AsyncRequestMessage<WordMarkerSubmittedEventArgs?>
{
    public WordMarkerDialogRequestMessage(double startTime, WordMarkerSubmittedEventArgs? existingMarker = null)
    {
        StartTime = startTime;
        ExistingMarker = existingMarker;
    }

    public double StartTime { get; }

    public WordMarkerSubmittedEventArgs? ExistingMarker { get; }
}