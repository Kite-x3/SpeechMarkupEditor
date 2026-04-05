// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Configuration;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.RecognitionModels;

public class RecognitionModelService : IRecognitionModelService
{
    private readonly string _settingsPath;
    private readonly string _settingsDirectory;
    private readonly List<RecognitionModelDefinition> _models;

    public RecognitionModelService(IOptions<ModelSettings> modelSettings)
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settingsDirectory = Path.GetDirectoryName(_settingsPath) ?? AppContext.BaseDirectory;
        _models = BuildInitialModels(modelSettings.Value);
    }

    public IReadOnlyList<RecognitionModelDefinition> GetModels()
    {
        return _models
            .OrderByDescending(model => model.IsCurrent)
            .ThenBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public RecognitionModelDefinition? GetCurrentModel()
    {
        return _models.FirstOrDefault(model => model.IsCurrent);
    }

    public async Task AddModelAsync(string name, string path)
    {
        var normalizedPath = NormalizePath(path);
        var existing = _models.FirstOrDefault(model => PathsEqual(model.Path, normalizedPath));
        if (existing != null)
        {
            existing.Name = name;
            await SetCurrentModelAsync(existing.Path);
            return;
        }

        foreach (var model in _models)
        {
            model.IsCurrent = false;
        }

        _models.Add(new RecognitionModelDefinition
        {
            Name = name,
            Engine = Resources.VoskEngineName,
            Path = normalizedPath,
            IsCurrent = true
        });

        await SaveAsync();
    }

    public async Task SetCurrentModelAsync(string path)
    {
        var normalizedPath = NormalizePath(path);
        foreach (var model in _models)
        {
            model.IsCurrent = PathsEqual(model.Path, normalizedPath);
        }

        await SaveAsync();
    }

    private List<RecognitionModelDefinition> BuildInitialModels(ModelSettings settings)
    {
        var models = new List<RecognitionModelDefinition>();

        foreach (var item in settings.Models)
        {
            if (string.IsNullOrWhiteSpace(item.Path))
                continue;

            models.Add(new RecognitionModelDefinition
            {
                Name = string.IsNullOrWhiteSpace(item.Name) ? Resources.VoskModelName : item.Name,
                Engine = string.IsNullOrWhiteSpace(item.Engine) ? Resources.VoskEngineName : item.Engine,
                Path = NormalizePath(item.Path),
                IsCurrent = item.IsCurrent
            });
        }

        if (models.Count == 0 && !string.IsNullOrWhiteSpace(settings.ModelPath))
        {
            models.Add(new RecognitionModelDefinition
            {
                Name = $"{Resources.VoskModelName} - {Resources.RussianLanguageCode}",
                Engine = Resources.VoskEngineName,
                Path = NormalizePath(settings.ModelPath),
                IsCurrent = true
            });
        }

        if (models.Count > 0 && models.All(model => !model.IsCurrent))
        {
            models[0].IsCurrent = true;
        }

        return models;
    }

    private async Task SaveAsync()
    {
        if (!File.Exists(_settingsPath))
            return;

        var root = JsonNode.Parse(await File.ReadAllTextAsync(_settingsPath))?.AsObject() ?? new JsonObject();
        var currentModel = _models.FirstOrDefault(model => model.IsCurrent);

        root["ModelSettings"] = new JsonObject
        {
            ["ModelPath"] = currentModel == null ? string.Empty : ToConfigPath(currentModel.Path),
            ["Models"] = new JsonArray(_models.Select(model =>
                (JsonNode)new JsonObject
                {
                    ["Name"] = model.Name,
                    ["Engine"] = model.Engine,
                    ["Path"] = ToConfigPath(model.Path),
                    ["IsCurrent"] = model.IsCurrent
                }).ToArray())
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(_settingsPath, root.ToJsonString(options));
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(_settingsDirectory, path));
    }

    private bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private string ToConfigPath(string path)
    {
        var normalized = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return Path.GetRelativePath(_settingsDirectory, normalized);
    }
}
