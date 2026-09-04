using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Tests.Parsing;

public sealed class RuntimeLineScannerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(1024)]
    public void FindNextLineFeed_VariousBuffersAndOffsets_MatchesScalar(
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
            var expected = ScalarLineScanner.FindNextLineFeed(
                buffer,
                startIndex);

            var actual = RuntimeLineScanner.FindNextLineFeed(
                buffer,
                startIndex);

            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(97)]
    public void FindNextLineFeed_InvalidStartIndex_ThrowsArgumentOutOfRangeException(
        int startIndex)
    {
        var buffer = new byte[96];

        Assert.Throws<ArgumentOutOfRangeException>(
            () => RuntimeLineScanner.FindNextLineFeed(
                buffer,
                startIndex));
    }
}
