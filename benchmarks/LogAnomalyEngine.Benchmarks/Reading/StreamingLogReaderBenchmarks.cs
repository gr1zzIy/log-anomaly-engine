using System.Text;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Reading;

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
        var data = CreateDataset(DatasetSize);
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

    private static byte[] CreateDataset(int targetSize)
    {
        const string line =
            "2026-09-04T10:15:30.123Z INFO OrderService Request completed successfully id=123456 duration=42ms\n";

        var lineBytes = Encoding.UTF8.GetBytes(line);
        var lineCount = targetSize / lineBytes.Length;
        var data = new byte[lineCount * lineBytes.Length];

        for (var offset = 0; offset < data.Length; offset += lineBytes.Length)
        {
            lineBytes.CopyTo(data, offset);
        }

        return data;
    }
}
