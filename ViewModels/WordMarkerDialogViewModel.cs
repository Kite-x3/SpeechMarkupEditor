// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Messages;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Dialog;

namespace SpeechMarkupEditor.ViewModels;

public partial class WordMarkerDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Шаг изменения времени при увеличении/уменьшении
    /// </summary>
    private const double TIME_INCREMENT = 0.05;

    /// <summary>
    /// Формат строкового представления времени
    /// </summary>
    private const string TIME_FORMAT = @"mm\:ss\.ff";
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Слово маркера
    /// </summary>
    [ObservableProperty]
    private string _word = string.Empty;

    /// <summary>
    /// Время начала слова
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartTimeString))]
    private double _startTime;

    /// <summary>
    /// Время окончания слова
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndTimeString))]
    private double _endTime;

    /// <summary>
    /// Выбранное нахождение слова
    /// </summary>
    [ObservableProperty]
    private EarType _earType;

    /// <summary>
    /// Выбранный элемент нахождения слова
    /// </summary>
    [ObservableProperty]
    private EarTypeItem _selectedEarType;

    public WordMarkerDialogViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        SelectedEarType = EarTypes[0];
    }

    /// <summary>
    /// Местонахождение слова
    /// </summary>
    public List<EarTypeItem> EarTypes { get; } =
    [
        new EarTypeItem { DisplayName = Resources.NonDichoticTest, Value = EarType.NonDichotic },
        new EarTypeItem { DisplayName = Resources.LeftEar, Value = EarType.Left },
        new EarTypeItem { DisplayName = Resources.RightEar, Value = EarType.Right },
    ];

    /// <summary>
    /// Начальный тайминг слова
    /// </summary>
    public string StartTimeString
    {
        get => TimeSpan.FromSeconds(StartTime).ToString(TIME_FORMAT);
        set
        {
            if (TimeSpan.TryParseExact(value, TIME_FORMAT, null, out var timeSpan))
            {
                StartTime = timeSpan.TotalSeconds;
            }
        }
    }

    /// <summary>
    /// Конечный тайминг слова
    /// </summary>
    public string EndTimeString
    {
        get => TimeSpan.FromSeconds(EndTime).ToString(TIME_FORMAT);
        set
        {
            if (TimeSpan.TryParseExact(value, TIME_FORMAT, null, out var timeSpan))
            {
                EndTime = timeSpan.TotalSeconds;
            }
        }
    }

    /// <summary>
    /// Увеличение времени начала слова
    /// </summary>
    [RelayCommand]
    private void IncrementStartTime()
        => StartTime += TIME_INCREMENT;

    /// <summary>
    /// Уменьшение времени начала слова
    /// </summary>
    [RelayCommand]
    private void DecrementStartTime()
        => StartTime = Math.Max(0, StartTime - TIME_INCREMENT);

    /// <summary>
    /// Увеличение времени окончания слова
    /// </summary>
    [RelayCommand]
    private void IncrementEndTime()
        => EndTime += TIME_INCREMENT;

    /// <summary>
    /// Уменьшение времени окончания слова
    /// </summary>
    [RelayCommand]
    private void DecrementEndTime()
        => EndTime = Math.Max(0, EndTime - TIME_INCREMENT);

    /// <summary>
    /// Команда подтверждения добавления нового маркера слова
    /// </summary>
    [RelayCommand]
    private void Submit()
    {
        if (!Validate())
            return;

        var args = new WordMarkerSubmittedEventArgs(Word, StartTime, EndTime, EarType);
        WeakReferenceMessenger.Default.Send(new WordMarkerSubmittedMessage(args));
        WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
    }

    /// <summary>
    /// Команда отмены диалога на добавление маркера слова
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
    }

    /// <summary>
    /// Обработчик выбора местонахождения слова
    /// </summary>
    /// <param name="value">Выбранный элемент местонахождения аудио</param>
    partial void OnSelectedEarTypeChanged(EarTypeItem value)
    {
        EarType = value?.Value ?? EarType.NonDichotic;
    }

    /// <summary>
    /// Валидация данных полученных из диалога
    /// </summary>
    /// <returns>Возвращает true в случае отсутствия ошибок, иначе false</returns>
    private bool Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Word))
        {
            errors.Add(Resources.WordRequired);
        }

        if (StartTime < 0)
        {
            errors.Add(Resources.NegativeStartTime);
        }

        if (EndTime < 0)
        {
            errors.Add(Resources.NegativeEndTime);
        }

        if (StartTime >= EndTime)
        {
            errors.Add(Resources.StartAfterEnd);
        }

        if (errors.Count == 0)
            return true;

        _dialogService.ShowErrorAsync(string.Join("\n", errors));
        return false;

    }
}