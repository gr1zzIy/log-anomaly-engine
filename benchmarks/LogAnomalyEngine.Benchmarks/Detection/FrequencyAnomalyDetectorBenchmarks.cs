using System.Text;
using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Benchmarks.Detection;

[MemoryDiagnoser]
public class FrequencyAnomalyDetectorBenchmarks
{
    private FrequencyAnomalyDetector _detector = null!;
    private byte[] _rawLine = null!;

    private int _sourceStart;
    private int _sourceEnd;
    private int _messageStart;

    [GlobalSetup]
    public void Setup()
    {
        const string timestamp = "2026-09-20T12:00:00Z";
        const string source = "PaymentService";
        const string message =
            "Payment 123 failed on node 456 after 789 attempts";

        _rawLine = Encoding.UTF8.GetBytes(
            $"{timestamp}{source}{message}");

        _sourceStart = timestamp.Length;
        _sourceEnd = _sourceStart + source.Length;
        _messageStart = _sourceEnd;

        _detector = new FrequencyAnomalyDetector();

        // Seed the model so the measured operation always updates
        // an already-known fingerprint.
        Observe();
    }

    [Benchmark]
    public FrequencyAnomalyResult KnownEvent()
    {
        return Observe();
    }

    private FrequencyAnomalyResult Observe()
    {
        var logEvent = new LogEventView(
            _rawLine,
            timestampRange: 0..20,
            level: LogLevel.Error,
            sourceRange: _sourceStart.._sourceEnd,
            messageRange: _messageStart.._rawLine.Length);

        return _detector.Observe(logEvent);
    }
}
