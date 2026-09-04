namespace LogAnomalyEngine.Core.Parsing;

public static class RuntimeLineScanner
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

        var matchIndex = buffer[startIndex..].IndexOf(LineFeed);

        if (matchIndex < 0)
        {
            return -1;
        }

        return startIndex + matchIndex;
    }
}
