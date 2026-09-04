using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Tests.Events;

public sealed class LogEventViewTests
{
    [Fact]
    public void Constructor_ValidRanges_ExposesExpectedFields()
    {
        var rawLine =
            "2026-09-04T20:00:00Z INFO PaymentService Payment completed"u8;

        var logEvent = new LogEventView(
            rawLine,
            timestampRange: 0..20,
            level: LogLevel.Information,
            sourceRange: 26..40,
            messageRange: 41..58);

        Assert.True(
            logEvent.RawLine.SequenceEqual(rawLine));

        Assert.True(
            logEvent.Timestamp.SequenceEqual(
                "2026-09-04T20:00:00Z"u8));

        Assert.Equal(
            LogLevel.Information,
            logEvent.Level);

        Assert.True(
            logEvent.Source.SequenceEqual(
                "PaymentService"u8));

        Assert.True(
            logEvent.Message.SequenceEqual(
                "Payment completed"u8));
    }

    [Fact]
    public void Constructor_EmptyOptionalRanges_ReturnsEmptySpans()
    {
        var rawLine = "application started"u8;

        var logEvent = new LogEventView(
            rawLine,
            timestampRange: 0..0,
            level: LogLevel.Unknown,
            sourceRange: 0..0,
            messageRange: 0..rawLine.Length);

        Assert.True(logEvent.Timestamp.IsEmpty);
        Assert.True(logEvent.Source.IsEmpty);

        Assert.True(
            logEvent.Message.SequenceEqual(rawLine));
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(20, 10)]
    public void Constructor_InvalidTimestampRange_ThrowsArgumentOutOfRangeException(
        int start,
        int end)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateLogEventWithTimestampRange(start, end));
    }

    private static void CreateLogEventWithTimestampRange(
        int start,
        int end)
    {
        ReadOnlySpan<byte> rawLine = "short line"u8;

        _ = new LogEventView(
            rawLine,
            timestampRange: new Range(start, end),
            level: LogLevel.Unknown,
            sourceRange: 0..0,
            messageRange: 0..rawLine.Length);
    }
}
