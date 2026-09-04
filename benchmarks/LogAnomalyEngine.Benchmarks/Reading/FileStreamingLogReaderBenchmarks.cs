using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Benchmarks.Infrastructure;
using LogAnomalyEngine.Core.Reading;

namespace LogAnomalyEngine.Benchmarks.Reading;

[MemoryDiagnoser]
public class FileStreamingLogReaderBenchmarks : IDisposable
{
    private const int DatasetSize = 64 * 1024 * 1024;

    private static readonly LogLineHandler LineHandler = static _ => { };

    private string _filePath = null!;

    [Params(4 * 1024, 64 * 1024, 1024 * 1024)]
    public int BufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var data = LogDatasetGenerator.Create(DatasetSize);

        _filePath = Path.Combine(
            Path.GetTempPath(),
            $"log-anomaly-engine-{Guid.NewGuid():N}.log");

        File.WriteAllBytes(_filePath, data);
    }

    [Benchmark]
    public long ReadLines()
    {
        using var stream = new FileStream(
            _filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1,
            FileOptions.SequentialScan);

        return StreamingLogReader.ReadLines(
            stream,
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
        if (!string.IsNullOrEmpty(_filePath))
        {
            File.Delete(_filePath);
        }

        GC.SuppressFinalize(this);
    }
}
