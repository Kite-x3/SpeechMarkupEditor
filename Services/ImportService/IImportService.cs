// Copyright (C) Neurosoft

using System.Threading.Tasks;

namespace SpeechMarkupEditor.Services.ImportService;

public interface IImportService
{
    Task<ImportedMarkup?> ImportAsync();
}
