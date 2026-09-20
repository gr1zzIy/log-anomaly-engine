using System.Text;
using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Benchmarks.Detection;

[MemoryDiagnoser]
public class SlidingWindowFrequencyDetectorBenchmarks
{
    private FrequencyAnomalyDetector _cumulative = null!;
    private SlidingWindowFrequencyDetector _sliding = null!;

    private byte[] _rawLine = null!;

    private int _sourceStart;
    private int _sourceEnd;
    private int _messageStart;

    [Params(
        128,
        1024,
        8192)]
    public int WindowSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        const string timestamp =
            "2026-09-20T12:00:00Z";

        const string source =
            "PaymentService";

        const string message =
            "Payment 123 failed on node 456 after 789 attempts";

        _rawLine = Encoding.UTF8.GetBytes(
            $"{timestamp}{source}{message}");

        _sourceStart = timestamp.Length;
        _sourceEnd =
            _sourceStart + source.Length;

        _messageStart =
            _sourceEnd;

        _cumulative =
            new FrequencyAnomalyDetector();

        _sliding =
            new SlidingWindowFrequencyDetector(
                WindowSize);

        // Seed cumulative detector.
        ObserveCumulative();

        // Fill the complete sliding window so the measured
        // operation includes steady-state eviction + insertion.
        for (var i = 0; i < WindowSize; i++)
        {
            ObserveSliding();
        }
    }

    [Benchmark(Baseline = true)]
    public FrequencyAnomalyResult CumulativeKnownEvent()
    {
        return ObserveCumulative();
    }

    [Benchmark]
    public SlidingWindowAnomalyResult SlidingWindowKnownEvent()
    {
        return ObserveSliding();
    }

    private FrequencyAnomalyResult ObserveCumulative()
    {
        var logEvent = new LogEventView(
            _rawLine,
            timestampRange: 0..20,
            level: LogLevel.Error,
            sourceRange:
                _sourceStart.._sourceEnd,
            messageRange:
                _messageStart.._rawLine.Length);

        return _cumulative.Observe(
            logEvent);
    }

    private SlidingWindowAnomalyResult ObserveSliding()
    {
        var logEvent = new LogEventView(
            _rawLine,
            timestampRange: 0..20,
            level: LogLevel.Error,
            sourceRange:
                _sourceStart.._sourceEnd,
            messageRange:
                _messageStart.._rawLine.Length);

        return _sliding.Observe(
            logEvent);
    }
}
