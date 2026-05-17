// Copyright (C) Neurosoft

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SpeechMarkupEditor.Assets;

namespace SpeechMarkupEditor.Models;

/// <summary>
/// Временная метка слова в аудиозаписи с валидацией значений.
/// </summary>
public class WordTimestamp : IComparable<WordTimestamp>, INotifyPropertyChanged
{
    private double _startTime;
    private double _endTime;
    private string _word;
    private bool _isPlaying;
    private EarType _channel;

    [JsonIgnore]
    public bool RangeWasAutoCorrected { get; private set; }
    [JsonIgnore]
    public string? LastRangeCorrectionMessage { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WordTimestamp(
        string word,
        double startTime,
        double endTime,
        EarType channel = EarType.NonDichotic)
    {
        ValidateWord(word);
        ValidateNonNegative(startTime, nameof(startTime), Resources.NegativeStartTime);
        ValidateNonNegative(endTime, nameof(endTime), Resources.NegativeEndTime);

        _word = word;
        _startTime = startTime;
        _endTime = endTime;
        RangeWasAutoCorrected = false;
        LastRangeCorrectionMessage = null;
        _channel = channel;
    }

    public EarType Channel
    {
        get => _channel;
        set
        {
            if (_channel == value)
                return;

            _channel = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value)
                return;

            _isPlaying = value;
            OnPropertyChanged();
        }
    }

    public string Word
    {
        get => _word;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                string old = _word;
                _word = string.Empty;
                OnPropertyChanged();

                _word = old;
                OnPropertyChanged();
                return;
            }

            value = value.Trim();
            if (_word == value)
                return;

            _word = value;
            OnPropertyChanged();
        }
    }

    public double StartTime
    {
        get => _startTime;
        set
        {
            ValidateNonNegative(value, nameof(StartTime), Resources.NegativeStartTime);
            if (Math.Abs(_startTime - value) <= double.Epsilon)
                return;

            _startTime = value;
            ResetRangeCorrectionState();
            OnPropertyChanged();
        }
    }

    public double EndTime
    {
        get => _endTime;
        set
        {
            ValidateNonNegative(value, nameof(EndTime), Resources.NegativeEndTime);
            if (Math.Abs(_endTime - value) <= double.Epsilon)
                return;

            _endTime = value;
            ResetRangeCorrectionState();
            OnPropertyChanged();
        }
    }

    private static string? ValidateWord(string word)
    {
        return string.IsNullOrWhiteSpace(word) ? Resources.WordEmpty : null;
    }

    private static void ValidateNonNegative(double value, string paramName, string message)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, message);
    }

    private void ResetRangeCorrectionState()
    {
        if (!RangeWasAutoCorrected && LastRangeCorrectionMessage == null)
            return;

        RangeWasAutoCorrected = false;
        LastRangeCorrectionMessage = null;
        OnPropertyChanged(nameof(RangeWasAutoCorrected));
        OnPropertyChanged(nameof(LastRangeCorrectionMessage));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public int CompareTo(WordTimestamp other)
    {
        int timeComparison = _startTime.CompareTo(other._startTime);
        return timeComparison != 0
            ? timeComparison
            : string.Compare(_word, other._word, StringComparison.Ordinal);
    }
}
