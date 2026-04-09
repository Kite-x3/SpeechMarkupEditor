// Copyright (C) Neurosoft

using System;
using SpeechMarkupEditor.Assets;

namespace SpeechMarkupEditor.Models;

/// <summary>
/// Временная метка слова в аудиозаписи с валидацией временных интервалов
/// </summary>
public class WordTimestamp : IComparable<WordTimestamp>
{
    private const double TOLERANCE = 0.01;
    private double _startTime;
    private double _endTime;
    private string _word;

    public WordTimestamp(string word, double startTime, double endTime)
    {
        ValidateWord(word);
        ValidateRange(startTime, endTime);

        _word = word;
        _startTime = startTime;
        _endTime = endTime;
    }

    /// <summary>
    /// Текст слова
    /// </summary>
    public string Word
    {
        get => _word;
        set
        {
            ValidateWord(value);
            _word = value;
        }
    }

    /// <summary>
    /// Время начала слова в секундах
    /// </summary>
    public double StartTime
    {
        get => _startTime;
        set
        {
            ValidateRange(value, _endTime);
            _startTime = value;
        }
    }

    /// <summary>
    /// Время окончания слова в секундах
    /// </summary>
    public double EndTime
    {
        get => _endTime;
        set
        {
            ValidateRange(_startTime, value);
            _endTime = value;
        }
    }

    /// <summary>
    /// Проверка валидности текста слова
    /// </summary>
    /// <param name="word">Текст слова для проверки</param>
    /// <exception cref="ArgumentNullException">Выбрасывается если word равен null или состоит из пробелов</exception>
    private void ValidateWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new ArgumentNullException(
                nameof(word),
                Resources.WordEmpty);
    }

    /// <summary>
    /// Проверка корректности временного интервала
    /// </summary>
    /// <param name="start">Время начала</param>
    /// <param name="end">Время окончания</param>
    /// <exception cref="ArgumentOutOfRangeException">Если start или end отрицательные</exception>
    /// <exception cref="ArgumentException">Если start >= end</exception>
    private static void ValidateRange(double start, double end)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(
                nameof(start),
                Resources.NegativeStartTime);

        if (end < 0)
            throw new ArgumentOutOfRangeException(
                nameof(end),
                Resources.NegativeEndTime);

        if (start >= end - TOLERANCE)
                throw new ArgumentException(Resources.StartAfterEnd);
    }

    public int CompareTo(WordTimestamp other)
    {
        int timeComparison = _startTime.CompareTo(other._startTime);
        return timeComparison != 0 ? timeComparison
            : string.Compare(_word, other._word, StringComparison.Ordinal);
    }
}