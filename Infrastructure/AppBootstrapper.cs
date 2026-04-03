// Copyright (C) Neurosoft

using System;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpeechMarkupEditor.Infrastructure.AudioSourceProviderFactory;
using SpeechMarkupEditor.Infrastructure.Configuration;
using SpeechMarkupEditor.Services.Audio;
using SpeechMarkupEditor.Services.AudioVisualization;
using SpeechMarkupEditor.Services.Dialog;
using SpeechMarkupEditor.Services.ExportService;
using SpeechMarkupEditor.Services.NewWordMarkerDialog;
using SpeechMarkupEditor.Services.SpeechRecognition;
using SpeechMarkupEditor.Services.StorageProvider;
using SpeechMarkupEditor.Services.WordSeries;
using SpeechMarkupEditor.ViewModels;
using SpeechMarkupEditor.Views;

namespace SpeechMarkupEditor.Infrastructure;

public class AppBootstrapper
{
    public IServiceProvider ServiceProvider { get; }

    public AppBootstrapper()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        ServiceProvider = host.Services;
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<AudioSettings>(context.Configuration.GetSection("AudioSettings"));
        services.Configure<ModelSettings>(context.Configuration.GetSection("ModelSettings"));
        services.Configure<DialogSettings>(context.Configuration.GetSection("DialogSettings"));

        services.AddScoped<IAudioService, AudioService>();
        services.AddScoped<IAudioVisualizationService, AudioVisualizationService>();
        services.AddScoped<ISpeechRecognitionService, SpeechRecognitionService>();
        services.AddScoped<IWordSeriesService, WordSeriesService>();
        services.AddScoped<IExportService, ExportToJsonService>();

        services.AddTransient<IWordMarkerDialogService, WordMarkerDialogService>();
        services.AddTransient<WordMarkerDialogViewModel>();

        services.AddSingleton<IAudioSourceProviderFactory, FileAudioSourceProviderFactory>();
        services.AddSingleton<IStorageProviderAccessor, StorageProviderAccessor>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        services.AddScoped<IStorageProvider>(provider =>
            provider.GetRequiredService<IStorageProviderAccessor>().GetStorageProvider());
    }
}