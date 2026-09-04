using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Benchmarks.Infrastructure;
using LogAnomalyEngine.Core.Reading;

namespace LogAnomalyEngine.Benchmarks.Reading;

[MemoryDiagnoser]
public class LineLengthImpactBenchmarks : IDisposable
{
    private const int DatasetSize = 16_000_000;
    private const int BufferSize = 64 * 1024;

    private static readonly LogLineHandler LineHandler = static _ => { };

    private MemoryStream _stream = null!;

    [Params(
        125,
        1000,
        16_000,
        100_000)]
    public int LineLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var data = LogDatasetGenerator.CreateFixedWidth(
            DatasetSize,
            LineLength);

        _stream = new MemoryStream(
            data,
            writable: false);
    }

    [Benchmark]
    public long ReadLines()
    {
        _stream.Position = 0;

        return StreamingLogReader.ReadLines(
            _stream,
            LineHandler,
            BufferSize);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
