namespace LogAnomalyEngine.Core.Events;

/// <summary>
/// Надає zero-copy представлення структурованої лог-події
/// в межах часу життя вихідного UTF-8 буфера.
/// </summary>
public readonly ref struct LogEventView
{
    private readonly ReadOnlySpan<byte> _rawLine;

    private readonly int _timestampOffset;
    private readonly int _timestampLength;

    private readonly int _sourceOffset;
    private readonly int _sourceLength;

    private readonly int _messageOffset;
    private readonly int _messageLength;

    public LogEventView(
        ReadOnlySpan<byte> rawLine,
        Range timestampRange,
        LogLevel level,
        Range sourceRange,
        Range messageRange)
    {
        _rawLine = rawLine;

        (_timestampOffset, _timestampLength) =
            timestampRange.GetOffsetAndLength(rawLine.Length);

        (_sourceOffset, _sourceLength) =
            sourceRange.GetOffsetAndLength(rawLine.Length);

        (_messageOffset, _messageLength) =
            messageRange.GetOffsetAndLength(rawLine.Length);

        Level = level;
    }

    public ReadOnlySpan<byte> RawLine => _rawLine;

    public ReadOnlySpan<byte> Timestamp =>
        _rawLine.Slice(_timestampOffset, _timestampLength);

    public LogLevel Level { get; }

    public ReadOnlySpan<byte> Source =>
        _rawLine.Slice(_sourceOffset, _sourceLength);

    public ReadOnlySpan<byte> Message =>
        _rawLine.Slice(_messageOffset, _messageLength);
}
