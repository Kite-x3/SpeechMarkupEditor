// Copyright (C) Neurosoft

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace SpeechMarkupEditor.Services.StorageProvider;

public class StorageProviderAccessor : IStorageProviderAccessor
{
    public IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        return desktop.MainWindow?.StorageProvider;
    }
}