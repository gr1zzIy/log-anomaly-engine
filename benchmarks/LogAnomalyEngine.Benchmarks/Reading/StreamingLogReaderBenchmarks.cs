using System.Text;
using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Reading;
using LogAnomalyEngine.Benchmarks.Infrastructure;

namespace LogAnomalyEngine.Benchmarks.Reading;

[MemoryDiagnoser]
public class StreamingLogReaderBenchmarks : IDisposable
{
    private const int DatasetSize = 16 * 1024 * 1024;

    private static readonly LogLineHandler LineHandler = static _ => { };

    private MemoryStream _stream = null!;

    [Params(4 * 1024, 64 * 1024, 1024 * 1024)]
    public int BufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var data = LogDatasetGenerator.Create(DatasetSize);
        _stream = new MemoryStream(data, writable: false);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
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

    public void Dispose()
    {
        _stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
