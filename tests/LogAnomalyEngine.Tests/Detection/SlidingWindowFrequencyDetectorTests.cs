using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Tests.Detection;

public sealed class SlidingWindowFrequencyDetectorTests
{
    [Fact]
    public void Constructor_ZeroWindowSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SlidingWindowFrequencyDetector(0));
    }

    [Fact]
    public void Constructor_NegativeWindowSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SlidingWindowFrequencyDetector(-1));
    }

    [Fact]
    public void Observe_FirstEvent_IsFirstSeenAndMaximallyRare()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 3);

        var result = Observe(
            detector,
            "Type A 1"u8);

        Assert.True(result.IsFirstSeen);

        Assert.Equal(
            0,
            result.PreviousCount);

        Assert.Equal(
            1,
            result.CurrentCount);

        Assert.Equal(
            0,
            result.HistoricalWindowCount);

        Assert.Equal(
            1,
            result.WindowCount);

        Assert.Equal(
            1,
            result.TotalObservedCount);

        Assert.Equal(
            0.0,
            result.HistoricalFrequency);

        Assert.Equal(
            1.0,
            result.RarityScore);
    }

    [Fact]
    public void Observe_RepeatedEvent_UsesPreviousWindowState()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 3);

        Observe(
            detector,
            "Type A 1"u8);

        var result = Observe(
            detector,
            "Type A 2"u8);

        Assert.False(result.IsFirstSeen);

        Assert.Equal(
            1,
            result.PreviousCount);

        Assert.Equal(
            2,
            result.CurrentCount);

        Assert.Equal(
            1,
            result.HistoricalWindowCount);

        Assert.Equal(
            1.0,
            result.HistoricalFrequency);

        Assert.Equal(
            0.0,
            result.RarityScore);
    }

    [Fact]
    public void Observe_WindowNeverExceedsConfiguredSize()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 3);

        for (var i = 0; i < 10; i++)
        {
            Observe(
                detector,
                "Repeated event 123"u8);

            Assert.InRange(
                detector.WindowCount,
                0,
                3);
        }

        Assert.Equal(
            3,
            detector.WindowCount);

        Assert.Equal(
            10,
            detector.TotalObservedCount);
    }

    [Fact]
    public void Observe_ExpiredFingerprint_IsRemovedFromState()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 2);

        Observe(
            detector,
            "Type A 1"u8);

        Observe(
            detector,
            "Type B 1"u8);

        Assert.Equal(
            2,
            detector.DistinctFingerprintCount);

        Observe(
            detector,
            "Type C 1"u8);

        Assert.Equal(
            2,
            detector.DistinctFingerprintCount);
    }

    [Fact]
    public void Observe_ExpiredEvent_IsFirstSeenAgain()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 2);

        Observe(
            detector,
            "Type A 1"u8);

        Observe(
            detector,
            "Type B 1"u8);

        Observe(
            detector,
            "Type C 1"u8);

        var result = Observe(
            detector,
            "Type A 999"u8);

        Assert.True(result.IsFirstSeen);

        Assert.Equal(
            0,
            result.PreviousCount);

        Assert.Equal(
            1.0,
            result.RarityScore);
    }

    [Fact]
    public void Observe_SameFingerprintAsExpiredOldest_MaintainsCorrectCount()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 3);

        Observe(
            detector,
            "Type A 1"u8);

        Observe(
            detector,
            "Type B 1"u8);

        Observe(
            detector,
            "Type A 2"u8);

        var result = Observe(
            detector,
            "Type A 999"u8);

        Assert.Equal(
            2,
            result.PreviousCount);

        Assert.Equal(
            2,
            result.CurrentCount);

        Assert.Equal(
            3,
            result.WindowCount);

        Assert.Equal(
            2.0 / 3.0,
            result.HistoricalFrequency,
            precision: 12);

        Assert.Equal(
            1.0 / 3.0,
            result.RarityScore,
            precision: 12);
    }

    [Fact]
    public void Observe_AdaptsWhenPreviouslyRarePatternBecomesNormal()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 3);

        Observe(
            detector,
            "Type A 1"u8);

        Observe(
            detector,
            "Type A 2"u8);

        Observe(
            detector,
            "Type A 3"u8);

        var firstB = Observe(
            detector,
            "Type B 1"u8);

        var secondB = Observe(
            detector,
            "Type B 2"u8);

        var thirdB = Observe(
            detector,
            "Type B 3"u8);

        var fourthB = Observe(
            detector,
            "Type B 4"u8);

        Assert.Equal(
            1.0,
            firstB.RarityScore);

        Assert.Equal(
            2.0 / 3.0,
            secondB.RarityScore,
            precision: 12);

        Assert.Equal(
            1.0 / 3.0,
            thirdB.RarityScore,
            precision: 12);

        Assert.Equal(
            0.0,
            fourthB.RarityScore);
    }

    [Fact]
    public void Observe_NumericVariantsShareSameWindowState()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 4);

        var first = Observe(
            detector,
            "Request 123 completed"u8);

        var second = Observe(
            detector,
            "Request 999 completed"u8);

        Assert.Equal(
            first.Fingerprint,
            second.Fingerprint);

        Assert.False(
            second.IsFirstSeen);

        Assert.Equal(
            2,
            second.CurrentCount);
    }

    [Fact]
    public void Observe_WindowSizeOne_UsesOnlyMostRecentEvent()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 1);

        Observe(
            detector,
            "Type A 1"u8);

        var firstB = Observe(
            detector,
            "Type B 1"u8);

        var secondB = Observe(
            detector,
            "Type B 2"u8);

        Assert.Equal(
            1.0,
            firstB.RarityScore);

        Assert.Equal(
            0.0,
            secondB.RarityScore);

        Assert.Equal(
            1,
            detector.WindowCount);
    }

    [Fact]
    public void Reset_ClearsWindowAndStatistics()
    {
        var detector =
            new SlidingWindowFrequencyDetector(
                windowSize: 3);

        Observe(
            detector,
            "Type A 1"u8);

        Observe(
            detector,
            "Type B 1"u8);

        detector.Reset();

        Assert.Equal(
            0,
            detector.WindowCount);

        Assert.Equal(
            0,
            detector.DistinctFingerprintCount);

        Assert.Equal(
            0,
            detector.TotalObservedCount);

        var result = Observe(
            detector,
            "Type A 999"u8);

        Assert.True(
            result.IsFirstSeen);

        Assert.Equal(
            1.0,
            result.RarityScore);
    }

    private static SlidingWindowAnomalyResult Observe(
        SlidingWindowFrequencyDetector detector,
        ReadOnlySpan<byte> message)
    {
        const int TimestampLength = 20;

        ReadOnlySpan<byte> source =
            "Worker"u8;

        var sourceOffset =
            TimestampLength;

        var messageOffset =
            sourceOffset + source.Length;

        var rawLine = new byte[
            TimestampLength +
            source.Length +
            message.Length];

        "2026-09-20T12:00:00Z"u8.CopyTo(
            rawLine);

        source.CopyTo(
            rawLine.AsSpan(sourceOffset));

        message.CopyTo(
            rawLine.AsSpan(messageOffset));

        var logEvent = new LogEventView(
            rawLine,
            timestampRange:
                0..TimestampLength,
            level:
                LogLevel.Information,
            sourceRange:
                sourceOffset..messageOffset,
            messageRange:
                messageOffset..rawLine.Length);

        return detector.Observe(logEvent);
    }
}
