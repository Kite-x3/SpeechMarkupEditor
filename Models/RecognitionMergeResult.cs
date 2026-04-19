// Copyright (C) Neurosoft

namespace SpeechMarkupEditor.Models;

public class RecognitionMergeResult
{
    public int AddedWordsCount { get; set; }
    public int OverlappingWordsCount { get; set; }

    public bool HasAddedWords => AddedWordsCount > 0;
    public bool HasOverlaps => OverlappingWordsCount > 0;

    public void Accumulate(RecognitionMergeResult other)
    {
        AddedWordsCount += other.AddedWordsCount;
        OverlappingWordsCount += other.OverlappingWordsCount;
    }
}
