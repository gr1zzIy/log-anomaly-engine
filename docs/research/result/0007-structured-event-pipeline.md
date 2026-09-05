# Research Result 0007 — Structured Event Streaming Pipeline

## Status

Validated M3 end-to-end structured-pipeline result.

## Date

2026-09-05

## Context

M3 introduced a zero-copy structured event representation and parser:

```text
StreamingLogReader
    |
    v
ReadOnlySpan<byte>
    |
    v
StructuredLogParser
    |
    v
LogEventView
    |
    v
StructuredLogEventHandler
```

The purpose of this experiment was to quantify the additional cost of converting framed UTF-8 log lines into structured events.

Two pipelines were compared over the same input data:

```text
FramingOnly:
Stream -> StreamingLogReader -> line handler

Structured:
Stream -> StreamingLogReader -> StructuredLogParser
       -> LogEventView -> structured handler
```

The benchmark keeps the total dataset size constant and varies only the log-line length.

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

## Benchmark configuration

Dataset size:

```text
16,000,000 bytes
```

Reader buffer:

```text
64 KiB
```

Line lengths:

```text
125 B
1,000 B
16,000 B
100,000 B
```

This produces approximately:

| Line length | Events in dataset |
| ---: | ---: |
| 125 B | 128,000 |
| 1,000 B | 16,000 |
| 16,000 B | 1,000 |
| 100,000 B | 160 |

All records are valid structured records.

## Results

| Line length | Framing only | Structured | Ratio | Additional time | Structured allocation |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 125 B | 1,324.0 us | 2,850.3 us | 2.15x | 1,526.3 us | 104 B |
| 1,000 B | 578.6 us | 761.1 us | 1.32x | 182.5 us | 104 B |
| 16,000 B | 497.2 us | 495.4 us | ~1.00x | within measurement noise | 104 B |
| 100,000 B | 533.2 us | 540.5 us | 1.01x | 7.3 us | 104 B |

Approximate in-memory throughput:

| Line length | Framing only | Structured |
| ---: | ---: | ---: |
| 125 B | 11.25 GiB/s | 5.23 GiB/s |
| 1,000 B | 25.76 GiB/s | 19.58 GiB/s |
| 16,000 B | 29.96 GiB/s | 30.07 GiB/s |
| 100,000 B | 27.95 GiB/s | 27.58 GiB/s |

These are memory-resident synthetic pipeline measurements. They are not storage-throughput claims.

## Key observations

### Structured parsing cost is event-count sensitive

The total dataset size is fixed at 16 MB, but the number of events varies substantially.

For 125-byte records, the structured pipeline processes approximately 128,000 events and is about 2.15x slower than framing alone.

For 1,000-byte records, it processes approximately 16,000 events and the ratio falls to 1.32x.

For 16,000-byte and 100,000-byte records, the structured-processing overhead becomes negligible relative to the cost of reading and framing the bytes.

This is consistent with the parser design: structural parsing has a mostly per-event cost rather than a cost proportional to message-body length.

### Short-record overhead is approximately constant per event

For the two high-cardinality cases:

```text
125 B:
1,526.3 us / 128,000 events ≈ 11.9 ns/event

1,000 B:
182.5 us / 16,000 events ≈ 11.4 ns/event
```

The similarity supports the interpretation that the structured parser introduces an approximately fixed per-event cost for these workloads.

The larger-line cases contain too few events for a reliable per-event estimate because the difference approaches benchmark noise.

### The 104-byte allocation is not per-event

`Structured` reports exactly 104 B for every tested line length:

```text
128,000 events -> 104 B
16,000 events  -> 104 B
1,000 events   -> 104 B
160 events     -> 104 B
```

Therefore, the benchmark provides no evidence of a per-event managed allocation.

The fixed cost is consistent with invocation-level adapter/closure state in `StructuredLogReader`, although this experiment alone does not prove the exact source.

The important property is that allocation does not scale with event count.

### Large messages remain cheap to structure

The 16,000-byte case is statistically indistinguishable from framing alone, and the 100,000-byte case is only about 1% slower.

This is expected because `StructuredLogParser` does not scan or decode the complete message payload. It identifies the structural prefix and exposes the remainder as a span.

## Decision

Keep the current `StructuredLogReader` composition.

Do not introduce a more complex callback-state API solely to eliminate the fixed 104-byte allocation.

The measured allocation is:

- constant per `ReadEvents` invocation;
- not proportional to the number of events;
- negligible compared with per-event allocation strategies.

No optimization is justified without evidence that this invocation-level allocation becomes material in a real workload.

## M3 architectural result

The structured-event pipeline now provides:

```text
Stream
    |
    v
optimized UTF-8 framing
    |
    v
allocation-free structural parser
    |
    v
ephemeral LogEventView
    |
    v
synchronous consumer
```

The hot path avoids per-event string and byte-array allocations.

For high-cardinality workloads, structured parsing has a measurable per-event CPU cost of approximately 11-12 ns on the tested Apple M5 system.

For larger records, byte framing dominates and the additional structured cost becomes negligible.

## Allocation statement

The correct conclusion is:

> No per-event managed allocation was observed in the structured pipeline benchmark. A fixed 104-byte allocation was observed per measured `ReadEvents` invocation.

This must not be generalized to future pipeline stages that may retain events, parse timestamps, extract semantic features, or cross asynchronous boundaries.

## Limitations

The benchmark uses:

- valid synthetic records only;
- a fixed structural prefix;
- an empty structured-event handler;
- memory-resident input;
- one tested Apple M5 system.

It does not include:

- malformed-record recovery cost;
- statistical anomaly detection;
- semantic processing;
- timestamp materialization;
- event retention;
- asynchronous queues;
- owned anomaly candidates.

Those stages require separate measurements when introduced.

## Conclusion

The M3 structured-event architecture is validated.

The primary cost is CPU time per event, not managed-memory growth.

The current design is therefore suitable as the input layer for the next milestone, where statistical anomaly detection can operate synchronously over `LogEventView` without forcing every event into an owned heap representation.
