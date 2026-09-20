using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Tests.Detection;

public sealed class ConceptDriftTests
{
    [Fact]
    public void SlidingWindow_AdaptsToNewNormalFasterThanCumulativeDetector()
    {
        const int WindowSize = 10;

        var cumulative =
            new FrequencyAnomalyDetector();

        var sliding =
            new SlidingWindowFrequencyDetector(
                WindowSize);

        // Historical regime:
        // Pattern A is normal.
        for (var i = 0; i < 100; i++)
        {
            ObserveBoth(
                cumulative,
                sliding,
                "Pattern A 123"u8);
        }

        // Concept drift:
        // Pattern B becomes the new normal.
        for (var i = 0; i < WindowSize; i++)
        {
            ObserveBoth(
                cumulative,
                sliding,
                "Pattern B 456"u8);
        }

        var result = ObserveBoth(
            cumulative,
            sliding,
            "Pattern B 789"u8);

        // Sliding window now contains only Pattern B,
        // so the new regime is fully adapted.
        Assert.Equal(
            0.0,
            result.Sliding.RarityScore);

        // Cumulative detector still carries the old
        // Pattern A history:
        //
        // B count = 10
        // historical total = 110
        // rarity = 1 - 10 / 110
        var expectedCumulativeRarity =
            1.0 - (10.0 / 110.0);

        Assert.Equal(
            expectedCumulativeRarity,
            result.Cumulative.RarityScore,
            precision: 12);

        Assert.True(
            result.Cumulative.RarityScore >
            result.Sliding.RarityScore);
    }

    private static (
        FrequencyAnomalyResult Cumulative,
        SlidingWindowAnomalyResult Sliding)
        ObserveBoth(
            FrequencyAnomalyDetector cumulative,
            SlidingWindowFrequencyDetector sliding,
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

        return (
            cumulative.Observe(logEvent),
            sliding.Observe(logEvent));
    }
}
