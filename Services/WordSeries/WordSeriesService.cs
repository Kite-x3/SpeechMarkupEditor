// Copyright (C) Neurosoft

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Comparers;
using SpeechMarkupEditor.Infrastructure.Configuration;
using SpeechMarkupEditor.Infrastructure.Converters;
using SpeechMarkupEditor.Models;
using SpeechMarkupEditor.Services.Dialog;

namespace SpeechMarkupEditor.Services.WordSeries;

public class WordSeriesService : IWordSeriesService
{
    /// <summary>
    /// Порог паузы между словами для группировки в серии
    /// </summary>
    private readonly IDialogService _dialogService;
    private readonly TimeToStringConverter _timeConverter = new TimeToStringConverter();
    private readonly double _pauseThreshold;

    public WordSeriesService(IDialogService dialogService, IOptions<AudioSettings> audioSettings)
    {
        _dialogService = dialogService;
        _pauseThreshold =  audioSettings.Value.PauseThreshold;
    }

    /// <summary>
    /// Группировка слов на основе временных интервалов
    /// </summary>
    /// <param name="words">Список слов для группировки</param>
    /// <returns>Список серий слов</returns>
    public List<List<WordTimestamp>> GroupWordsIntoSeries(List<WordTimestamp> words)
    {
        if (words.Count == 0)
            return new List<List<WordTimestamp>>();

        var series = new List<List<WordTimestamp>>();
        var currentSeries = new List<WordTimestamp> { words[0] };

        for (int i = 1; i < words.Count; i++)
        {
            var prevWord = words[i - 1];
            var currentWord = words[i];

            if (currentWord.StartTime - prevWord.EndTime <= _pauseThreshold)
            {
                currentSeries.Add(currentWord);
            }
            else
            {
                series.Add(currentSeries);
                currentSeries = new List<WordTimestamp> { currentWord };
            }
        }

        series.Add(currentSeries);
        return series;
    }

    /// <summary>
    /// Объединяет результаты распознавания с существующими сериями
    /// </summary>
    /// <param name="targetSeries">Изначальный список серий</param>
    /// <param name="newSeries">Список распознанных серий</param>
    public RecognitionMergeResult MergeRecognitionResult(ObservableCollection<Series> targetSeries, List<Series> newSeries)
    {
        var result = new RecognitionMergeResult();

        foreach (var newSeriesItem in newSeries.Where(ns => ns.Words.Count > 0))
        {
            result.Accumulate(ProcessSeriesWithOverlaps(targetSeries, newSeriesItem, false));
        }

        return result;
    }

    /// <summary>
    /// Добавляет слово в соответствующую серию
    /// </summary>
    /// <param name="series">Серия, в которую добавляют</param>
    /// <param name="word">Слово, которое добавляют</param>
    public void AddWordToSeries(ObservableCollection<Series> series, WordTimestamp word)
    {
        var tempSeries = new Series();
        tempSeries.AddWord(word);
        ProcessSeriesWithOverlaps(series, tempSeries, true);
    }

    public void RemoveWordFromSeries(ObservableCollection<Series> series, WordTimestamp word)
    {
        foreach (var item in series.ToList())
        {
            if (!item.Words.Contains(word))
                continue;

            item.Words.Remove(word);
            if (item.Words.Count == 0)
                series.Remove(item);

            RenumberSeriesByTimeOrder(series);
            return;
        }
    }

    /// <summary>
    /// Обработка новой серии слов, проверяя на пересечения с существующими сериями
    /// </summary>
    /// <param name="target">Серия, в которую добавляют</param>
    /// <param name="newSeries">Серия, которую добавляют</param>
    private RecognitionMergeResult ProcessSeriesWithOverlaps(ObservableCollection<Series> target, Series newSeries, bool showOverlapDetails)
    {
        var result = new RecognitionMergeResult();
        var overlappingWords = FindOverlappingWords(target, newSeries);

        if (overlappingWords.Count != 0)
        {
            if (showOverlapDetails)
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine($"{Resources.OverlapsWithWords}:");

                foreach (var word in overlappingWords)
                {
                    errorBuilder.AppendLine(
                        $"- '{word.Word}' (" +
                        $"{_timeConverter.Convert(word.StartTime, typeof(string), null, CultureInfo.InvariantCulture)}-" +
                        $"{_timeConverter.Convert(word.EndTime, typeof(string), null, CultureInfo.InvariantCulture)})");
                }

                string errorMessage = errorBuilder.ToString();
                _dialogService.ShowWarningAsync(errorMessage);
            }

            var filteredWords = new List<WordTimestamp>();
            foreach (var nw in newSeries.Words)
            {
                if (!target.SelectMany(s => s.Words).Any(w => WordsOverlap(w, nw)) &&
                    !filteredWords.Contains(nw))
                {
                    filteredWords.Add(nw);
                }
            }

            result.OverlappingWordsCount = newSeries.Words.Count - filteredWords.Count;
            newSeries.Words.Clear();
            foreach (var word in filteredWords)
            {
                newSeries.AddWord(word);
            }

            if (newSeries.Words.Count == 0)
                return result;
        }

        result.AddedWordsCount = newSeries.Words.Count;
        AddNonOverlappingSeries(target, newSeries);
        return result;
    }

    /// <summary>
    /// Нахождение слов, пересекающихся по времени с новой серией
    /// </summary>
    /// <param name="series">Коллекция существующих серий</param>
    /// <param name="newSeries">Новая серия для проверки</param>
    /// <returns>Список пересекающихся слов</returns>
    private List<WordTimestamp> FindOverlappingWords(IEnumerable<Series> series, Series newSeries)
    {
        return series.SelectMany(s => s.Words)
                   .Where(w => newSeries.Words.Any(nw => WordsOverlap(w, nw)))
                   .ToList();
    }

    /// <summary>
    /// Добавление новой серии слов с объединением с близкими по времени сериями при необходимости
    /// </summary>
    /// <param name="target">Серия, в которую добавляют</param>
    /// <param name="newSeries">Серия, которую добавляют</param>
    private void AddNonOverlappingSeries(ObservableCollection<Series> target, Series newSeries)
    {
        var seriesToMerge = FindMergeCandidates(target, newSeries);

        if (seriesToMerge.Count > 0)
        {
            MergeSeriesCollections(target, seriesToMerge, newSeries);
        }
        else
        {
            target.Add(new Series
            {
                SeriesNumber = target.Count + 1,
                Words = new ObservableCollection<WordTimestamp>(newSeries.Words)
            });
        }

        RenumberSeriesByTimeOrder(target);
    }

    /// <summary>
    /// Нахождение серий, которые можно объединить с новой серией
    /// </summary>
    /// <param name="existing">Существующие серии<</param>
    /// <param name="newSeries">Новая серия</param>
    /// <returns>Список серий для объединения</returns>
    private List<Series> FindMergeCandidates(ObservableCollection<Series> existing, Series newSeries)
    {
        double newStart = newSeries.Words.First().StartTime;
        double newEnd = newSeries.Words.Last().EndTime;

        return existing.Where(s => s.Words.Count > 0)
                      .Where(s => IsWithinThreshold(s.Words.First().StartTime, newEnd) ||
                                 IsWithinThreshold(s.Words.Last().EndTime, newStart))
                      .ToList();
    }

    /// <summary>
    /// Объединение нескольких серий в одну
    /// </summary>
    /// <param name="target">Серия, в которую объединяют</param>
    /// <param name="toMerge">Серии для объединения</param>
    /// <param name="newSeries">Новая серия</param>
    private void MergeSeriesCollections(ObservableCollection<Series> target,
                                      List<Series> toMerge,
                                      Series newSeries)
    {
        var combinedSeries = new Series();

        foreach (var series in toMerge)
        {
            foreach (var word in series.Words)
            {
                combinedSeries.AddWord(word);
            }
            target.Remove(series);
        }

        foreach (var word in newSeries.Words)
        {
            combinedSeries.AddWord(word);
        }

        target.Add(combinedSeries);
    }

    /// <summary>
    /// Проверка, находится ли разница между временами в пределах порога
    /// </summary>
    /// <param name="time1">Первое время</param>
    /// <param name="time2">Второе время</param>
    /// <returns>Если разница не превышает порог PAUSE_THRESHOLD, возвращает true иначе false</returns>
    private bool IsWithinThreshold(double time1, double time2)
    {
        return Math.Abs(time1 - time2) <= _pauseThreshold;
    }

    /// <summary>
    /// Нумерация серий в хронологическом порядке
    /// </summary>
    /// <param name="series">Коллекция серий для нумерации</param>
    private void RenumberSeriesByTimeOrder(ObservableCollection<Series> series)
    {
        var ordered = series.Where(s => s.Words.Count > 0)
                           .OrderBy(s => s.Words.First().StartTime)
                           .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            var currentSeries = ordered[i];
            currentSeries.SeriesNumber = i + 1;

            var currentIndex = series.IndexOf(currentSeries);
            if (currentIndex >= 0 && currentIndex != i)
            {
                series.Move(currentIndex, i);
            }
        }
    }

    /// <summary>
    /// Проверка пересечения временных интервалов двух слов
    /// </summary>
    /// <param name="w1">Первое слово</param>
    /// <param name="w2">Второе слово</param>
    /// <returns></returns>
    private bool WordsOverlap(WordTimestamp w1, WordTimestamp w2)
    {
        return w1.StartTime < w2.EndTime && w2.StartTime < w1.EndTime;
    }
}
