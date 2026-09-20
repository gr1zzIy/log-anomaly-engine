namespace LogAnomalyEngine.Core.Detection;

public readonly record struct FrequencyAnomalyResult(
    ulong Fingerprint,
    long PreviousCount,
    long CurrentCount,
    long TotalCount,
    bool IsFirstSeen,
    double HistoricalFrequency,
    double RarityScore);
