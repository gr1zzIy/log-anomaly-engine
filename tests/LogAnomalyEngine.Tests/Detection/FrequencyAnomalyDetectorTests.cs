using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Tests.Detection;

public sealed class FrequencyAnomalyDetectorTests
{
    [Fact]
    public void Observe_FirstEvent_IsFirstSeenAndMaximallyRare()
    {
        var detector =
            new FrequencyAnomalyDetector();

        var result = Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Request 123 completed"u8);

        Assert.True(result.IsFirstSeen);
        Assert.Equal(0, result.PreviousCount);
        Assert.Equal(1, result.CurrentCount);
        Assert.Equal(1, result.TotalCount);

        Assert.Equal(
            0.0,
            result.HistoricalFrequency);

        Assert.Equal(
            1.0,
            result.RarityScore);
    }

    [Fact]
    public void Observe_RepeatedEvent_UpdatesExistingState()
    {
        var detector =
            new FrequencyAnomalyDetector();

        Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Request 123 completed"u8);

        var second = Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Request 123 completed"u8);

        Assert.False(second.IsFirstSeen);
        Assert.Equal(1, second.PreviousCount);
        Assert.Equal(2, second.CurrentCount);
        Assert.Equal(2, second.TotalCount);

        Assert.Equal(
            1.0,
            second.HistoricalFrequency);

        Assert.Equal(
            0.0,
            second.RarityScore);
    }

    [Fact]
    public void Observe_NumericVariants_UpdateSameFingerprint()
    {
        var detector =
            new FrequencyAnomalyDetector();

        var first = Observe(
            detector,
            LogLevel.Information,
            "AuthService"u8,
            "User 123 logged in from node 7"u8);

        var second = Observe(
            detector,
            LogLevel.Information,
            "AuthService"u8,
            "User 999 logged in from node 42"u8);

        Assert.Equal(
            first.Fingerprint,
            second.Fingerprint);

        Assert.False(second.IsFirstSeen);
        Assert.Equal(2, second.CurrentCount);
        Assert.Equal(1, detector.DistinctFingerprintCount);
    }

    [Fact]
    public void Observe_DifferentStructures_HaveIndependentState()
    {
        var detector =
            new FrequencyAnomalyDetector();

        var completed = Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Request 123 completed"u8);

        var failed = Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Request 123 failed"u8);

        Assert.NotEqual(
            completed.Fingerprint,
            failed.Fingerprint);

        Assert.True(failed.IsFirstSeen);
        Assert.Equal(1, failed.CurrentCount);

        Assert.Equal(
            2,
            detector.DistinctFingerprintCount);
    }

    [Fact]
    public void Observe_MixedStream_ComputesHistoricalFrequency()
    {
        var detector =
            new FrequencyAnomalyDetector();

        Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Type A 1"u8);

        Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Type A 2"u8);

        Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Type B 1"u8);

        var result = Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Type B 2"u8);

        Assert.Equal(1, result.PreviousCount);
        Assert.Equal(2, result.CurrentCount);
        Assert.Equal(4, result.TotalCount);

        Assert.Equal(
            1.0 / 3.0,
            result.HistoricalFrequency,
            precision: 12);

        Assert.Equal(
            2.0 / 3.0,
            result.RarityScore,
            precision: 12);
    }

    [Fact]
    public void Observe_RarityScore_RemainsWithinExpectedRange()
    {
        var detector =
            new FrequencyAnomalyDetector();

        for (var i = 0; i < 100; i++)
        {
            var result = Observe(
                detector,
                LogLevel.Information,
                "Worker"u8,
                "Repeated event 123"u8);

            Assert.InRange(
                result.RarityScore,
                0.0,
                1.0);

            Assert.InRange(
                result.HistoricalFrequency,
                0.0,
                1.0);
        }
    }

    [Fact]
    public void Observe_TracksTotalAndDistinctCounts()
    {
        var detector =
            new FrequencyAnomalyDetector();

        Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Type A 1"u8);

        Observe(
            detector,
            LogLevel.Information,
            "Worker"u8,
            "Type A 2"u8);

        Observe(
            detector,
            LogLevel.Warning,
            "Worker"u8,
            "Type B 1"u8);

        Assert.Equal(3, detector.TotalCount);
        Assert.Equal(2, detector.DistinctFingerprintCount);
    }

    [Fact]
    public void Reset_ClearsDetectorState()
    {
        var detector =
            new FrequencyAnomalyDetector();

        Observe(
            detector,
            LogLevel.Error,
            "Worker"u8,
            "Request 123 failed"u8);

        detector.Reset();

        Assert.Equal(0, detector.TotalCount);
        Assert.Equal(
            0,
            detector.DistinctFingerprintCount);

        var result = Observe(
            detector,
            LogLevel.Error,
            "Worker"u8,
            "Request 456 failed"u8);

        Assert.True(result.IsFirstSeen);
        Assert.Equal(1.0, result.RarityScore);
    }

    private static FrequencyAnomalyResult Observe(
        FrequencyAnomalyDetector detector,
        LogLevel level,
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> message)
    {
        const int TimestampLength = 20;

        var sourceOffset = TimestampLength;
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
            timestampRange: 0..TimestampLength,
            level,
            sourceRange:
                sourceOffset..messageOffset,
            messageRange:
                messageOffset..rawLine.Length);

        return detector.Observe(logEvent);
    }
}
