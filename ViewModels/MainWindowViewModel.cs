﻿// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Audio;
using SpeechMarkupEditor.Infrastructure.AudioSourceProviderFactory;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Audio;
using SpeechMarkupEditor.Services.AudioVisualization;
using SpeechMarkupEditor.Services.Dialog;
using SpeechMarkupEditor.Services.ExportService;
using SpeechMarkupEditor.Services.ImportService;
using SpeechMarkupEditor.Services.NewWordMarkerDialog;
using SpeechMarkupEditor.Services.SpeechRecognition;
using SpeechMarkupEditor.Services.WordSeries;

namespace SpeechMarkupEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly IAudioSourceProviderFactory _sourceProviderFactory;
    private readonly IServiceScope _audioServiceScope;
    private readonly IAudioVisualizationService _visualizationService;
    private readonly IWordMarkerDialogService _wordMarkerDialogService;
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly IWordSeriesService _wordSeriesService;
    private readonly IExportService _exportService;
    private readonly IImportService _importService;
    private IAudioService? _audioService;
    private IAudioSourceProvider? _currentAudioSource;
    private string _fullFilePath = string.Empty;
    private bool _disposed;
    private CancellationTokenSource? _recognitionCts;

    /// <summary>
    /// Имя выбранного файла
    /// </summary>
    [ObservableProperty]
    private string _selectedFileName = string.Empty;

    /// <summary>
    /// Флаг наличия выбранного файла
    /// </summary>
    [ObservableProperty]
    private bool _isFileSelected;

    [ObservableProperty]
    private bool _hasAudioLoaded;

    /// <summary>
    /// Громкость воспроизведения
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VolumePercentage))]
    private double _volume = 0.3;

    /// <summary>
    /// Текущая позиция воспроизведения
    /// </summary>
    [ObservableProperty]
    private double _currentTimeSeconds;

    /// <summary>
    /// Общая длительность аудиофайла
    /// </summary>
    [ObservableProperty]
    private double _totalTimeSeconds;

    /// <summary>
    /// Флаг состояния воспроизведения
    /// </summary>
    [ObservableProperty]
    private bool _isPlaying;

    /// <summary>
    /// Коллекция серий слов для левого канала
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Series> _leftSeries;

    /// <summary>
    /// Коллекция серий слов для правого канала
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Series> _rightSeries;

    /// <summary>
    /// Указывает, активен ли левый аудиоканал в данный момент
    /// </summary>
    [ObservableProperty]
    private bool _isLeftChannelActive = true;

    /// <summary>
    /// Указывает, активен ли правый аудиоканал в данный момент
    /// </summary>
    [ObservableProperty]
    private bool _isRightChannelActive = true;

    /// <summary>
    /// Определяет, является ли текущий аудиофайл стерео (true) или моно (false)
    /// </summary>
    [ObservableProperty]
    private bool _isStereoAudio = false;

    /// <summary>
    /// Флаг обработки аудио
    /// </summary>
    [ObservableProperty]
    private bool _isProcessingAudio;

    [ObservableProperty]
    private bool _isNonDichoticRecognition;

    [ObservableProperty]
    private int _leftSeriesColumnSpan = 1;

    public double VolumePercentage => Volume * 100;

    public MainWindowViewModel(){}

    public MainWindowViewModel(IDialogService dialogService, IAudioSourceProviderFactory sourceProviderFactory,
        IServiceProvider serviceProvider, IAudioVisualizationService visualizationService, IWordMarkerDialogService wordMarkerDialogService,
        ISpeechRecognitionService speechRecognitionService, IWordSeriesService wordSeriesService, IExportService exportService,
        IImportService importService)
    {
        _dialogService = dialogService;
        _sourceProviderFactory = sourceProviderFactory;
        _audioServiceScope = serviceProvider.CreateScope();
        _visualizationService = visualizationService;
        _wordMarkerDialogService = wordMarkerDialogService;
        _speechRecognitionService = speechRecognitionService;
        _wordSeriesService = wordSeriesService;
        _leftSeries = new ObservableCollection<Series>();
        _rightSeries = new ObservableCollection<Series>();
        _exportService = exportService;
        _importService = importService;
    }

    /// <summary>
    /// Обработчик обновления текущей позиции воспроизведения
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="position">Текущая позиция в секундах</param>
    private void OnPlaybackPositionUpdated(object? sender, double position)
    {
        CurrentTimeSeconds = position;
    }

    /// <summary>
    /// Обработчик изменения общей длительности трека
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="totalTime">Общая длительность в секундах</param>
    private void OnTotalTimeChanged(object? sender, double totalTime)
    {
        TotalTimeSeconds = totalTime;
    }

    /// <summary>
    /// Обработчик изменения состояния воспроизведения
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="isPlaying">Флаг активности воспроизведения</param>
    private void OnPlaybackStateChanged(object? sender, bool isPlaying)
    {
        if (_audioService != null)
        {
            IsPlaying = isPlaying;
        }
    }

    /// <summary>
    /// Обработчик изменения текущей позиции воспроизведения
    /// </summary>
    partial void OnCurrentTimeSecondsChanged(double value)
    {
        _audioService?.Seek(value);
    }

    /// <summary>
    /// Обработчик изменения громкости
    /// </summary>
    partial void OnVolumeChanged(double volume)
    {
        _audioService?.SetVolume(volume);
    }

    public void OnWaveformUpdated(object? sender, WaveformEventArgs waveformEventArgs)
    {

    }

    /// <summary>
    /// Команда выбора wav файла
    /// </summary>
    [RelayCommand]
    private async Task SelectWavFile()
    {
        try
        {
            var source = await _sourceProviderFactory.CreateSourceAsync();
            if (source == null)
                return;

            _recognitionCts?.Cancel();
            _currentAudioSource = source;

            await InitializeAudioService(source);
            SelectedFileName = source.DisplayName;
            IsFileSelected = true;
            HasAudioLoaded = true;
            await _visualizationService.UpdateVisualizationAsync(source);
            await RunRecognitionAsync(source);
        }

        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(Resources.FileChoosingError);

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Инициализирует аудио сервис с указанным источником
    /// </summary>
    /// <param name="sourceProvider">Источник аудиоданных</param>
    private async Task InitializeAudioService(IAudioSourceProvider sourceProvider)
    {
        CleanupAudioService();

        _audioService = _audioServiceScope.ServiceProvider.GetRequiredService<IAudioService>();
        _audioService.PlaybackPositionUpdated += OnPlaybackPositionUpdated;
        _audioService.TotalTimeChanged += OnTotalTimeChanged;
        _audioService.PlaybackStateChanged += OnPlaybackStateChanged;
        await _audioService.Initialize(sourceProvider);
        _audioService.SetVolume(Volume);
        CurrentTimeSeconds = 0;
        IsPlaying = false;
        HasAudioLoaded = true;
        IsStereoAudio = _audioService.IsStereoAudio;
        IsLeftChannelActive =  _audioService.IsLeftChannelActive;
        IsRightChannelActive = _audioService.IsRightChannelActive;
    }

    /// <summary>
    /// Очищает текущий аудио сервис и освобождает ресурсы
    /// </summary>
    private void CleanupAudioService()
    {
        if (_audioService is null)
            return;

        _audioService.PlaybackPositionUpdated -= OnPlaybackPositionUpdated;
        _audioService.TotalTimeChanged -= OnTotalTimeChanged;
        _audioService.PlaybackStateChanged -= OnPlaybackStateChanged;

        _audioService.Dispose();
        _audioService = null;
        HasAudioLoaded = false;

    }

    [RelayCommand]
    private async Task ImportMarkup()
    {
        var importedMarkup = await _importService.ImportAsync();
        if (importedMarkup == null)
            return;

        _recognitionCts?.Cancel();
        _currentAudioSource = null;
        CleanupAudioService();

        LeftSeries.Clear();
        RightSeries.Clear();

        foreach (var series in importedMarkup.LeftChannel)
        {
            LeftSeries.Add(series);
        }

        foreach (var series in importedMarkup.RightChannel)
        {
            RightSeries.Add(series);
        }

        UpdateRecognitionPresentationMode();

        SelectedFileName = importedMarkup.FileName;
        IsFileSelected = true;
        HasAudioLoaded = false;
        CurrentTimeSeconds = 0;
        TotalTimeSeconds = 0;
        IsPlaying = false;
        IsStereoAudio = false;
        IsLeftChannelActive = true;
        IsRightChannelActive = true;
    }

    [RelayCommand]
    private void Export()
    {
        if (LeftSeries.Count == 0 && RightSeries.Count == 0)
        {
            _dialogService.ShowErrorAsync(Resources.NothingToExport);
            return;
        }

        try
        {
            _exportService.ExportAsync(RightSeries, LeftSeries, SelectedFileName);
        }
        catch (Exception ex)
        {
            _dialogService.ShowErrorAsync($"{Resources.ExportError}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CancelRecognition()
    {
        if (!IsProcessingAudio || _recognitionCts == null)
            return;

        var shouldCancel = await _dialogService.ShowConfirmationAsync(
            Resources.Warning,
            Resources.StopRecognitionConfirmation,
            Resources.StopRecognition,
            Resources.ContinueRecognition);

        if (!shouldCancel)
            return;

        _recognitionCts?.Cancel();
    }

    [RelayCommand]
    private async Task RestartRecognition()
    {
        if (!HasAudioLoaded || _currentAudioSource == null || IsProcessingAudio)
            return;

        await RunRecognitionAsync(_currentAudioSource);
    }

    private async Task RunRecognitionAsync(IAudioSourceProvider source)
    {
        _recognitionCts?.Cancel();
        _recognitionCts?.Dispose();
        _recognitionCts = new CancellationTokenSource();

        var cancellationToken = _recognitionCts.Token;
        LeftSeries.Clear();
        RightSeries.Clear();
        IsProcessingAudio = true;

        try
        {
            var recognitionResult = await _speechRecognitionService
                .RecognizeAsync(source, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            _wordSeriesService.MergeRecognitionResult(LeftSeries, recognitionResult.LeftChannelSeries);
            _wordSeriesService.MergeRecognitionResult(RightSeries, recognitionResult.RightChannelSeries);
            UpdateRecognitionPresentationMode();
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected and confirmed by user.
        }
        finally
        {
            IsProcessingAudio = false;
            UpdateRecognitionPresentationMode();
        }
    }

    /// <summary>
    /// Команда переключения воспроизведения/паузы
    /// </summary>
    [RelayCommand]
    private void TogglePlayPause()
    {
        _audioService?.PlayOrPause();
    }

    /// <summary>
    /// Команда остановки воспроизведения
    /// </summary>
    [RelayCommand]
    private void Stop()
    {
        _audioService?.Stop();
    }

    /// <summary>
    /// Команда добавления маркера нового слова
    /// </summary>
    /// <param name="position">Позиция маркера</param>
    [RelayCommand]
    private async Task AddWordMarker(double position)
    {
        var marker = await _wordMarkerDialogService.ShowAddWordMarkerDialog(position);
        if (marker != null)
            AddWordToCollection(marker);
    }

    /// <summary>
    /// Добавление нового маркера слова
    /// </summary>
    /// <param name="marker">Маркер слова</param>
    private void AddWordToCollection(WordMarkerSubmittedEventArgs marker)
    {
        var word = new WordTimestamp(marker.Word, marker.StartTime, marker.EndTime);

        switch (marker.EarType)
        {
            case EarType.Left:
                _wordSeriesService.AddWordToSeries(LeftSeries, word);
                break;

            case EarType.Right:
                _wordSeriesService.AddWordToSeries(RightSeries, word);
                break;

            case EarType.NonDichotic:
                _wordSeriesService.AddWordToSeries(LeftSeries, word);
                _wordSeriesService.AddWordToSeries(RightSeries, word);
                break;
        }

        UpdateRecognitionPresentationMode();
    }

    [RelayCommand]
    private async Task RemoveLeftWord(WordTimestamp word)
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.Warning,
            string.Format(Resources.DeleteWordLeftChannelFormat, word.Word),
            Resources.Delete,
            Resources.Cancel);

        if (!confirmed)
            return;

        _wordSeriesService.RemoveWordFromSeries(LeftSeries, word);
        UpdateRecognitionPresentationMode();
    }

    [RelayCommand]
    private async Task RemoveRightWord(WordTimestamp word)
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.Warning,
            string.Format(Resources.DeleteWordRightChannelFormat, word.Word),
            Resources.Delete,
            Resources.Cancel);

        if (!confirmed)
            return;

        _wordSeriesService.RemoveWordFromSeries(RightSeries, word);
        UpdateRecognitionPresentationMode();
    }

    private void UpdateRecognitionPresentationMode()
    {
        IsNonDichoticRecognition = AreSeriesCollectionsEqual(LeftSeries, RightSeries);
        LeftSeriesColumnSpan = IsNonDichoticRecognition ? 2 : 1;
    }

    private static bool AreSeriesCollectionsEqual(IReadOnlyList<Series> left, IReadOnlyList<Series> right)
    {
        if (left.Count == 0 && right.Count == 0)
            return false;

        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            var leftWords = left[i].Words;
            var rightWords = right[i].Words;

            if (leftWords.Count != rightWords.Count)
                return false;

            for (var j = 0; j < leftWords.Count; j++)
            {
                var l = leftWords[j];
                var r = rightWords[j];

                if (!string.Equals(l.Word, r.Word, StringComparison.Ordinal))
                    return false;

                if (Math.Abs(l.StartTime - r.StartTime) > 0.001)
                    return false;

                if (Math.Abs(l.EndTime - r.EndTime) > 0.001)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Команда включения/выключения левого канала
    /// </summary>
    [RelayCommand]
    private void ToggleLeftChannel()
    {
        if (_audioService == null)
            return;

        _audioService.ToggleLeftChannel();
        IsLeftChannelActive = _audioService.IsLeftChannelActive;
    }

    /// <summary>
    /// Команда включения/выключения правого канала
    /// </summary>
    [RelayCommand]
    private void ToggleRightChannel()
    {
        if (_audioService == null)
            return;

        _audioService.ToggleRightChannel();
        IsRightChannelActive = _audioService.IsRightChannelActive;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        CleanupAudioService();
        _audioServiceScope.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

}
