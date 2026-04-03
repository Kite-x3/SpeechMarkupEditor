// Copyright (C) Neurosoft

using Avalonia.Platform.Storage;

namespace SpeechMarkupEditor.Services.StorageProvider;

public interface IStorageProviderAccessor
{
    IStorageProvider? GetStorageProvider();
}