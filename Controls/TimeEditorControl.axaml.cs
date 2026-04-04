// Copyright (C) Neurosoft

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SpeechMarkupEditor.Controls;

public partial class TimeEditorControl : UserControl
{
    private static readonly Regex ExactTimePattern =
        new(@"^(?<minutes>\d+):(?<seconds>[0-5]\d)\.(?<fraction>\d{2})$");

    private static readonly Regex PartialTimePattern =
        new(@"^\d*(?::\d{0,2}(?:\.\d{0,2})?)?$");

    public static readonly StyledProperty<double> TimeValueProperty =
        AvaloniaProperty.Register<TimeEditorControl, double>(
            nameof(TimeValue),
            defaultBindingMode: BindingMode.TwoWay);

    private bool _suppressTextEvents;
    private bool _internalTimeValueUpdate;
    private string _lastValidText = "0:00.00";

    public double TimeValue
    {
        get => GetValue(TimeValueProperty);
        set => SetValue(TimeValueProperty, value);
    }

    public TimeEditorControl()
    {
        InitializeComponent();
        SetText(FormatTime(TimeValue));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != TimeValueProperty || _internalTimeValueUpdate || EditorTextBox.IsFocused)
            return;

        SetText(FormatTime(change.GetNewValue<double>()));
    }

    private void Increment()
        => TimeValue += 0.01;

    private void Decrement()
        => TimeValue = Math.Max(0, TimeValue - 0.01);

    private void OnIncrement(object? sender, RoutedEventArgs e)
    {
        Increment();
        SetText(FormatTime(TimeValue));
    }

    private void OnDecrement(object? sender, RoutedEventArgs e)
    {
        Decrement();
        SetText(FormatTime(TimeValue));
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
            return;

        foreach (var ch in e.Text)
        {
            if (!char.IsDigit(ch) && ch != ':' && ch != '.')
            {
                e.Handled = true;
                return;
            }
        }
    }

    private void OnTextChanging(object? sender, TextChangingEventArgs e)
    {
        if (_suppressTextEvents || sender is not TextBox tb)
            return;

        var text = tb.Text ?? string.Empty;
        if (!IsValidPartialTime(text))
        {
            RestoreLastValidText(tb);
            return;
        }

        _lastValidText = text;
        if (!TryParseTime(text, out var seconds))
            return;

        _internalTimeValueUpdate = true;
        try
        {
            TimeValue = seconds;
        }
        finally
        {
            _internalTimeValueUpdate = false;
        }
    }

    private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        _lastValidText = EditorTextBox.Text ?? FormatTime(TimeValue);
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        SetText(FormatTime(TimeValue));
    }

    private void RestoreLastValidText(TextBox tb)
    {
        var caretIndex = Math.Min(tb.CaretIndex - 1, _lastValidText.Length);
        SetText(_lastValidText, Math.Max(caretIndex, 0));
    }

    private void SetText(string text, int? caretIndex = null)
    {
        _suppressTextEvents = true;
        try
        {
            EditorTextBox.Text = text;
            EditorTextBox.CaretIndex = caretIndex ?? text.Length;
            _lastValidText = text;
        }
        finally
        {
            _suppressTextEvents = false;
        }
    }

    private static bool IsValidPartialTime(string text)
        => PartialTimePattern.IsMatch(text);

    private static bool TryParseTime(string text, out double totalSeconds)
    {
        var match = ExactTimePattern.Match(text);
        if (!match.Success)
        {
            totalSeconds = 0;
            return false;
        }

        var minutes = double.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture);
        var seconds = double.Parse(match.Groups["seconds"].Value, CultureInfo.InvariantCulture);
        var fraction = double.Parse(match.Groups["fraction"].Value, CultureInfo.InvariantCulture);

        totalSeconds = minutes * 60 + seconds + (fraction / 100);
        return true;
    }

    private static string FormatTime(double totalSeconds)
    {
        var totalHundredths = (long)Math.Round(Math.Max(0, totalSeconds) * 100, MidpointRounding.AwayFromZero);
        var totalMinutes = totalHundredths / 6000;
        var seconds = (totalHundredths % 6000) / 100;
        var hundredths = totalHundredths % 100;

        return $"{totalMinutes}:{seconds:00}.{hundredths:00}";
    }
}
