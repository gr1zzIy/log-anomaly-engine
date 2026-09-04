using System.Buffers;
using LogAnomalyEngine.Core.Reading;

namespace LogAnomalyEngine.Benchmarks.Reading;

internal static class ScannerComparisonReader
{
    private const byte CarriageReturn = (byte)'\r';

    public static long ReadLines(
        Stream stream,
        LogLineHandler handler,
        int bufferSize,
        Func<ReadOnlySpan<byte>, int, int> findNextLineFeed)
    {
        throw new NotSupportedException();
    }
}
