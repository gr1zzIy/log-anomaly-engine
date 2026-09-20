using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Benchmarks.Detection;

[MemoryDiagnoser]
public class SlidingWindowBoundedStateBenchmarks
{
    private const int TimestampLength = 20;
    private const int SourceLength = 14;

    private const int WindowSize = 1024;
    private const int DistinctEventPool = 4096;

    private byte[][] _events = null!;

    [Params(
        8192,
        65536)]
    public int EventCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _events =
            new byte[DistinctEventPool][];

        for (var i = 0;
             i < _events.Length;
             i++)
        {
            var source =
                $"Worker{i.ToString(
                    "D8",
                    CultureInfo.InvariantCulture)}";

            _events[i] =
                Encoding.UTF8.GetBytes(
                    $"2026-09-20T12:00:00Z{source}Request 123 completed");
        }
    }

    [Benchmark]
    public int ProcessChurningStream()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                WindowSize);

        for (var i = 0;
             i < EventCount;
             i++)
        {
            var rawLine =
                _events[i % _events.Length];

            var sourceEnd =
                TimestampLength +
                SourceLength;

            var logEvent =
                new LogEventView(
                    rawLine,
                    timestampRange:
                        0..TimestampLength,
                    level:
                        LogLevel.Information,
                    sourceRange:
                        TimestampLength..
                        sourceEnd,
                    messageRange:
                        sourceEnd..
                        rawLine.Length);

            detector.Observe(
                logEvent);
        }

        return detector
            .DistinctFingerprintCount;
    }
}
