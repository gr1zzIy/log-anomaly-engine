namespace LogAnomalyEngine.Core.Reading;

public readonly record struct StructuredReadResult(
    long TotalLines,
    long ParsedEvents,
    long MalformedLines);
