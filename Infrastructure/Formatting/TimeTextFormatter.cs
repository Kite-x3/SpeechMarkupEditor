using System;
using System.Globalization;

namespace SpeechMarkupEditor.Infrastructure.Formatting;

public static class TimeTextFormatter
{
    public const double MaxTimeSeconds = 60 * 60 + 59.999;
    private const int ExpectedDigitsCount = 7;

    public static string Format(double totalSeconds)
    {
        var totalMilliseconds = (long)Math.Round(
            Math.Clamp(totalSeconds, 0, MaxTimeSeconds) * 1000,
            MidpointRounding.AwayFromZero);
        var minutes = totalMilliseconds / 60000;
        var seconds = (totalMilliseconds % 60000) / 1000;
        var milliseconds = totalMilliseconds % 1000;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{minutes:00}:{seconds:00}.{milliseconds:000}");
    }

    public static bool TryParse(string? text, out double seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var digits = ExtractDigits(text, ExpectedDigitsCount);
        if (digits.Length != ExpectedDigitsCount)
            return false;

        var minutes = int.Parse(digits[..2], CultureInfo.InvariantCulture);
        var wholeSeconds = int.Parse(digits.Substring(2, 2), CultureInfo.InvariantCulture);
        var milliseconds = int.Parse(digits.Substring(4, 3), CultureInfo.InvariantCulture);

        minutes = Math.Clamp(minutes, 0, 60);
        wholeSeconds = Math.Clamp(wholeSeconds, 0, 59);
        milliseconds = Math.Clamp(milliseconds, 0, 999);

        seconds = minutes * 60 + wholeSeconds + milliseconds / 1000.0;
        return true;
    }

    private static string ExtractDigits(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        Span<char> buffer = stackalloc char[Math.Max(maxLength, 1)];
        var count = 0;
        foreach (var ch in text)
        {
            if (!char.IsDigit(ch))
                continue;

            if (count == buffer.Length)
                break;

            buffer[count++] = ch;
        }

        return new string(buffer[..count]);
    }
}
