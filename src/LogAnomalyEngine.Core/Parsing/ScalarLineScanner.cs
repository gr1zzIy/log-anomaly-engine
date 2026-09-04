namespace LogAnomalyEngine.Core.Parsing;

public static class ScalarLineScanner
{
    private const byte LineFeed = (byte)'\n';

    public static int FindNextLineFeed(
        ReadOnlySpan<byte> buffer,
        int startIndex = 0)
    {
        if (startIndex < 0 || startIndex > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        for (var i = startIndex; i < buffer.Length; i++)
        {
            if (buffer[i] == LineFeed)
            {
                return i;
            }
        }

        return -1;
    }
}
