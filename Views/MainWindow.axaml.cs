// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using SpeechMarkupEditor.Assets;
using Microsoft.Extensions.DependencyInjection;
using SpeechMarkupEditor.Controls;
using SpeechMarkupEditor.Messages;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.AudioVisualization;
using SpeechMarkupEditor.ViewModels;

namespace SpeechMarkupEditor.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<TimeEditorControl, (double StartTime, double EndTime)> _timeEditorSnapshots = new();
    private MainWindowViewModel? _attachedViewModel;

    public MainWindow()
    {
        InitializeComponent();
        WindowIconFactory.ApplyAppIcon(this);
    }

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        WindowIconFactory.ApplyAppIcon(this);

        WeakReferenceMessenger.Default.Register<MainWindow, WordMarkerDialogRequestMessage>(
            this, (recipient, message) =>
            {
                var dialog = new WordMarkerDialog
                {
                    DataContext = _serviceProvider.GetRequiredService<WordMarkerDialogViewModel>()
                };

                var vm = (WordMarkerDialogViewModel)dialog.DataContext;
                vm.StartTime = message.StartTime;
                vm.EndTime = message.StartTime + 0.5;

                var task = new TaskCompletionSource<WordMarkerSubmittedEventArgs?>();

                WeakReferenceMessenger.Default.Register<WordMarkerSubmittedMessage>(
                    this, (r, m)
                        =>
                    {
                        task.TrySetResult(m.Args);
                        WeakReferenceMessenger.Default.Unregister<WordMarkerSubmittedMessage>(this);
                    });

                dialog.ShowDialog(recipient);
                message.Reply(task.Task);
            });

        this.DataContextChanged += OnDataContextChanged;
    }
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DetachFromViewModel();

        if (DataContext is MainWindowViewModel vm)
        {
            _attachedViewModel = vm;
            var visualizationService = _serviceProvider.GetRequiredService<IAudioVisualizationService>();
            visualizationService.Initialize(this.FindControl<WaveformControl>("WaveformControl"));
            visualizationService.WaveformUpdated += vm.OnWaveformUpdated;
            AttachSeriesCollection(vm.LeftSeries);
            AttachSeriesCollection(vm.RightSeries);
        }
    }

    private async void ModelSettingsMenuItem_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<ModelSettingsWindow>();
        window.DataContext = _serviceProvider.GetRequiredService<ModelSettingsViewModel>();
        await window.ShowDialog(this);
    }

    private async void OpenHistoryMenuItem_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<MarkupHistoryWindow>();
        var viewModel = _serviceProvider.GetRequiredService<MarkupHistoryViewModel>();
        await viewModel.InitializeAsync();
        window.DataContext = viewModel;

        var selectedEntry = await window.ShowDialog<MarkupHistoryEntrySummary?>(this);
        if (selectedEntry == null)
            return;

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            await mainWindowViewModel.LoadMarkupHistoryAsync(selectedEntry.Id);
    }

    private void WordTimingEditor_OnGotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        if (sender is not TimeEditorControl timeEditor || timeEditor.DataContext is not WordTimestamp word)
            return;

        _timeEditorSnapshots[timeEditor] = (word.StartTime, word.EndTime);
    }

    private void AttachSeriesCollection(ObservableCollection<Series> seriesCollection)
    {
        seriesCollection.CollectionChanged += OnSeriesCollectionChanged;
        foreach (var series in seriesCollection)
            AttachSeries(series);
    }

    private void DetachSeriesCollection(ObservableCollection<Series> seriesCollection)
    {
        seriesCollection.CollectionChanged -= OnSeriesCollectionChanged;
        foreach (var series in seriesCollection)
            DetachSeries(series);
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is Series series)
                    DetachSeries(series);
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is Series series)
                    AttachSeries(series);
            }
        }
    }

    private void AttachSeries(Series series)
    {
        series.Words.CollectionChanged += OnWordsCollectionChanged;
        foreach (var word in series.Words)
            word.PropertyChanged += OnWordPropertyChanged;
    }

    private void DetachSeries(Series series)
    {
        series.Words.CollectionChanged -= OnWordsCollectionChanged;
        foreach (var word in series.Words)
            word.PropertyChanged -= OnWordPropertyChanged;
    }

    private void OnWordsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is WordTimestamp word)
                    word.PropertyChanged -= OnWordPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is WordTimestamp word)
                    word.PropertyChanged += OnWordPropertyChanged;
            }
        }
    }

    private async void OnWordPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(WordTimestamp.StartTime) and not nameof(WordTimestamp.EndTime) and not nameof(WordTimestamp.Word))
            return;

        await SaveInlineMarkupEditAsync();
    }

    private async Task SaveInlineMarkupEditAsync()
    {
        if (_attachedViewModel == null)
            return;

        await _attachedViewModel.SaveInlineMarkupEditAsync();
    }

    private void DetachFromViewModel()
    {
        if (_attachedViewModel == null)
            return;

        var visualizationService = _serviceProvider.GetRequiredService<IAudioVisualizationService>();
        visualizationService.WaveformUpdated -= _attachedViewModel.OnWaveformUpdated;
        DetachSeriesCollection(_attachedViewModel.LeftSeries);
        DetachSeriesCollection(_attachedViewModel.RightSeries);
        _attachedViewModel = null;
    }
}
