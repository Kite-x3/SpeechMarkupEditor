// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using SpeechMarkupEditor.Services.Localization;
using SpeechMarkupEditor.Services.MarkupHistory;
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
    private readonly IMarkupHistoryAutoSaveService _markupHistoryAutoSaveService;
    private readonly IExportService _exportService;
    private readonly IImportService _importService;
    private readonly IMarkupHistoryService _markupHistoryService;
    private readonly ILocalizationService _localizationService;
    private readonly bool _isLanguageSelectionInitialized;
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

    [ObservableProperty]
    private bool _showSeries = true;

    [ObservableProperty]
    private ObservableCollection<LanguageOption> _availableLanguages = [];

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    public double VolumePercentage => Volume * 100;

    public MainWindowViewModel(){}

    public MainWindowViewModel(IDialogService dialogService, IAudioSourceProviderFactory sourceProviderFactory,
        IServiceProvider serviceProvider, IAudioVisualizationService visualizationService, IWordMarkerDialogService wordMarkerDialogService,
        ISpeechRecognitionService speechRecognitionService, IWordSeriesService wordSeriesService, IMarkupHistoryAutoSaveService markupHistoryAutoSaveService, IExportService exportService,
        IImportService importService, IMarkupHistoryService markupHistoryService, ILocalizationService localizationService)
    {
        _dialogService = dialogService;
        _sourceProviderFactory = sourceProviderFactory;
        _audioServiceScope = serviceProvider.CreateScope();
        _visualizationService = visualizationService;
        _wordMarkerDialogService = wordMarkerDialogService;
        _speechRecognitionService = speechRecognitionService;
        _wordSeriesService = wordSeriesService;
        _markupHistoryAutoSaveService = markupHistoryAutoSaveService;
        _leftSeries = new ObservableCollection<Series>();
        _rightSeries = new ObservableCollection<Series>();
        _exportService = exportService;
        _importService = importService;
        _markupHistoryService = markupHistoryService;
        _localizationService = localizationService;
        AvailableLanguages = new ObservableCollection<LanguageOption>(_localizationService.GetAvailableLanguages());
        SelectedLanguage = AvailableLanguages.FirstOrDefault(item => item.Code == _localizationService.CurrentLanguageCode)
            ?? AvailableLanguages.FirstOrDefault();
        _isLanguageSelectionInitialized = true;
    }

    private static void RestartApplication()
    {
        string? processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = true
        });

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }


    private static bool AreSeriesCollectionsEqual(IReadOnlyList<Series> left, IReadOnlyList<Series> right)
    {
        if (left.Count == 0 && right.Count == 0)
            return false;

        if (left.Count != right.Count)
            return false;

        for (int i = 0; i < left.Count; i++)
        {
            var leftWords = left[i].Words;
            var rightWords = right[i].Words;

            if (leftWords.Count != rightWords.Count)
                return false;

            for (int j = 0; j < leftWords.Count; j++)
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
    /// Обработчик изменения текущей позиции воспроизведения
    /// </summary>
    partial void OnCurrentTimeSecondsChanged(double value)
    {
        _audioService?.Seek(value);
    }

    /// <summary>
    /// Обработчик изменения громкости
    /// </summary>
    partial void OnVolumeChanged(double value)
    {
        _audioService?.SetVolume(value);
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
        IAudioSourceProvider? source;

        try
        {
            source = await _sourceProviderFactory.CreateSourceAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(Resources.FileChoosingError);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine($"Error: {ex.Message}");
            return;
        }

        if (source == null)
            return;

        _recognitionCts?.Cancel();
        _currentAudioSource = source;
        _fullFilePath = source.SourcePath ?? string.Empty;
        LeftSeries.Clear();
        RightSeries.Clear();
        UpdateRecognitionPresentationMode();

        await InitializeAudioService(source);
        SelectedFileName = source.DisplayName;
        IsFileSelected = true;
        HasAudioLoaded = true;
        await _visualizationService.UpdateVisualizationAsync(source);
        await RunRecognitionAsync(source);
    }

    [RelayCommand]
    private async Task SelectMissingAudioFile()
    {
        try
        {
            var source = await _sourceProviderFactory.CreateSourceAsync();

            if (source == null)
                return;

            _currentAudioSource = source;

            _fullFilePath = source.SourcePath ?? string.Empty;

            await InitializeAudioService(source);

            SelectedFileName = source.DisplayName;

            IsFileSelected = true;
            HasAudioLoaded = true;

            await _visualizationService
                .UpdateVisualizationAsync(source);
            ScheduleHistorySave();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                $"{Resources.Error}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportMarkup()
    {
        var importedMarkup = await _importService.ImportAsync();
        if (importedMarkup == null)
            return;

        _recognitionCts?.Cancel();
        _markupHistoryAutoSaveService.CancelPendingSave();
        ApplyImportedMarkup(importedMarkup);

        await LoadAudioAsync(importedMarkup.SourcePath);

        ScheduleHistorySave();
    }

    [RelayCommand]
    private async Task Export()
    {
        if (LeftSeries.Count == 0 && RightSeries.Count == 0)
        {
            await _dialogService.ShowErrorAsync(Resources.NothingToExport);
            return;
        }

        try
        {
            if (!await PrepareMarkupForSaveAsync())
                return;

            await _exportService.ExportAsync(LeftSeries, RightSeries, SelectedFileName, _fullFilePath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"{Resources.ExportError}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CancelRecognition()
    {
        if (!IsProcessingAudio || _recognitionCts == null)
            return;

        bool shouldCancel = await _dialogService.ShowConfirmationAsync(
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

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (!_isLanguageSelectionInitialized || value == null || value.Code == _localizationService.CurrentLanguageCode)
            return;

        _ = ApplyLanguageInternalAsync(value.Code);
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
        WordMarkerSubmittedEventArgs? marker = null;

        while (true)
        {
            marker = await _wordMarkerDialogService.ShowAddWordMarkerDialog(position, marker);

            if (marker == null)
                return;

            var overlaps = AddWordToCollection(marker);

            if (overlaps.Count == 0)
                break;

            var builder = new StringBuilder();

            builder.AppendLine(Resources.OverlapsWithWords);
            builder.AppendLine();

            foreach (var word in overlaps)
            {
                builder.AppendLine(
                    $"- '{word.Word}' " +
                    $"({word.StartTime:F2}-{word.EndTime:F2})");
            }

            await _dialogService.ShowWarningAsync(
                builder.ToString());
        }

        await _markupHistoryAutoSaveService
            .FlushPendingSaveAsync();

        await SaveCurrentMarkupToHistoryInternalAsync(false);
    }

    [RelayCommand]
    private void PlayWordSegment(WordTimestamp word)
    {
        _audioService?.PlaySegment(word);
    }

    [RelayCommand]
    private async Task RemoveLeftWord(WordTimestamp word)
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.Warning,
            string.Format(Resources.DeleteWordLeftChannelFormat, word.Word),
            Resources.Delete,
            Resources.Cancel);

        if (!confirmed)
            return;

        _wordSeriesService.RemoveWordFromSeries(LeftSeries, word);
        UpdateRecognitionPresentationMode();
        await _markupHistoryAutoSaveService.FlushPendingSaveAsync();
        await SaveCurrentMarkupToHistoryInternalAsync(false);
    }

    [RelayCommand]
    private async Task RemoveRightWord(WordTimestamp word)
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            Resources.Warning,
            string.Format(Resources.DeleteWordRightChannelFormat, word.Word),
            Resources.Delete,
            Resources.Cancel);

        if (!confirmed)
            return;

        _wordSeriesService.RemoveWordFromSeries(RightSeries, word);
        UpdateRecognitionPresentationMode();
        await _markupHistoryAutoSaveService.FlushPendingSaveAsync();
        await SaveCurrentMarkupToHistoryInternalAsync(false);
    }

    [RelayCommand]
    private async Task SaveMarkupToHistory()
    {
        if (LeftSeries.Count == 0 && RightSeries.Count == 0)
        {
            await _dialogService.ShowWarningAsync(Resources.NothingToSaveToHistory);
            return;
        }

        if (!await PrepareMarkupForSaveAsync())
            return;

        await _markupHistoryAutoSaveService.FlushPendingSaveAsync();
        await SaveCurrentMarkupToHistoryInternalAsync(showSuccessMessage: true);
    }

    public async Task LoadMarkupHistoryAsync(long id)
    {
        var importedMarkup = await _markupHistoryService.LoadAsync(id);
        if (importedMarkup == null)
        {
            await _dialogService.ShowWarningAsync(Resources.HistoryEntryNotFound);
            return;
        }

        _recognitionCts?.Cancel();
        _markupHistoryAutoSaveService.CancelPendingSave();
        ApplyImportedMarkup(importedMarkup);

        await LoadAudioAsync(importedMarkup.SourcePath);
    }

    private void UpdateRecognitionPresentationMode()
    {
        IsNonDichoticRecognition = AreSeriesCollectionsEqual(LeftSeries, RightSeries);
        LeftSeriesColumnSpan = IsNonDichoticRecognition ? 2 : 1;
    }

    private void ApplyImportedMarkup(ImportedMarkup importedMarkup)
    {
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
        _fullFilePath = importedMarkup.SourcePath ?? string.Empty;
        IsFileSelected = true;
        HasAudioLoaded = false;
        CurrentTimeSeconds = 0;
        TotalTimeSeconds = 0;
        IsPlaying = false;
        IsStereoAudio = false;
        IsLeftChannelActive = true;
        IsRightChannelActive = true;
    }

    private async Task SaveCurrentMarkupToHistoryInternalAsync(bool showSuccessMessage = false)
    {
        if (LeftSeries.Count == 0 && RightSeries.Count == 0)
            return;

        await _markupHistoryAutoSaveService.SaveNowAsync(SelectedFileName, LeftSeries, RightSeries, _fullFilePath);

        if (showSuccessMessage)
            await _dialogService.ShowSuccessAsync(Resources.MarkupSavedToHistory);
    }

    private async Task<bool> PrepareMarkupForSaveAsync()
    {
        string? overlapMessage = BuildSaveOverlapMessage();
        if (!string.IsNullOrWhiteSpace(overlapMessage))
        {
            return await _dialogService.ShowConfirmationAsync(
                Resources.Warning,
                overlapMessage,
                Resources.SaveDespiteOverlaps,
                Resources.Cancel);
        }

        _wordSeriesService.RebuildSeriesCollection(LeftSeries);
        _wordSeriesService.RebuildSeriesCollection(RightSeries);
        UpdateRecognitionPresentationMode();
        ScheduleHistorySave();
        return true;
    }

    private string? BuildSaveOverlapMessage()
    {
        string? leftOverlapWarning = _wordSeriesService.GetOverlapWarning(LeftSeries);
        string? rightOverlapWarning = _wordSeriesService.GetOverlapWarning(RightSeries);

        if (string.IsNullOrWhiteSpace(leftOverlapWarning) && string.IsNullOrWhiteSpace(rightOverlapWarning))
            return null;

        var builder = new StringBuilder();
        builder.AppendLine(Resources.RebuildSeriesBeforeSaveFailed);
        builder.AppendLine();

        AppendChannelOverlapWarning(builder, Resources.LeftChannelLabel, leftOverlapWarning);
        AppendChannelOverlapWarning(builder, Resources.RightChannelLabel, rightOverlapWarning);

        builder.AppendLine();
        builder.Append(Resources.SaveAnywayQuestion);
        return builder.ToString();
    }

    private static void AppendChannelOverlapWarning(StringBuilder builder, string channelName, string? warning)
    {
        if (string.IsNullOrWhiteSpace(warning))
            return;

        if (builder[^1] != '\n')
            builder.AppendLine();

        builder.AppendLine($"{channelName}:");
        builder.AppendLine(warning);
    }

    public Task SaveInlineMarkupEditAsync()
    {
        ScheduleHistorySave();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RebuildSeries()
    {
        if (LeftSeries.Count == 0 && RightSeries.Count == 0)
            return;

        string? leftOverlapWarning = _wordSeriesService.GetOverlapWarning(LeftSeries);
        if (!string.IsNullOrWhiteSpace(leftOverlapWarning))
        {
            await _dialogService.ShowWarningAsync(leftOverlapWarning);
            return;
        }

        string? rightOverlapWarning = _wordSeriesService.GetOverlapWarning(RightSeries);
        if (!string.IsNullOrWhiteSpace(rightOverlapWarning))
        {
            await _dialogService.ShowWarningAsync(rightOverlapWarning);
            return;
        }

        _wordSeriesService.RebuildSeriesCollection(LeftSeries);
        _wordSeriesService.RebuildSeriesCollection(RightSeries);
        UpdateRecognitionPresentationMode();
        await SaveCurrentMarkupToHistoryInternalAsync();
    }

    private void ScheduleHistorySave()
    {
        _markupHistoryAutoSaveService.ScheduleSave(SelectedFileName, LeftSeries, RightSeries, _fullFilePath);
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

        _markupHistoryAutoSaveService.CancelPendingSave();
        CleanupAudioService();
        _audioServiceScope.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Добавление нового маркера слова
    /// </summary>
    /// <param name="marker">Маркер слова</param>
    private List<WordTimestamp> AddWordToCollection(WordMarkerSubmittedEventArgs marker)
    {
        var overlappingWords = new List<WordTimestamp>();
        bool addToLeft = marker.EarType == EarType.Left || marker.EarType == EarType.NonDichotic;
        bool addToRight = marker.EarType == EarType.Right || marker.EarType == EarType.NonDichotic;

        if (addToLeft)
        {
            overlappingWords.AddRange(_wordSeriesService.AddWordToSeries(LeftSeries,
                new WordTimestamp(marker.Word, marker.StartTime, marker.EndTime, EarType.Left)));
        }

        if (addToRight)
        {
            overlappingWords.AddRange(_wordSeriesService.AddWordToSeries(RightSeries,
                new WordTimestamp(marker.Word, marker.StartTime, marker.EndTime, EarType.Right)));
        }

        UpdateRecognitionPresentationMode();

        return overlappingWords
            .Distinct()
            .ToList();
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


    private async Task RunRecognitionAsync(IAudioSourceProvider source)
    {
        _recognitionCts?.Cancel();
        _recognitionCts?.Dispose();
        _recognitionCts = new CancellationTokenSource();

        var cancellationToken = _recognitionCts.Token;
        IsProcessingAudio = true;

        try
        {
            var recognitionResult = await _speechRecognitionService
                .RecognizeAsync(source, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            var mergeResult = new RecognitionMergeResult();
            mergeResult.Accumulate(_wordSeriesService.MergeRecognitionResult(LeftSeries, recognitionResult.LeftChannelSeries));
            mergeResult.Accumulate(_wordSeriesService.MergeRecognitionResult(RightSeries, recognitionResult.RightChannelSeries));
            UpdateRecognitionPresentationMode();

            if (mergeResult.HasAddedWords)
                ScheduleHistorySave();

            if (mergeResult.HasOverlaps)
            {
                string? warningMessage = mergeResult.HasAddedWords
                    ? Resources.RecognitionOverlapsPartialAdded
                    : Resources.RecognitionOverlapsNothingAdded;
                await _dialogService.ShowWarningAsync(warningMessage);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected and confirmed by user.
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"{Resources.Error}: {ex.Message}");
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            IsProcessingAudio = false;
            UpdateRecognitionPresentationMode();
        }
    }


    private async Task ApplyLanguageInternalAsync(string languageCode)
    {
        _recognitionCts?.Cancel();
        await _markupHistoryAutoSaveService.FlushPendingSaveAsync();
        await _localizationService.SetLanguageAsync(languageCode);
        RestartApplication();
    }


    private async Task LoadAudioAsync(string path)
    {
        var source = _sourceProviderFactory.CreateSourceFromPath(path);

        if (source == null)
            return;

        _currentAudioSource = source;
        _fullFilePath = source.SourcePath ?? string.Empty;

        await InitializeAudioService(source);

        SelectedFileName = source.DisplayName;
        IsFileSelected = true;
        HasAudioLoaded = true;

        await _visualizationService.UpdateVisualizationAsync(source);
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
}
