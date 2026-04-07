// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Configuration;
using SpeechMarkupEditor.Infrastructure.Data;
using SpeechMarkupEditor.Infrastructure.Data.Entities;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.RecognitionModels;

public class RecognitionModelService : IRecognitionModelService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly List<RecognitionModelDefinition> _models;
    private readonly string _baseModelPath;

    public RecognitionModelService(
        IOptions<ModelSettings> modelSettings,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _baseModelPath = NormalizePath(modelSettings.Value.ModelPath);
        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Database.EnsureCreated();
        _models = LoadOrSeedModels(dbContext, modelSettings.Value);
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
            existing.Engine = Resources.VoskEngineName;
            existing.IsDeletable = IsDeletablePath(existing.Path);
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
            IsCurrent = true,
            IsDeletable = IsDeletablePath(normalizedPath)
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

    public async Task DeleteModelAsync(string path)
    {
        var normalizedPath = NormalizePath(path);
        var modelToRemove = _models.FirstOrDefault(model => PathsEqual(model.Path, normalizedPath));
        if (modelToRemove == null || !modelToRemove.IsDeletable)
            return;

        var removedWasCurrent = modelToRemove.IsCurrent;
        _models.Remove(modelToRemove);

        if (removedWasCurrent && _models.Count > 0 && _models.All(model => !model.IsCurrent))
            _models[0].IsCurrent = true;

        await SaveAsync();
    }

    private List<RecognitionModelDefinition> LoadOrSeedModels(AppDbContext dbContext, ModelSettings settings)
    {
        var persistedModels = dbContext.RecognitionModels
            .AsNoTracking()
            .OrderByDescending(model => model.IsCurrent)
            .ThenBy(model => model.Name)
            .Select(model => new RecognitionModelDefinition
            {
                Name = model.Name,
                Engine = model.Engine,
                Path = model.Path,
                IsCurrent = model.IsCurrent
            })
            .ToList();

        foreach (var model in persistedModels)
        {
            model.IsDeletable = IsDeletablePath(model.Path);
        }

        if (persistedModels.Count > 0)
        {
            EnsureCurrentModelAssigned(persistedModels);
            return persistedModels;
        }

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
                IsCurrent = item.IsCurrent,
                IsDeletable = IsDeletablePath(item.Path)
            });
        }

        if (models.Count == 0)
        {
            foreach (var path in settings.AvailableModels)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var normalizedPath = NormalizePath(path);
                if (models.Any(model => PathsEqual(model.Path, normalizedPath)))
                    continue;

                models.Add(new RecognitionModelDefinition
                {
                    Name = Path.GetFileName(normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                    Engine = Resources.VoskEngineName,
                    Path = normalizedPath,
                    IsCurrent = false,
                    IsDeletable = IsDeletablePath(normalizedPath)
                });
            }
        }

        if (models.Count == 0 && !string.IsNullOrWhiteSpace(settings.ModelPath))
        {
            models.Add(new RecognitionModelDefinition
            {
                Name = $"{Resources.VoskModelName} - {Resources.RussianLanguageCode}",
                Engine = Resources.VoskEngineName,
                Path = NormalizePath(settings.ModelPath),
                IsCurrent = true,
                IsDeletable = IsDeletablePath(settings.ModelPath)
            });
        }

        if (models.Count > 0 && models.All(model => !model.IsCurrent))
        {
            models[0].IsCurrent = true;
        }

        dbContext.RecognitionModels.AddRange(models.Select(model => new RecognitionModelEntity
        {
            Name = model.Name,
            Engine = model.Engine,
            Path = model.Path,
            IsCurrent = model.IsCurrent
        }));
        dbContext.SaveChanges();

        return models;
    }

    private async Task SaveAsync()
    {
        EnsureCurrentModelAssigned(_models);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var existingEntities = await dbContext.RecognitionModels.ToListAsync();
        dbContext.RecognitionModels.RemoveRange(existingEntities);
        await dbContext.SaveChangesAsync();

        dbContext.RecognitionModels.AddRange(_models.Select(model => new RecognitionModelEntity
        {
            Name = model.Name,
            Engine = model.Engine,
            Path = NormalizePath(model.Path),
            IsCurrent = model.IsCurrent
        }));
        await dbContext.SaveChangesAsync();
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDeletablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(_baseModelPath))
            return true;

        return !PathsEqual(path, _baseModelPath);
    }

    private static void EnsureCurrentModelAssigned(List<RecognitionModelDefinition> models)
    {
        if (models.Count > 0 && models.All(model => !model.IsCurrent))
            models[0].IsCurrent = true;
    }
}
