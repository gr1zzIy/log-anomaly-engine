using System.Text;
using BenchmarkDotNet.Attributes;
using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Benchmarks.Parsing;

[MemoryDiagnoser]
public class StructuredLogParserBenchmarks
{
    private byte[] _line = null!;

    [Params(32, 256, 4096)]
    public int MessageLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var message = new string('A', MessageLength);

        _line = Encoding.UTF8.GetBytes(
            $"2026-09-05T20:00:00Z INFO PaymentService {message}");
    }

    [Benchmark(Baseline = true)]
    public int StringBased()
    {
        var text = Encoding.UTF8.GetString(_line);

        var parts = text.Split(
            ' ',
            4,
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4)
        {
            return -1;
        }

        return parts[0].Length +
               parts[1].Length +
               parts[2].Length +
               parts[3].Length;
    }

    [Benchmark]
    public int ZeroCopy()
    {
        if (!StructuredLogParser.TryParse(
                _line,
                out var logEvent))
        {
            return -1;
        }

        return logEvent.Timestamp.Length +
               (int)logEvent.Level +
               logEvent.Source.Length +
               logEvent.Message.Length;
    }
}
