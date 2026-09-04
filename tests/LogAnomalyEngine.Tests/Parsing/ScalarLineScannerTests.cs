using System.Text;
using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Tests.Parsing;

public sealed class ScalarLineScannerTests
{
    [Fact]
    public void FindNextLineFeed_EmptyBuffer_ReturnsMinusOne()
    {
        ReadOnlySpan<byte> buffer = [];

        var result = ScalarLineScanner.FindNextLineFeed(buffer);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void FindNextLineFeed_BufferWithoutLineFeed_ReturnsMinusOne()
    {
        var buffer = "INFO application started"u8;

        var result = ScalarLineScanner.FindNextLineFeed(buffer);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void FindNextLineFeed_LineFeedAtBeginning_ReturnsZero()
    {
        var buffer = "\nINFO application started"u8;

        var result = ScalarLineScanner.FindNextLineFeed(buffer);

        Assert.Equal(0, result);
    }

    [Fact]
    public void FindNextLineFeed_LineFeedAtEnd_ReturnsLastIndex()
    {
        var buffer = "INFO application started\n"u8;

        var result = ScalarLineScanner.FindNextLineFeed(buffer);

        Assert.Equal(buffer.Length - 1, result);
    }

    [Fact]
    public void FindNextLineFeed_MultipleLines_ReturnsFirstLineFeed()
    {
        var buffer = "INFO first\nWARN second\nERROR third"u8;

        var result = ScalarLineScanner.FindNextLineFeed(buffer);

        Assert.Equal(10, result);
    }

    [Fact]
    public void FindNextLineFeed_StartIndexProvided_ReturnsNextLineFeed()
    {
        var buffer = "INFO first\nWARN second\nERROR third"u8;
        var firstLineFeed = ScalarLineScanner.FindNextLineFeed(buffer);

        var result = ScalarLineScanner.FindNextLineFeed(
            buffer,
            firstLineFeed + 1);

        Assert.Equal(22, result);
    }

    [Fact]
    public void FindNextLineFeed_StartIndexEqualsBufferLength_ReturnsMinusOne()
    {
        var buffer = "INFO"u8;

        var result = ScalarLineScanner.FindNextLineFeed(
            buffer,
            buffer.Length);

        Assert.Equal(-1, result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void FindNextLineFeed_InvalidStartIndex_ThrowsArgumentOutOfRangeException(
        int startIndex)
    {
        byte[] buffer = "INFO"u8.ToArray();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ScalarLineScanner.FindNextLineFeed(buffer, startIndex));
    }

    [Fact]
    public void FindNextLineFeed_MultibyteUtf8Content_FindsLineFeed()
    {
        var buffer = Encoding.UTF8.GetBytes("INFO Користувач увійшов\nERROR Помилка");

        var result = ScalarLineScanner.FindNextLineFeed(buffer);

        Assert.Equal(
            Array.IndexOf(buffer, (byte)'\n'),
            result);
    }
}
