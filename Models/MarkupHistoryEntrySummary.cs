using System;

namespace SpeechMarkupEditor.Models;

public class MarkupHistoryEntrySummary
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
