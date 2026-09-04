namespace LogAnomalyEngine.Core.Parsing;

internal interface ILineScanner<TSelf>
    where TSelf : ILineScanner<TSelf>
{
    static abstract int FindNextLineFeed(
        ReadOnlySpan<byte> buffer,
        int startIndex = 0);
}
