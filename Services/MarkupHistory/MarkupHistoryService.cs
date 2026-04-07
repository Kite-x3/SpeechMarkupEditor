using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeechMarkupEditor.Infrastructure.Data;
using SpeechMarkupEditor.Infrastructure.Data.Entities;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.ImportService;

namespace SpeechMarkupEditor.Services.MarkupHistory;

public class MarkupHistoryService : IMarkupHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public MarkupHistoryService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SaveAsync(string fileName, IReadOnlyList<Series> leftSeries, IReadOnlyList<Series> rightSeries, string? sourcePath = null)
    {
        var snapshot = new MarkupSnapshot
        {
            FileName = fileName,
            SourcePath = sourcePath,
            LeftChannel = MapSeries(leftSeries),
            RightChannel = MapSeries(rightSeries)
        };

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.MarkupHistoryEntries.Add(new MarkupHistoryEntryEntity
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? "markup" : fileName,
            SourcePath = sourcePath,
            CreatedAtUtc = DateTimeOffset.Now,
            PayloadJson = JsonSerializer.Serialize(snapshot, JsonOptions)
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MarkupHistoryEntrySummary>> GetHistoryAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var entries = await dbContext.MarkupHistoryEntries
            .AsNoTracking()
            .Select(x => new MarkupHistoryEntrySummary
            {
                Id = x.Id,
                FileName = x.FileName,
                SourcePath = x.SourcePath,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return entries
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();
    }

    public async Task<ImportedMarkup?> LoadAsync(long id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.MarkupHistoryEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return null;

        var snapshot = JsonSerializer.Deserialize<MarkupSnapshot>(entity.PayloadJson, JsonOptions);
        if (snapshot == null)
            return null;

        return new ImportedMarkup
        {
            FileName = snapshot.FileName,
            SourcePath = snapshot.SourcePath,
            LeftChannel = RestoreSeries(snapshot.LeftChannel),
            RightChannel = RestoreSeries(snapshot.RightChannel)
        };
    }

    public async Task DeleteAsync(long id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.MarkupHistoryEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return;

        dbContext.MarkupHistoryEntries.Remove(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task ClearAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var entries = await dbContext.MarkupHistoryEntries.ToListAsync();
        if (entries.Count == 0)
            return;

        dbContext.MarkupHistoryEntries.RemoveRange(entries);
        await dbContext.SaveChangesAsync();
    }

    private static List<SeriesSnapshot> MapSeries(IReadOnlyList<Series> seriesList)
    {
        return seriesList.Select(series => new SeriesSnapshot
        {
            Words = series.Words.Select(word => new WordSnapshot
            {
                Word = word.Word,
                StartTime = word.StartTime,
                EndTime = word.EndTime
            }).ToList()
        }).ToList();
    }

    private static ObservableCollection<Series> RestoreSeries(List<SeriesSnapshot>? source)
    {
        var result = new ObservableCollection<Series>();
        if (source == null)
            return result;

        for (var i = 0; i < source.Count; i++)
        {
            var series = new Series { SeriesNumber = i + 1 };
            foreach (var word in source[i].Words.OrderBy(x => x.StartTime).ThenBy(x => x.Word, StringComparer.Ordinal))
            {
                series.AddWord(new WordTimestamp(word.Word ?? string.Empty, word.StartTime, word.EndTime));
            }

            if (series.Words.Count > 0)
                result.Add(series);
        }

        return result;
    }

    private sealed class MarkupSnapshot
    {
        public string FileName { get; set; } = string.Empty;
        public string? SourcePath { get; set; }
        public List<SeriesSnapshot> LeftChannel { get; set; } = [];
        public List<SeriesSnapshot> RightChannel { get; set; } = [];
    }

    private sealed class SeriesSnapshot
    {
        public List<WordSnapshot> Words { get; set; } = [];
    }

    private sealed class WordSnapshot
    {
        public string? Word { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }
}
