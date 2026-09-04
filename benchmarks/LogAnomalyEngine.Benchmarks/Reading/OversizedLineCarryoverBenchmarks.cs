using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Benchmarks.Infrastructure;
using LogAnomalyEngine.Core.Reading;

namespace LogAnomalyEngine.Benchmarks.Reading;

[MemoryDiagnoser]
public class OversizedLineCarryoverBenchmarks : IDisposable
{
    private const int DatasetSize = 64_000_000;
    private const int LineLength = 100_000;
    private const int BufferSize = 64 * 1024;

    private static readonly LogLineHandler LineHandler = static _ => { };

    private LineBoundaryReadStream _unalignedStream = null!;
    private LineBoundaryReadStream _lineAlignedStream = null!;

    [GlobalSetup]
    public void Setup()
    {
        var data = LogDatasetGenerator.CreateFixedWidth(
            DatasetSize,
            LineLength);

        _unalignedStream = new LineBoundaryReadStream(
            data,
            LineLength,
            alignReadsToLineBoundaries: false);

        _lineAlignedStream = new LineBoundaryReadStream(
            data,
            LineLength,
            alignReadsToLineBoundaries: true);
    }

    [Benchmark(Baseline = true)]
    public long UnalignedReads()
    {
        _unalignedStream.Reset();

        return StreamingLogReader.ReadLines(
            _unalignedStream,
            LineHandler,
            BufferSize);
    }

    [Benchmark]
    public long LineAlignedReads()
    {
        _lineAlignedStream.Reset();

        return StreamingLogReader.ReadLines(
            _lineAlignedStream,
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
        _unalignedStream?.Dispose();
        _lineAlignedStream?.Dispose();

        GC.SuppressFinalize(this);
    }
}
