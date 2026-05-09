using System;
using System.Globalization;

namespace SpeechMarkupEditor.Infrastructure.Formatting;

public static class TimeTextFormatter
{
    public const double MAX_TIME_SECONDS = 60 * 60 + 59.999;
    private const int EXPECTED_DIGITS_COUNT = 7;

    public static string Format(double totalSeconds)
    {
        long totalMilliseconds = (long)Math.Round(
            Math.Clamp(totalSeconds, 0, MAX_TIME_SECONDS) * 1000,
            MidpointRounding.AwayFromZero);
        long minutes = totalMilliseconds / 60000;
        long seconds = totalMilliseconds % 60000 / 1000;
        long milliseconds = totalMilliseconds % 1000;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{minutes:00}:{seconds:00}.{milliseconds:000}");
    }

    public static bool TryParse(string? text, out double seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string digits = ExtractDigits(text, EXPECTED_DIGITS_COUNT);
        if (digits.Length != EXPECTED_DIGITS_COUNT)
            return false;

        int minutes = int.Parse(digits[..2], CultureInfo.InvariantCulture);
        int wholeSeconds = int.Parse(digits.Substring(2, 2), CultureInfo.InvariantCulture);
        int milliseconds = int.Parse(digits.Substring(4, 3), CultureInfo.InvariantCulture);

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
        int count = 0;
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
