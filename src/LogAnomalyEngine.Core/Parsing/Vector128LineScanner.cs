using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace LogAnomalyEngine.Core.Parsing;

public static class Vector128LineScanner
{
    private const byte LineFeed = (byte)'\n';

    public static int FindNextLineFeed(
        ReadOnlySpan<byte> buffer,
        int startIndex = 0)
    {
        if (startIndex < 0 || startIndex > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (!Vector128.IsHardwareAccelerated)
        {
            return ScalarLineScanner.FindNextLineFeed(
                buffer,
                startIndex);
        }

        var remainingLength = buffer.Length - startIndex;

        if (remainingLength < Vector128<byte>.Count)
        {
            return ScalarLineScanner.FindNextLineFeed(
                buffer,
                startIndex);
        }

        var target = Vector128.Create(LineFeed);
        ref var bufferReference = ref MemoryMarshal.GetReference(buffer);

        var vectorEnd = buffer.Length - Vector128<byte>.Count;
        var index = startIndex;

        while (index <= vectorEnd)
        {
            var current = Vector128.LoadUnsafe(
                ref bufferReference,
                (nuint)index);

            var matches = Vector128.Equals(
                current,
                target);

            var mask = Vector128.ExtractMostSignificantBits(matches);

            if (mask != 0)
            {
                var matchOffset = BitOperations.TrailingZeroCount(mask);

                return index + matchOffset;
            }

            index += Vector128<byte>.Count;
        }

        return ScalarLineScanner.FindNextLineFeed(
            buffer,
            index);
    }
}
