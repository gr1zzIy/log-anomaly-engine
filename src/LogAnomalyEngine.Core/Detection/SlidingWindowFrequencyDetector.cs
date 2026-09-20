using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Core.Detection;

public sealed class SlidingWindowFrequencyDetector
{
    private readonly int _windowSize;
    private readonly Queue<ulong> _window;
    private readonly Dictionary<ulong, int> _counts = new();

    private long _totalObservedCount;

    public SlidingWindowFrequencyDetector(int windowSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowSize);

        _windowSize = windowSize;
        _window = new Queue<ulong>(windowSize);
    }

    public int WindowSize => _windowSize;

    public int WindowCount => _window.Count;

    public int DistinctFingerprintCount => _counts.Count;

    public long TotalObservedCount => _totalObservedCount;

    public SlidingWindowAnomalyResult Observe(
        LogEventView logEvent)
    {
        var fingerprint =
            EventFingerprint.Compute(logEvent);

        _counts.TryGetValue(
            fingerprint,
            out var previousCount);

        var historicalWindowCount =
            _window.Count;

        var historicalFrequency =
            historicalWindowCount == 0
                ? 0.0
                : (double)previousCount /
                  historicalWindowCount;

        var rarityScore =
            1.0 - historicalFrequency;

        //
        // Score is calculated against the complete historical
        // window before the current event modifies the model.
        //
        if (_window.Count == _windowSize)
        {
            RemoveOldest();
        }

        _window.Enqueue(fingerprint);

        _counts.TryGetValue(
            fingerprint,
            out var countBeforeInsert);

        var currentCount =
            checked(countBeforeInsert + 1);

        _counts[fingerprint] =
            currentCount;

        _totalObservedCount =
            checked(_totalObservedCount + 1);

        return new SlidingWindowAnomalyResult(
            Fingerprint: fingerprint,
            PreviousCount: previousCount,
            CurrentCount: currentCount,
            HistoricalWindowCount:
                historicalWindowCount,
            WindowCount: _window.Count,
            TotalObservedCount:
                _totalObservedCount,
            IsFirstSeen: previousCount == 0,
            HistoricalFrequency:
                historicalFrequency,
            RarityScore:
                rarityScore);
    }

    public void Reset()
    {
        _window.Clear();
        _counts.Clear();
        _totalObservedCount = 0;
    }

    private void RemoveOldest()
    {
        var expiredFingerprint =
            _window.Dequeue();

        var expiredCount =
            _counts[expiredFingerprint];

        if (expiredCount == 1)
        {
            _counts.Remove(
                expiredFingerprint);

            return;
        }

        _counts[expiredFingerprint] =
            expiredCount - 1;
    }
}
