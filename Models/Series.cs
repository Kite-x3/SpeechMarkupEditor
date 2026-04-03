// Copyright (C) Neurosoft

using System.Collections.Generic;
using System.Linq;
using SpeechMarkupEditor.Infrastructure.Comparers;

namespace SpeechMarkupEditor.Models;

public class Series
{
    public int SeriesNumber { get; set; }
    public SortedSet<WordTimestamp> Words { get; set; } = new SortedSet<WordTimestamp>(new WordTimestampComparer());

    public void AddWord(WordTimestamp word)
    {
        Words.Add(word);
    }
}