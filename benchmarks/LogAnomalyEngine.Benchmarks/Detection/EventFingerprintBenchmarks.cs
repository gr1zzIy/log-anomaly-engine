using System.Text;
using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Benchmarks.Detection;

[MemoryDiagnoser]
public class EventFingerprintBenchmarks
{
    private byte[] _rawLine = null!;

    private int _sourceStart;
    private int _sourceEnd;
    private int _messageStart;

    [Params(32, 256, 4096)]
    public int MessageLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        const string timestamp = "2026-09-05T20:00:00Z";
        const string source = "PaymentService";

        var message = CreateMessage(MessageLength);

        _rawLine = Encoding.UTF8.GetBytes(
            $"{timestamp}{source}{message}");

        _sourceStart = timestamp.Length;
        _sourceEnd = _sourceStart + source.Length;
        _messageStart = _sourceEnd;
    }

    [Benchmark]
    public ulong Compute()
    {
        var logEvent = new LogEventView(
            _rawLine,
            timestampRange: 0..20,
            level: LogLevel.Error,
            sourceRange: _sourceStart.._sourceEnd,
            messageRange: _messageStart.._rawLine.Length);

        return EventFingerprint.Compute(logEvent);
    }

    private static string CreateMessage(int targetLength)
    {
        const string pattern =
            " Request 123 failed on node 456 after 789 attempts";

        var builder = new StringBuilder(targetLength);

        while (builder.Length < targetLength)
        {
            builder.Append(pattern);
        }

        if (builder.Length > targetLength)
        {
            builder.Length = targetLength;
        }

        return builder.ToString();
    }
}
