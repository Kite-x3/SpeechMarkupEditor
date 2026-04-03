// Copyright (C) Neurosoft

using System.Collections.Generic;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Infrastructure.Comparers;

public class WordTimestampComparer : IComparer<WordTimestamp>
{
    public int Compare(WordTimestamp x, WordTimestamp y)
    {
        return x.StartTime.CompareTo(y.StartTime);
    }
}