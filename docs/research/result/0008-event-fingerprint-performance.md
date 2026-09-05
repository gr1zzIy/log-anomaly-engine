# Research Result 0008 — Event Fingerprint Performance

## Status

Validated M4 fingerprint hot-path benchmark.

## Date

2026-09-06

## Context

M4 introduced `EventFingerprint`, a deterministic 64-bit FNV-1a fingerprint computed directly from `LogEventView`.

The fingerprint:

- includes log level;
- includes source;
- scans the complete message payload;
- normalizes decimal numeric runs;
- avoids string materialization;
- is deterministic across runs and platforms by construction.

Because fingerprinting is expected to run for every structured event entering the statistical detector, its CPU and allocation characteristics must be measured independently.

## Benchmark

Benchmark:

`EventFingerprintBenchmarks`

Measured message lengths:

```text
32 B
256 B
4096 B
```

Event construction is performed outside the measured operation.

The measured operation creates a `LogEventView` over existing memory and computes the fingerprint.

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

## Results

| Message length | Mean | Approx. ns/message byte | Approx. message scan throughput | Managed allocation |
| ---: | ---: | ---: | ---: | ---: |
| 32 B | 18.35 ns | 0.57 ns/B | 1.62 GiB/s | none observed |
| 256 B | 179.48 ns | 0.70 ns/B | 1.33 GiB/s | none observed |
| 4096 B | 3,127.52 ns | 0.76 ns/B | 1.22 GiB/s | none observed |

The throughput column is an approximate normalization based only on message length divided by mean execution time. The actual fingerprint also processes the event level, source, field separators, and normalization logic.

## Key observations

### Fingerprint cost grows with message length

Measured execution time:

```text
32 B    ->   18.35 ns
256 B   ->  179.48 ns
4096 B  -> 3127.52 ns
```

This is expected.

Unlike `StructuredLogParser`, which only scans the structural prefix and exposes the message as a span, `EventFingerprint` intentionally reads the complete message body.

Its complexity is therefore approximately:

```text
O(message length)
```

for a fixed-size source field.

### No managed allocations were observed

BenchmarkDotNet reported no managed allocations for all measured fingerprint operations.

This confirms that complete-message normalization and hashing can remain allocation-free in the statistical hot path.

### Large messages make fingerprinting a material CPU stage

For a 4096-byte message, fingerprint computation takes approximately:

```text
3.13 us/event
```

That is no longer negligible compared with the earlier structural-parser cost of roughly 15-16 ns per event.

This distinction is important for the M4 architecture:

```text
structured parsing:
cheap prefix processing

fingerprinting:
full-message processing
```

Therefore, once statistical detection is integrated end to end, fingerprint computation may become one of the dominant CPU stages for large log records.

### The current result does not justify premature hash replacement

The benchmark establishes a measurable cost, but it does not yet show that the fingerprint algorithm is the system bottleneck under the complete detector workload.

Replacing FNV-1a with a more complex hash, vectorized implementation, sampled-message strategy, or partial-template algorithm would change either complexity, reproducibility, or normalization semantics.

Those alternatives should only be evaluated if the complete statistical pipeline demonstrates that fingerprinting materially limits throughput.

## Decision

Keep the current `EventFingerprint` implementation for the first statistical detector.

The implementation provides:

- deterministic output;
- numeric-run normalization;
- zero observed managed allocation;
- simple cross-platform semantics;
- predictable linear scaling with message length.

Do not optimize or replace the hash algorithm yet.

Instead, integrate it into the first online frequency/rarity detector and benchmark the complete statistical path.

## Architectural implication

The M4 hot path is expected to become:

```text
LogEventView
    |
    v
EventFingerprint
    |
    v
online frequency state
    |
    v
rarity/anomaly score
```

The next experiment should determine the combined cost of:

- full-message fingerprinting;
- state lookup/update;
- anomaly-score computation.

That end-to-end detector benchmark will show whether hashing, dictionary state, or another stage dominates.

## Allocation statement

The correct conclusion is:

> No managed allocations were observed for the measured `EventFingerprint.Compute` operations.

This does not imply that the future detector state itself is allocation-free, because adding previously unseen fingerprints to a dictionary or another state structure may allocate memory as the model grows.

Steady-state updates and model-growth allocations should be measured separately.

## Limitations

The benchmark uses synthetic messages containing repeated numeric patterns.

It does not yet measure:

- highly diverse UTF-8 text;
- messages without numeric runs;
- very short source fields versus long source fields;
- dictionary lookup/update cost;
- state growth;
- anomaly scoring;
- collision behavior;
- complete file-backed statistical detection.

The benchmark also represents one Apple M5 / ARM64 environment.

The relative behavior should later be evaluated on the Windows x64 development system.

## Conclusion

The normalized fingerprint satisfies the first M4 hot-path requirement:

```text
deterministic
+
allocation-free in measured steady-state computation
+
linear and predictable CPU cost
```

The next optimization decision should be made at the complete statistical-detector level rather than from the fingerprint microbenchmark alone.
