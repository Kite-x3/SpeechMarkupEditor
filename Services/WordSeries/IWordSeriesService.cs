// Copyright (C) Neurosoft

using System.Collections.Generic;
using System.Collections.ObjectModel;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.WordSeries;

public interface IWordSeriesService
{
    void MergeRecognitionResult(
        ObservableCollection<Series> targetSeries,
        List<Series> newSeriesData);

    void AddWordToSeries(ObservableCollection<Series> series, WordTimestamp word);
    List<List<WordTimestamp>> GroupWordsIntoSeries(List<WordTimestamp> words);
}