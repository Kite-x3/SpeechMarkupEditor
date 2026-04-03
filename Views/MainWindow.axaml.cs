// Copyright (C) Neurosoft

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
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
    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();

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
        if (DataContext is MainWindowViewModel vm)
        {
            var visualizationService = _serviceProvider.GetRequiredService<IAudioVisualizationService>();
            visualizationService.Initialize(this.FindControl<WaveformControl>("WaveformControl"));
            visualizationService.WaveformUpdated += vm.OnWaveformUpdated;
        }
    }
}