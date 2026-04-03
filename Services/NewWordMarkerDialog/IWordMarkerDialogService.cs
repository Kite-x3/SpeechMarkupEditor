// Copyright (C) Neurosoft

using System.Threading.Tasks;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.NewWordMarkerDialog;

public interface IWordMarkerDialogService
{
    Task<WordMarkerSubmittedEventArgs?> ShowAddWordMarkerDialog(double startTime);
}