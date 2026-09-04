using System.Text;

namespace LogAnomalyEngine.Benchmarks.Infrastructure;

internal static class LogDatasetGenerator
{
    private const string LogLine =
        "2026-09-04T10:15:30.123Z INFO OrderService Request completed successfully id=123456 duration=42ms\n";

    private static readonly byte[] LogLineBytes = Encoding.UTF8.GetBytes(LogLine);

    public static byte[] Create(int targetSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetSize);

        var lineCount = targetSize / LogLineBytes.Length;

        if (lineCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetSize),
                "Target size must be large enough to contain at least one log line.");
        }

        var data = new byte[lineCount * LogLineBytes.Length];

        for (var offset = 0; offset < data.Length; offset += LogLineBytes.Length)
        {
            LogLineBytes.CopyTo(data, offset);
        }

        return data;
    }

    public static byte[] CreateFixedWidth(
        int targetSize,
        int lineLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetSize);

        if (lineLength < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineLength),
                "Line length must be at least two bytes.");
        }

        var lineCount = targetSize / lineLength;

        if (lineCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetSize),
                "Target size must be large enough to contain at least one line.");
        }

        var data = new byte[lineCount * lineLength];

        for (var offset = 0; offset < data.Length; offset += lineLength)
        {
            data.AsSpan(offset, lineLength - 1)
                .Fill((byte)'A');

            data[offset + lineLength - 1] = (byte)'\n';
        }

        return data;
    }
}
