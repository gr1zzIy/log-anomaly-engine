using LogAnomalyEngine.Core.Events;
using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Tests.Parsing;

public sealed class StructuredLogParserTests
{
    [Fact]
    public void TryParse_ValidLine_ParsesExpectedFields()
    {
        var line =
            "2026-09-04T20:00:00Z INFO PaymentService Payment completed"u8;

        var result =
            StructuredLogParser.TryParse(line, out var logEvent);

        Assert.True(result);

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

        Assert.True(
            logEvent.RawLine.SequenceEqual(line));
    }

    [Fact]
    public void TryParse_MultipleSpacesAndTabs_ParsesFields()
    {
        var line =
            "2026-09-04T20:00:00Z\t  WARN\tWorker   Queue delayed"u8;

        var result =
            StructuredLogParser.TryParse(line, out var logEvent);

        Assert.True(result);

        Assert.Equal(
            LogLevel.Warning,
            logEvent.Level);

        Assert.True(
            logEvent.Source.SequenceEqual(
                "Worker"u8));

        Assert.True(
            logEvent.Message.SequenceEqual(
                "Queue delayed"u8));
    }

    [Theory]
    [InlineData("TRACE", LogLevel.Trace)]
    [InlineData("trace", LogLevel.Trace)]
    [InlineData("DEBUG", LogLevel.Debug)]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("INFO", LogLevel.Information)]
    [InlineData("information", LogLevel.Information)]
    [InlineData("WARN", LogLevel.Warning)]
    [InlineData("warning", LogLevel.Warning)]
    [InlineData("ERROR", LogLevel.Error)]
    [InlineData("error", LogLevel.Error)]
    [InlineData("FATAL", LogLevel.Critical)]
    [InlineData("critical", LogLevel.Critical)]
    public void TryParse_KnownLevel_MapsExpectedLevel(
        string levelText,
        LogLevel expectedLevel)
    {
        var line = System.Text.Encoding.UTF8.GetBytes(
            $"2026-09-04T20:00:00Z {levelText} Worker message");

        var result =
            StructuredLogParser.TryParse(
                line,
                out var logEvent);

        Assert.True(result);
        Assert.Equal(expectedLevel, logEvent.Level);
    }

    [Fact]
    public void TryParse_UnknownLevel_MapsToUnknown()
    {
        var line =
            "2026-09-04T20:00:00Z NOTICE Worker message"u8;

        var result =
            StructuredLogParser.TryParse(
                line,
                out var logEvent);

        Assert.True(result);

        Assert.Equal(
            LogLevel.Unknown,
            logEvent.Level);
    }

    [Fact]
    public void TryParse_EmptyMessage_IsValid()
    {
        var line =
            "2026-09-04T20:00:00Z INFO Worker"u8;

        var result =
            StructuredLogParser.TryParse(
                line,
                out var logEvent);

        Assert.True(result);
        Assert.True(logEvent.Message.IsEmpty);
    }

    [Fact]
    public void TryParse_Utf8Message_PreservesBytes()
    {
        var line =
            "2026-09-04T20:00:00Z ERROR Worker Помилка підключення"u8;

        var result =
            StructuredLogParser.TryParse(
                line,
                out var logEvent);

        Assert.True(result);

        Assert.True(
            logEvent.Message.SequenceEqual(
                "Помилка підключення"u8));
    }

    [Theory]
    [InlineData("")]
    [InlineData("timestamp")]
    [InlineData("timestamp INFO")]
    public void TryParse_IncompleteLine_ReturnsFalse(
        string text)
    {
        var line =
            System.Text.Encoding.UTF8.GetBytes(text);

        var result =
            StructuredLogParser.TryParse(
                line,
                out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WhitespaceOnly_ReturnsFalse()
    {
        var line = "   \t   "u8;

        var result =
            StructuredLogParser.TryParse(
                line,
                out _);

        Assert.False(result);
    }
}
