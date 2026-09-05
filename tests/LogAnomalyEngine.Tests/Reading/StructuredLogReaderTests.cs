using System.Text;
using LogAnomalyEngine.Core.Events;
using LogAnomalyEngine.Core.Reading;

namespace LogAnomalyEngine.Tests.Reading;

public sealed class StructuredLogReaderTests
{
    [Fact]
    public void ReadEvents_ValidLines_DeliversStructuredEvents()
    {
        const string content =
            """
            2026-09-05T20:00:00Z INFO PaymentService Payment completed
            2026-09-05T20:00:01Z ERROR Worker Queue failed
            """;

        using var stream = CreateStream(content);

        var levels = new List<LogLevel>();
        var sources = new List<string>();
        var messages = new List<string>();

        var result = StructuredLogReader.ReadEvents(
            stream,
            logEvent =>
            {
                levels.Add(logEvent.Level);

                sources.Add(
                    Encoding.UTF8.GetString(logEvent.Source));

                messages.Add(
                    Encoding.UTF8.GetString(logEvent.Message));
            });

        Assert.Equal(2, result.TotalLines);
        Assert.Equal(2, result.ParsedEvents);
        Assert.Equal(0, result.MalformedLines);

        Assert.Equal(
            [LogLevel.Information, LogLevel.Error],
            levels);

        Assert.Equal(
            ["PaymentService", "Worker"],
            sources);

        Assert.Equal(
            ["Payment completed", "Queue failed"],
            messages);
    }

    [Fact]
    public void ReadEvents_MalformedLine_ContinuesProcessing()
    {
        const string content =
            """
            2026-09-05T20:00:00Z INFO Worker first
            malformed
            2026-09-05T20:00:02Z WARN Worker third
            """;

        using var stream = CreateStream(content);

        var handledEvents = 0;

        var result = StructuredLogReader.ReadEvents(
            stream,
            _ => handledEvents++);

        Assert.Equal(3, result.TotalLines);
        Assert.Equal(2, result.ParsedEvents);
        Assert.Equal(1, result.MalformedLines);
        Assert.Equal(2, handledEvents);
    }

    [Fact]
    public void ReadEvents_AllMalformedLines_ReturnsExpectedCounts()
    {
        const string content =
            """
            malformed
            timestamp INFO
            another-invalid-line
            """;

        using var stream = CreateStream(content);

        var handledEvents = 0;

        var result = StructuredLogReader.ReadEvents(
            stream,
            _ => handledEvents++);

        Assert.Equal(3, result.TotalLines);
        Assert.Equal(0, result.ParsedEvents);
        Assert.Equal(3, result.MalformedLines);
        Assert.Equal(0, handledEvents);
    }

    [Fact]
    public void ReadEvents_EmptyStream_ReturnsZeroCounts()
    {
        using var stream = CreateStream(string.Empty);

        var handledEvents = 0;

        var result = StructuredLogReader.ReadEvents(
            stream,
            _ => handledEvents++);

        Assert.Equal(0, result.TotalLines);
        Assert.Equal(0, result.ParsedEvents);
        Assert.Equal(0, result.MalformedLines);
        Assert.Equal(0, handledEvents);
    }

    [Fact]
    public void ReadEvents_Utf8Message_PreservesContent()
    {
        const string content =
            "2026-09-05T20:00:00Z ERROR Worker Помилка підключення";

        using var stream = CreateStream(content);

        string? message = null;

        var result = StructuredLogReader.ReadEvents(
            stream,
            logEvent =>
            {
                message = Encoding.UTF8.GetString(
                    logEvent.Message);
            });

        Assert.Equal(1, result.ParsedEvents);
        Assert.Equal("Помилка підключення", message);
    }

    [Fact]
    public void ReadEvents_SmallBuffer_HandlesLinesAcrossReadBoundaries()
    {
        const string content =
            """
            2026-09-05T20:00:00Z INFO PaymentService Payment completed successfully
            2026-09-05T20:00:01Z WARN Worker Queue processing delayed
            """;

        using var stream = CreateStream(content);

        var handledEvents = 0;

        var result = StructuredLogReader.ReadEvents(
            stream,
            _ => handledEvents++,
            bufferSize: 8);

        Assert.Equal(2, result.TotalLines);
        Assert.Equal(2, result.ParsedEvents);
        Assert.Equal(0, result.MalformedLines);
        Assert.Equal(2, handledEvents);
    }

    [Fact]
    public void ReadEvents_HandlerThrows_PropagatesException()
    {
        const string content =
            "2026-09-05T20:00:00Z INFO Worker message";

        using var stream = CreateStream(content);

        Assert.Throws<InvalidOperationException>(
            () => StructuredLogReader.ReadEvents(
                stream,
                _ => throw new InvalidOperationException(
                    "Handler failed.")));
    }

    [Fact]
    public void ReadEvents_NullHandler_ThrowsArgumentNullException()
    {
        using var stream = CreateStream(
            "2026-09-05T20:00:00Z INFO Worker message");

        Assert.Throws<ArgumentNullException>(
            () => StructuredLogReader.ReadEvents(
                stream,
                null!));
    }

    private static MemoryStream CreateStream(string content)
    {
        return new MemoryStream(
            Encoding.UTF8.GetBytes(content));
    }
}
