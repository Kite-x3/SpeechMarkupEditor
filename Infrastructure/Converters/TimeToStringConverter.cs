// Copyright (C) Neurosoft

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SpeechMarkupEditor.Infrastructure.Converters;

public class TimeToStringConverter: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double time)
        {
            var timeSpan = TimeSpan.FromSeconds(time);
            return timeSpan.TotalHours >= 1
                ? timeSpan.ToString(@"hh\:mm\:ss\.ff", CultureInfo.InvariantCulture)
                : timeSpan.ToString(@"mm\:ss\.ff", CultureInfo.InvariantCulture);
        }

        return "00:00.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}