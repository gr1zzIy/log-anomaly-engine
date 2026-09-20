namespace LogAnomalyEngine.Core.Detection;

public readonly record struct SlidingWindowAnomalyResult(
    ulong Fingerprint,
    int PreviousCount,
    int CurrentCount,
    int HistoricalWindowCount,
    int WindowCount,
    long TotalObservedCount,
    bool IsFirstSeen,
    double HistoricalFrequency,
    double RarityScore);
