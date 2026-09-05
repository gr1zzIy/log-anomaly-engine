using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Benchmarks.Infrastructure;
using LogAnomalyEngine.Core.Events;
using LogAnomalyEngine.Core.Reading;

namespace LogAnomalyEngine.Benchmarks.Reading;

[MemoryDiagnoser]
public class StructuredEventPipelineBenchmarks : IDisposable
{
    private const int DatasetSize = 16_000_000;
    private const int BufferSize = 64 * 1024;

    private static readonly LogLineHandler RawHandler =
        ConsumeLine;

    private static readonly StructuredLogEventCallback StructuredHandler =
        ConsumeEvent;

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
        var data =
            LogDatasetGenerator.CreateStructuredFixedWidth(
                DatasetSize,
                LineLength);

        _stream = new MemoryStream(
            data,
            writable: false);
    }

    [Benchmark(Baseline = true)]
    public long FramingOnly()
    {
        _stream.Position = 0;

        return StreamingLogReader.ReadLines(
            _stream,
            RawHandler,
            BufferSize);
    }

    [Benchmark]
    public long Structured()
    {
        _stream.Position = 0;

        var result = StructuredLogReader.ReadEvents(
            _stream,
            StructuredHandler,
            BufferSize);

        return result.ParsedEvents;
    }

    public void Dispose()
    {
        _stream?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void ConsumeLine(
        ReadOnlySpan<byte> line)
    {
    }

    private static void ConsumeEvent(
        LogEventView logEvent)
    {
    }
}
