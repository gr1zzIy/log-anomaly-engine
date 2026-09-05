using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Core.Detection;

public static class EventFingerprint
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    // 0xFE та 0xFF не є валідними байтами UTF-8, тому можемо
    // безпечно використовувати їх як службові маркери fingerprint.
    private const byte NumericRunMarker = 0xFE;
    private const byte FieldSeparator = 0xFF;

    public static ulong Compute(LogEventView logEvent)
    {
        var hash = OffsetBasis;

        hash = AddByte(hash, (byte)logEvent.Level);
        hash = AddByte(hash, FieldSeparator);

        hash = AddBytes(hash, logEvent.Source);
        hash = AddByte(hash, FieldSeparator);

        hash = AddNormalizedMessage(
            hash,
            logEvent.Message);

        return hash;
    }

    private static ulong AddNormalizedMessage(
        ulong hash,
        ReadOnlySpan<byte> message)
    {
        var insideNumericRun = false;

        foreach (var value in message)
        {
            if (IsAsciiDigit(value))
            {
                // Незалежно від кількості цифр у значенні весь числовий
                // фрагмент представлений одним стабільним маркером.
                if (!insideNumericRun)
                {
                    hash = AddByte(
                        hash,
                        NumericRunMarker);

                    insideNumericRun = true;
                }

                continue;
            }

            insideNumericRun = false;
            hash = AddByte(hash, value);
        }

        return hash;
    }

    private static ulong AddBytes(
        ulong hash,
        ReadOnlySpan<byte> values)
    {
        foreach (var value in values)
        {
            hash = AddByte(hash, value);
        }

        return hash;
    }

    private static ulong AddByte(
        ulong hash,
        byte value)
    {
        return unchecked(
            (hash ^ value) * Prime);
    }

    private static bool IsAsciiDigit(byte value)
    {
        return value is >= (byte)'0' and <= (byte)'9';
    }
}
