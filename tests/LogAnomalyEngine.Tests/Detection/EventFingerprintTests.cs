using LogAnomalyEngine.Core.Detection;
using LogAnomalyEngine.Core.Events;

namespace LogAnomalyEngine.Tests.Detection;

public sealed class EventFingerprintTests
{
    [Fact]
    public void Compute_SameEvent_ReturnsSameFingerprint()
    {
        var first = ComputeFingerprint(
            LogLevel.Information,
            "PaymentService"u8,
            "Payment completed"u8);

        var second = ComputeFingerprint(
            LogLevel.Information,
            "PaymentService"u8,
            "Payment completed"u8);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_DifferentNumericValues_ReturnsSameFingerprint()
    {
        var first = ComputeFingerprint(
            LogLevel.Information,
            "AuthService"u8,
            "User 123 logged in from node 7"u8);

        var second = ComputeFingerprint(
            LogLevel.Information,
            "AuthService"u8,
            "User 456789 logged in from node 12"u8);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_DifferentMessageStructure_ReturnsDifferentFingerprint()
    {
        var first = ComputeFingerprint(
            LogLevel.Information,
            "AuthService"u8,
            "User 123 logged in"u8);

        var second = ComputeFingerprint(
            LogLevel.Information,
            "AuthService"u8,
            "User 123 authentication failed"u8);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Compute_DifferentSources_ReturnDifferentFingerprints()
    {
        var first = ComputeFingerprint(
            LogLevel.Error,
            "WorkerA"u8,
            "Queue 123 failed"u8);

        var second = ComputeFingerprint(
            LogLevel.Error,
            "WorkerB"u8,
            "Queue 123 failed"u8);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Compute_DifferentLevels_ReturnDifferentFingerprints()
    {
        var information = ComputeFingerprint(
            LogLevel.Information,
            "Worker"u8,
            "Request 123 completed"u8);

        var error = ComputeFingerprint(
            LogLevel.Error,
            "Worker"u8,
            "Request 123 completed"u8);

        Assert.NotEqual(information, error);
    }

    [Fact]
    public void Compute_MultipleNumericRuns_NormalizesEachRun()
    {
        var first = ComputeFingerprint(
            LogLevel.Warning,
            "Gateway"u8,
            "Request 123 failed after 45 attempts on node 7"u8);

        var second = ComputeFingerprint(
            LogLevel.Warning,
            "Gateway"u8,
            "Request 9 failed after 1000 attempts on node 42"u8);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_NumberStructureStillAffectsFingerprint()
    {
        var singleRun = ComputeFingerprint(
            LogLevel.Information,
            "Worker"u8,
            "Value 1234"u8);

        var separateRuns = ComputeFingerprint(
            LogLevel.Information,
            "Worker"u8,
            "Value 12 34"u8);

        Assert.NotEqual(singleRun, separateRuns);
    }

    [Fact]
    public void Compute_NumbersInsideWords_AreNormalized()
    {
        var first = ComputeFingerprint(
            LogLevel.Information,
            "Worker"u8,
            "worker123 processed item456"u8);

        var second = ComputeFingerprint(
            LogLevel.Information,
            "Worker"u8,
            "worker999 processed item7"u8);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_SourceNumbers_AreNotNormalized()
    {
        var first = ComputeFingerprint(
            LogLevel.Information,
            "Worker1"u8,
            "Processed request 123"u8);

        var second = ComputeFingerprint(
            LogLevel.Information,
            "Worker2"u8,
            "Processed request 456"u8);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Compute_Utf8Message_NormalizesNumbersWithoutChangingTextStructure()
    {
        var first = ComputeFingerprint(
            LogLevel.Error,
            "PaymentService"u8,
            "Помилка платежу 123 для користувача 456"u8);

        var second = ComputeFingerprint(
            LogLevel.Error,
            "PaymentService"u8,
            "Помилка платежу 999 для користувача 7"u8);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_KnownInput_ReturnsStableFingerprint()
    {
        var fingerprint = ComputeFingerprint(
            LogLevel.Error,
            "PaymentService"u8,
            "Payment 123 failed"u8);

        Assert.Equal(
            17024915595914404356UL,
            fingerprint);
    }

    private static ulong ComputeFingerprint(
        LogLevel level,
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> message)
    {
        const int TimestampLength = 20;

        var sourceOffset = TimestampLength;
        var messageOffset = sourceOffset + source.Length;

        var rawLine = new byte[
            TimestampLength +
            source.Length +
            message.Length];

        "2026-09-05T20:00:00Z"u8.CopyTo(rawLine);

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

        return EventFingerprint.Compute(logEvent);
    }
}
