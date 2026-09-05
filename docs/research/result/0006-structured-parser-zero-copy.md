# Research Result 0006 — Zero-Copy Structured Parser

## Status

Validated M3 performance result.

## Date

2026-09-05

## Context

M3 introduced `LogEventView`, a `readonly ref struct` that represents a structured log event without owning the underlying UTF-8 memory.

`StructuredLogParser` parses the structural prefix of a log record directly from `ReadOnlySpan<byte>` and exposes:

- timestamp as a span slice;
- log level as an enum;
- source as a span slice;
- message as the remaining span.

The parser does not decode the complete record into a `string`, does not split the message payload, and does not copy the input buffer.

The purpose of this experiment was to compare that design with a straightforward string-based parsing approach.

## Benchmark

Benchmark:

`StructuredLogParserBenchmarks`

Input format:

```text
<timestamp> <level> <source> <message>
```

Example:

```text
2026-09-05T20:00:00Z INFO PaymentService AAAAA...
```

Message lengths:

```text
32 B
256 B
4096 B
```

Both implementations receive an already prepared UTF-8 byte array.

Input construction is performed in `GlobalSetup` and is not included in the measured operation.

## Implementations

### StringBased

The baseline:

1. decodes the complete UTF-8 record to `string`;
2. uses `string.Split` with four output fields;
3. returns a value derived from the parsed fields.

Conceptually:

```text
UTF-8 bytes
    |
    v
string allocation
    |
    v
Split
    |
    v
field strings
```

### ZeroCopy

The optimized parser:

1. scans only the structural prefix;
2. stores timestamp/source/message as slices of the original input;
3. converts only the level token to `LogLevel`;
4. performs no string conversion.

Conceptually:

```text
UTF-8 bytes
    |
    v
StructuredLogParser
    |
    v
LogEventView
```

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

## Results

| Message length | String-based | Zero-copy | Speedup | Time reduction | String allocation | Zero-copy allocation |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 32 B | 42.57 ns | 15.15 ns | 2.81x | 64.4% | 464 B | none observed |
| 256 B | 85.05 ns | 15.99 ns | 5.32x | 81.2% | 1,360 B | none observed |
| 4096 B | 805.89 ns | 15.79 ns | 51.04x | 98.0% | 16,720 B | none observed |

BenchmarkDotNet also reported managed GC activity for the string-based implementation, while no managed allocation was observed for the zero-copy measured operations.

## Key observations

### Zero-copy parsing remains nearly constant as the message grows

Measured zero-copy times:

```text
32 B    -> 15.15 ns
256 B   -> 15.99 ns
4096 B  -> 15.79 ns
```

This is expected from the parser design.

The parser reads:

```text
timestamp
level
source
```

and treats the rest of the input as the message slice.

It does not scan or decode the entire message body.

Therefore, its cost depends primarily on the structural prefix rather than total message length.

### String-based cost grows with the complete record size

Measured string-based times:

```text
32 B    -> 42.57 ns
256 B   -> 85.05 ns
4096 B  -> 805.89 ns
```

Managed allocation grows at the same time:

```text
32 B    ->    464 B
256 B   ->  1,360 B
4096 B  -> 16,720 B
```

The string-based design must decode the complete UTF-8 record and materialize managed text objects before the structured fields can be consumed.

### The 4096-byte result is intentionally asymmetric

The approximately 51x result does not mean that arbitrary zero-copy text processing is universally 51x faster than string processing.

The two approaches implement the intended pipeline differently:

- the string baseline materializes the complete message;
- the zero-copy parser intentionally leaves the message as an unprocessed span.

That asymmetry is part of the architecture being evaluated.

For the planned hybrid anomaly pipeline, this is desirable because the majority of events should pass through a cheap first stage without requiring full text materialization.

## Architectural implication

The benchmark supports the two-level ownership model defined in ADR 0002.

Normal event:

```text
UTF-8 line
    |
    v
LogEventView
    |
    v
statistical fast path
    |
    v
discard / aggregate
```

Suspicious event:

```text
LogEventView
    |
    v
explicit ownership/materialization
    |
    v
semantic stage
```

The cost of copying or decoding message content is therefore paid only when a later stage actually requires ownership.

## Decision

Keep `StructuredLogParser` and `LogEventView` as the default structured representation for the streaming hot path.

Do not:

- decode every event to `string`;
- call `string.Split` in the normal pipeline;
- copy every message into a new byte array;
- parse or normalize the complete message body during structural framing.

Introduce owned data only when an event must outlive the reader callback or cross an asynchronous boundary.

## Allocation statement

The correct interpretation of the benchmark is:

> no managed allocations were observed for the measured zero-copy parser operations.

This is not a universal statement that every future stage of the structured-event or anomaly-detection pipeline will allocate zero managed memory.

## Limitations

The experiment measures structural parsing only.

It does not include:

- timestamp conversion to `DateTimeOffset`;
- semantic tokenization;
- message normalization;
- feature extraction;
- statistical state updates;
- asynchronous processing;
- event retention.

The benchmark also uses synthetic ASCII message payloads.

Future stages may need separate benchmarks when they introduce additional processing over the message body.

## Conclusion

The experiment validates the M3 zero-copy parsing architecture.

The most important result is not a single speedup factor, but the scaling behavior:

```text
message size increases
        |
        +-- string-based parsing: time and allocation increase
        |
        `-- zero-copy structural parsing: approximately constant cost
```

This makes `LogEventView` a suitable hot-path representation for the planned hybrid anomaly-detection pipeline.
