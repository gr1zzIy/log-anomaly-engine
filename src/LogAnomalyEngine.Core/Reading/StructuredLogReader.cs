using LogAnomalyEngine.Core.Parsing;

namespace LogAnomalyEngine.Core.Reading;

public static class StructuredLogReader
{
    public static StructuredReadResult ReadEvents(
        Stream stream,
        StructuredLogEventCallback handler,
        int bufferSize = StreamingLogReader.DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(handler);

        long parsedEvents = 0;
        long malformedLines = 0;

        var totalLines = StreamingLogReader.ReadLines(
            stream,
            line =>
            {
                if (!StructuredLogParser.TryParse(
                        line,
                        out var logEvent))
                {
                    malformedLines++;
                    return;
                }

                handler(logEvent);
                parsedEvents++;
            },
            bufferSize);

        return new StructuredReadResult(
            TotalLines: totalLines,
            ParsedEvents: parsedEvents,
            MalformedLines: malformedLines);
    }
}
