// Copyright (C) Neurosoft

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SpeechMarkupEditor.Infrastructure.Formatting;

namespace SpeechMarkupEditor.Infrastructure.Converters;

public class TimeToStringConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double time)
            return TimeTextFormatter.Format(time);

        return TimeTextFormatter.Format(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && TimeTextFormatter.TryParse(text, out var seconds))
            return seconds;

        throw new FormatException("Invalid time format.");
    }
}
