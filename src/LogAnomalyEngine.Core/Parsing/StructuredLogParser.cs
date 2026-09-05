using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Core.Parsing;

public static class StructuredLogParser
{
    private const byte Space = (byte)' ';
    private const byte Tab = (byte)'\t';

    public static bool TryParse(
        ReadOnlySpan<byte> line,
        out LogEventView logEvent)
    {
        logEvent = default;

        var position = 0;

        if (!TryReadToken(
                line,
                ref position,
                out var timestampStart,
                out var timestampLength))
        {
            return false;
        }

        if (!TryReadToken(
                line,
                ref position,
                out var levelStart,
                out var levelLength))
        {
            return false;
        }

        if (!TryReadToken(
                line,
                ref position,
                out var sourceStart,
                out var sourceLength))
        {
            return false;
        }

        SkipSeparators(line, ref position);

        var level = ParseLevel(
            line.Slice(levelStart, levelLength));

        logEvent = new LogEventView(
            line,
            timestampRange:
            timestampStart..(timestampStart + timestampLength),
            level,
            sourceRange:
            sourceStart..(sourceStart + sourceLength),
            messageRange:
            position..line.Length);

        return true;
    }

    private static bool TryReadToken(
        ReadOnlySpan<byte> line,
        ref int position,
        out int start,
        out int length)
    {
        SkipSeparators(line, ref position);

        start = position;

        while (position < line.Length &&
               !IsSeparator(line[position]))
        {
            position++;
        }

        length = position - start;

        return length > 0;
    }

    private static void SkipSeparators(
        ReadOnlySpan<byte> line,
        ref int position)
    {
        while (position < line.Length &&
               IsSeparator(line[position]))
        {
            position++;
        }
    }

    private static bool IsSeparator(byte value)
    {
        return value is Space or Tab;
    }

    private static LogLevel ParseLevel(
        ReadOnlySpan<byte> value)
    {
        if (EqualsAsciiIgnoreCase(value, "TRACE"u8))
        {
            return LogLevel.Trace;
        }

        if (EqualsAsciiIgnoreCase(value, "DEBUG"u8))
        {
            return LogLevel.Debug;
        }

        if (EqualsAsciiIgnoreCase(value, "INFO"u8) ||
            EqualsAsciiIgnoreCase(value, "INFORMATION"u8))
        {
            return LogLevel.Information;
        }

        if (EqualsAsciiIgnoreCase(value, "WARN"u8) ||
            EqualsAsciiIgnoreCase(value, "WARNING"u8))
        {
            return LogLevel.Warning;
        }

        if (EqualsAsciiIgnoreCase(value, "ERROR"u8))
        {
            return LogLevel.Error;
        }

        if (EqualsAsciiIgnoreCase(value, "FATAL"u8) ||
            EqualsAsciiIgnoreCase(value, "CRITICAL"u8))
        {
            return LogLevel.Critical;
        }

        return LogLevel.Unknown;
    }

    private static bool EqualsAsciiIgnoreCase(
        ReadOnlySpan<byte> value,
        ReadOnlySpan<byte> expectedUppercase)
    {
        if (value.Length != expectedUppercase.Length)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (current is >= (byte)'a' and <= (byte)'z')
            {
                current = (byte)(current - ('a' - 'A'));
            }

            if (current != expectedUppercase[i])
            {
                return false;
            }
        }

        return true;
    }
}
