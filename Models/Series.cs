// Copyright (C) Neurosoft

using System.Linq;
using System.Collections.ObjectModel;
using SpeechMarkupEditor.Infrastructure.Comparers;

namespace SpeechMarkupEditor.Models;

public class Series
{
    private static readonly WordTimestampComparer Comparer = new();

    public int SeriesNumber { get; set; }
    public ObservableCollection<WordTimestamp> Words { get; set; } = [];

    public void AddWord(WordTimestamp word)
    {
        var insertIndex = 0;
        while (insertIndex < Words.Count && Comparer.Compare(Words[insertIndex], word) < 0)
        {
            insertIndex++;
        }

        if (insertIndex < Words.Count && Comparer.Compare(Words[insertIndex], word) == 0)
            return;

        Words.Insert(insertIndex, word);
    }
}
