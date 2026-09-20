using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Core.Detection;

public sealed class FrequencyAnomalyDetector
{
    private readonly Dictionary<ulong, long> _counts = new();

    private long _totalCount;

    public long TotalCount => _totalCount;

    public int DistinctFingerprintCount => _counts.Count;

    public FrequencyAnomalyResult Observe(
        LogEventView logEvent)
    {
        var fingerprint =
            EventFingerprint.Compute(logEvent);

        _counts.TryGetValue(
            fingerprint,
            out var previousCount);

        var previousTotalCount = _totalCount;

        var historicalFrequency =
            previousTotalCount == 0
                ? 0.0
                : (double)previousCount /
                  previousTotalCount;

        var rarityScore =
            1.0 - historicalFrequency;

        var currentCount =
            checked(previousCount + 1);

        _totalCount =
            checked(_totalCount + 1);

        _counts[fingerprint] = currentCount;

        return new FrequencyAnomalyResult(
            Fingerprint: fingerprint,
            PreviousCount: previousCount,
            CurrentCount: currentCount,
            TotalCount: _totalCount,
            IsFirstSeen: previousCount == 0,
            HistoricalFrequency: historicalFrequency,
            RarityScore: rarityScore);
    }

    public void Reset()
    {
        _counts.Clear();
        _totalCount = 0;
    }
}
