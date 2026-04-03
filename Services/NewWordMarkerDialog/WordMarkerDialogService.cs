// Copyright (C) Neurosoft

using System;
using System.Linq;
using SpeechMarkupEditor.Models;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SpeechMarkupEditor.Messages;
using SpeechMarkupEditor.ViewModels;
using SpeechMarkupEditor.Views;

namespace SpeechMarkupEditor.Services.NewWordMarkerDialog;

public class WordMarkerDialogService : IWordMarkerDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public WordMarkerDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Вызов диалога добавления нового маркера слова
    /// </summary>
    /// <param name="startTime">Стартовое время маркера</param>
    /// <returns></returns>
    public async Task<WordMarkerSubmittedEventArgs?> ShowAddWordMarkerDialog(double startTime)
    {
        return await WeakReferenceMessenger.Default.Send(
            new WordMarkerDialogRequestMessage(startTime));
    }
}