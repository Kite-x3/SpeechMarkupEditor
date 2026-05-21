// Copyright (C) Neurosoft

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using SpeechMarkupEditor.Messages;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.WordSeries;

public interface IWordSeriesService
{
    RecognitionMergeResult MergeRecognitionResult(
        ObservableCollection<Series> targetSeries,
        List<Series> newSeriesData);

    void AddWordToSeries(ObservableCollection<Series> series, WordTimestamp word);
    List<WordTimestamp> GetOverlaps(ObservableCollection<Series> series, WordTimestamp word);
    void RemoveWordFromSeries(ObservableCollection<Series> series, WordTimestamp word);
    string? GetOverlapWarning(ObservableCollection<Series> series);
    Task RebuildSeriesCollection(ObservableCollection<Series> series, CancellationToken ct = default);
    List<List<WordTimestamp>> GroupWordsIntoSeries(List<WordTimestamp> words);
    List<Series> ConvertToSeriesList(List<List<WordTimestamp>> wordSeries);
}
