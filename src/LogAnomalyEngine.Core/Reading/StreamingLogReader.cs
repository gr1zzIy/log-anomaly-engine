using System.Buffers;
using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Core.Reading;

public static class StreamingLogReader
{
    public const int DefaultBufferSize = 64 * 1024;

    private const byte CarriageReturn = (byte)'\r';

    public static long ReadLines(
        Stream stream,
        LogLineHandler handler,
        int bufferSize = DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(handler);

        if (!stream.CanRead)
        {
            throw new ArgumentException(
                "The supplied stream must be readable.",
                nameof(stream));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var capacity = bufferSize;
        var bufferedCount = 0;
        long lineCount = 0;

        try
        {
            while (true)
            {
                if (bufferedCount == capacity)
                {
                    GrowBuffer(ref buffer, ref capacity, bufferedCount);
                }

                var bytesRead = stream.Read(
                    buffer.AsSpan(
                        bufferedCount,
                        capacity - bufferedCount));

                if (bytesRead == 0)
                {
                    if (bufferedCount > 0)
                    {
                        handler(TrimCarriageReturn(
                            buffer.AsSpan(0, bufferedCount)));

                        lineCount++;
                    }

                    return lineCount;
                }

                var availableCount = bufferedCount + bytesRead;
                var lineStart = 0;

                while (true)
                {
                    var lineFeedIndex = ScalarLineScanner.FindNextLineFeed(
                        buffer.AsSpan(0, availableCount),
                        lineStart);

                    if (lineFeedIndex < 0)
                    {
                        break;
                    }

                    var line = buffer.AsSpan(
                        lineStart,
                        lineFeedIndex - lineStart);

                    handler(TrimCarriageReturn(line));
                    lineCount++;

                    lineStart = lineFeedIndex + 1;
                }

                bufferedCount = availableCount - lineStart;

                if (bufferedCount > 0 && lineStart > 0)
                {
                    buffer.AsSpan(lineStart, bufferedCount)
                        .CopyTo(buffer);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void GrowBuffer(
        ref byte[] buffer,
        ref int capacity,
        int bufferedCount)
    {
        var newCapacity = checked(capacity * 2);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);

        buffer.AsSpan(0, bufferedCount)
            .CopyTo(newBuffer);

        ArrayPool<byte>.Shared.Return(buffer);

        buffer = newBuffer;
        capacity = newCapacity;
    }

    private static ReadOnlySpan<byte> TrimCarriageReturn(
        ReadOnlySpan<byte> line)
    {
        if (!line.IsEmpty && line[^1] == CarriageReturn)
        {
            return line[..^1];
        }

        return line;
    }
}
