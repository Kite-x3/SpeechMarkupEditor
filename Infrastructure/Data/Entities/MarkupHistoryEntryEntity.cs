using System;

namespace SpeechMarkupEditor.Infrastructure.Data.Entities;

public class MarkupHistoryEntryEntity
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? SourcePath { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}
