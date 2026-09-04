using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Benchmarks.Parsing;

[MemoryDiagnoser]
public class LineFeedScannerBenchmarks
{
    private byte[] _buffer = null!;

    [Params(
        32,
        128,
        1024,
        16 * 1024,
        64 * 1024)]
    public int BufferLength { get; set; }

    [Params(false, true)]
    public bool HasLineFeed { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[BufferLength];
        Array.Fill(_buffer, (byte)'A');

        if (HasLineFeed)
        {
            // Ставимо delimiter наприкінці, щоб усі реалізації
            // проходили майже весь buffer і виконували порівнювану роботу.
            _buffer[^1] = (byte)'\n';
        }
    }

    [Benchmark(Baseline = true)]
    public int Scalar()
    {
        return ScalarLineScanner.FindNextLineFeed(_buffer);
    }

    [Benchmark]
    public int Vector128()
    {
        return Vector128LineScanner.FindNextLineFeed(_buffer);
    }

    [Benchmark]
    public int RuntimeIndexOf()
    {
        return _buffer.AsSpan().IndexOf((byte)'\n');
    }
}
