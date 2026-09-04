using System.Text;
using LogAnomalyEngine.Core.Reading;

namespace LogAnomalyEngine.Tests.Reading;

public sealed class StreamingLogReaderTests
{
    [Fact]
    public void ReadLines_EmptyStream_ProducesNoLines()
    {
        using var stream = CreateStream(string.Empty);
        var lines = new List<string>();

        var count = StreamingLogReader.ReadLines(
            stream,
            line => lines.Add(Encoding.UTF8.GetString(line)));

        Assert.Equal(0, count);
        Assert.Empty(lines);
    }

    [Fact]
    public void ReadLines_SingleTerminatedLine_ReturnsLine()
    {
        using var stream = CreateStream("INFO started\n");
        var lines = ReadAllLines(stream);

        Assert.Equal(["INFO started"], lines);
    }

    [Fact]
    public void ReadLines_SingleUnterminatedLine_ReturnsLine()
    {
        using var stream = CreateStream("INFO started");
        var lines = ReadAllLines(stream);

        Assert.Equal(["INFO started"], lines);
    }

    [Fact]
    public void ReadLines_MultipleLines_ReturnsAllLines()
    {
        using var stream = CreateStream(
            "INFO started\nWARN retry\nERROR failed\n");

        var lines = ReadAllLines(stream);

        Assert.Equal(
            [
                "INFO started",
                "WARN retry",
                "ERROR failed"
            ],
            lines);
    }

    [Fact]
    public void ReadLines_EmptyLines_PreservesEmptyLines()
    {
        using var stream = CreateStream("INFO\n\nERROR\n");
        var lines = ReadAllLines(stream);

        Assert.Equal(
            [
                "INFO",
                string.Empty,
                "ERROR"
            ],
            lines);
    }

    [Fact]
    public void ReadLines_CrlfLineEndings_RemovesCarriageReturn()
    {
        using var stream = CreateStream(
            "INFO started\r\nWARN retry\r\n");

        var lines = ReadAllLines(stream);

        Assert.Equal(
            [
                "INFO started",
                "WARN retry"
            ],
            lines);
    }

    [Fact]
    public void ReadLines_LineSplitAcrossChunks_ReconstructsLine()
    {
        using var stream = CreateStream(
            "INFO first\nWARN second\nERROR third");

        var lines = ReadAllLines(
            stream,
            bufferSize: 8);

        Assert.Equal(
            [
                "INFO first",
                "WARN second",
                "ERROR third"
            ],
            lines);
    }

    [Fact]
    public void ReadLines_LineLargerThanInitialBuffer_GrowsBuffer()
    {
        var longMessage = new string('A', 128);

        using var stream = CreateStream(
            $"{longMessage}\nINFO next");

        var lines = ReadAllLines(
            stream,
            bufferSize: 8);

        Assert.Equal(
            [
                longMessage,
                "INFO next"
            ],
            lines);
    }

    [Fact]
    public void ReadLines_MultibyteUtf8AcrossChunks_ReconstructsContent()
    {
        using var stream = CreateStream(
            "INFO Користувач увійшов\nERROR Помилка");

        var lines = ReadAllLines(
            stream,
            bufferSize: 7);

        Assert.Equal(
            [
                "INFO Користувач увійшов",
                "ERROR Помилка"
            ],
            lines);
    }

    [Fact]
    public void ReadLines_ReturnsProcessedLineCount()
    {
        using var stream = CreateStream(
            "INFO first\nWARN second\nERROR third");

        var lines = new List<string>();

        var count = StreamingLogReader.ReadLines(
            stream,
            line => lines.Add(Encoding.UTF8.GetString(line)),
            bufferSize: 8);

        Assert.Equal(3, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReadLines_NonPositiveBufferSize_ThrowsArgumentOutOfRangeException(
        int bufferSize)
    {
        using var stream = CreateStream("INFO");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => StreamingLogReader.ReadLines(
                stream,
                _ => { },
                bufferSize));
    }

    [Fact]
    public void ReadLines_HandlerThrows_PropagatesException()
    {
        using var stream = CreateStream("INFO first\nWARN second\n");

        var exception = Assert.Throws<InvalidOperationException>(
            () => StreamingLogReader.ReadLines(
                stream,
                _ => throw new InvalidOperationException("Test failure")));

        Assert.Equal("Test failure", exception.Message);
    }

    private static List<string> ReadAllLines(
        Stream stream,
        int bufferSize = StreamingLogReader.DefaultBufferSize)
    {
        var lines = new List<string>();

        StreamingLogReader.ReadLines(
            stream,
            line => lines.Add(Encoding.UTF8.GetString(line)),
            bufferSize);

        return lines;
    }


    private static MemoryStream CreateStream(string content)
    {
        return new MemoryStream(
            Encoding.UTF8.GetBytes(content),
            writable: false);
    }
}
