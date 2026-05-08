// Copyright (C) Neurosoft

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SpeechMarkupEditor.Infrastructure.Formatting;

namespace SpeechMarkupEditor.Controls;

public partial class TimeEditorControl : UserControl
{
    private const double MinimumRangeGapSeconds = 0.001;

    public static readonly StyledProperty<double> TimeValueProperty =
        AvaloniaProperty.Register<TimeEditorControl, double>(
            nameof(TimeValue),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsStartEditorProperty =
        AvaloniaProperty.Register<TimeEditorControl, bool>(nameof(IsStartEditor));

    private bool _suppressEvents;
    private bool _internalTimeValueUpdate;
    public double TimeValue
    {
        get => GetValue(TimeValueProperty);
        set => SetValue(TimeValueProperty, value);
    }

    public bool IsStartEditor
    {
        get => GetValue(IsStartEditorProperty);
        set => SetValue(IsStartEditorProperty, value);
    }

    public TimeEditorControl()
    {
        InitializeComponent();
        ApplyTimeToFields(TimeValue);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != TimeValueProperty || _internalTimeValueUpdate)
            return;

        ApplyTimeToFields(change.GetNewValue<double>());
    }

    private void OnIncrement(object? sender, RoutedEventArgs e)
    {
        SetTimeValue(ClampToWordBounds(TimeValue + 0.001));
        ApplyTimeToFields(TimeValue);
    }

    private void OnDecrement(object? sender, RoutedEventArgs e)
    {
        SetTimeValue(ClampToWordBounds(TimeValue - 0.001));
        ApplyTimeToFields(TimeValue);
    }

    private void OnTimeTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressEvents)
            return;

        if (TimeTextFormatter.TryParse(TimeTextBox.Text, out var parsedSeconds))
            SetTimeValue(ClampToWordBounds(parsedSeconds));
    }

    private void OnTimeTextBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        TimeTextBox.SelectionStart = 0;
        TimeTextBox.SelectionEnd = TimeTextBox.Text?.Length ?? 0;
    }

    private void OnTimeTextBoxLostFocus(object? sender, RoutedEventArgs e)
        => CommitOrRestoreOnBlur();

    private void NormalizeAndCommit()
    {
        SetTimeValue(ParseTimeTextOrDefault(TimeTextBox.Text));
        ApplyTimeToFields(TimeValue);
    }

    private void CommitOrRestoreOnBlur()
    {
        Dispatcher.UIThread.Post(() =>
        {
            NormalizeAndCommit();
        });
    }

    private double ClampToWordBounds(double candidateSeconds)
    {
        candidateSeconds = Math.Clamp(candidateSeconds, 0, TimeTextFormatter.MaxTimeSeconds);

        if (DataContext is not Models.WordTimestamp word)
            return candidateSeconds;

        return IsStartEditor
            ? Math.Min(candidateSeconds, Math.Max(0, word.EndTime - MinimumRangeGapSeconds))
            : Math.Max(candidateSeconds, Math.Min(TimeTextFormatter.MaxTimeSeconds, word.StartTime + MinimumRangeGapSeconds));
    }

    private void ApplyTimeToFields(double totalSeconds)
    {
        _suppressEvents = true;
        try
        {
            TimeTextBox.Text = TimeTextFormatter.Format(totalSeconds);
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void SetTimeValue(double seconds)
    {
        _internalTimeValueUpdate = true;
        try
        {
            TimeValue = Math.Clamp(seconds, 0, TimeTextFormatter.MaxTimeSeconds);
        }
        finally
        {
            _internalTimeValueUpdate = false;
        }
    }

    private double ParseTimeTextOrDefault(string? text)
    {
        if (!TimeTextFormatter.TryParse(text, out var parsedSeconds))
            return ClampToWordBounds(TimeValue);

        return ClampToWordBounds(parsedSeconds);
    }
}
