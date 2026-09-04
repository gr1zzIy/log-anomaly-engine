using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Tests.Parsing;

public sealed class Vector128LineScannerTests
{
    [Fact]
    public void FindNextLineFeed_EmptyBuffer_ReturnsMinusOne()
    {
        ReadOnlySpan<byte> buffer = [];

        var result = Vector128LineScanner.FindNextLineFeed(buffer);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void FindNextLineFeed_BufferWithoutLineFeed_ReturnsMinusOne()
    {
        var buffer =
            "INFO application started without delimiter"u8;

        var result = Vector128LineScanner.FindNextLineFeed(buffer);

        Assert.Equal(-1, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    public void FindNextLineFeed_LineFeedAtPosition_ReturnsExpectedIndex(
        int lineFeedIndex)
    {
        var buffer = new byte[96];

        Array.Fill(buffer, (byte)'A');
        buffer[lineFeedIndex] = (byte)'\n';

        var result =
            Vector128LineScanner.FindNextLineFeed(buffer);

        Assert.Equal(lineFeedIndex, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    public void FindNextLineFeed_StartOffset_ReturnsNextLineFeed(
        int startIndex)
    {
        var buffer = new byte[64];

        Array.Fill(buffer, (byte)'A');

        buffer[8] = (byte)'\n';
        buffer[40] = (byte)'\n';

        var expected =
            ScalarLineScanner.FindNextLineFeed(
                buffer,
                startIndex);

        var actual =
            Vector128LineScanner.FindNextLineFeed(
                buffer,
                startIndex);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FindNextLineFeed_MultipleMatches_ReturnsFirstMatch()
    {
        var buffer = new byte[64];

        Array.Fill(buffer, (byte)'A');

        buffer[19] = (byte)'\n';
        buffer[20] = (byte)'\n';
        buffer[50] = (byte)'\n';

        var result =
            Vector128LineScanner.FindNextLineFeed(buffer);

        Assert.Equal(19, result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(15)]
    public void FindNextLineFeed_BufferSmallerThanVector_MatchesScalar(
        int length)
    {
        var buffer = new byte[length];

        Array.Fill(buffer, (byte)'A');

        if (length > 1)
        {
            buffer[length - 1] = (byte)'\n';
        }

        var expected =
            ScalarLineScanner.FindNextLineFeed(buffer);

        var actual =
            Vector128LineScanner.FindNextLineFeed(buffer);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(97)]
    public void FindNextLineFeed_InvalidStartIndex_ThrowsArgumentOutOfRangeException(
        int startIndex)
    {
        var buffer = new byte[96];

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Vector128LineScanner.FindNextLineFeed(
                buffer,
                startIndex));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(1024)]
    public void FindNextLineFeed_VariousBuffers_MatchesScalar(
        int length)
    {
        var buffer = new byte[length];

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)('A' + (i % 26));
        }

        if (length > 5)
        {
            buffer[length * 3 / 4] = (byte)'\n';
        }

        for (var startIndex = 0; startIndex <= buffer.Length; startIndex++)
        {
            var expected =
                ScalarLineScanner.FindNextLineFeed(
                    buffer,
                    startIndex);

            var actual =
                Vector128LineScanner.FindNextLineFeed(
                    buffer,
                    startIndex);

            Assert.Equal(expected, actual);
        }
    }
}
