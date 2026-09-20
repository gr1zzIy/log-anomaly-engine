# Research Result 0010 — Sliding-Window Frequency Detector Performance

## Status

Validated M4 adaptive-detector benchmark.

## Date

2026-09-20

## Context

M4 introduced two online statistical detectors over normalized event fingerprints:

- `FrequencyAnomalyDetector` — cumulative historical frequency;
- `SlidingWindowFrequencyDetector` — bounded history with event eviction.

The sliding-window model improves adaptation to concept drift because old observations eventually leave the model. This benchmark measures the runtime cost of that adaptability and verifies whether state growth remains bounded during a long churning stream.

Two questions are evaluated separately:

```text
1. What is the steady-state latency overhead of sliding-window maintenance?

2. Does managed allocation remain bounded when stream length grows
   while the active model size is fixed?
```

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

## Experiment A — Cumulative vs sliding-window steady state

Benchmark:

`SlidingWindowFrequencyDetectorBenchmarks`

The cumulative detector is pre-seeded with a known fingerprint.

The sliding-window detector is pre-filled to the configured window size, so every measured observation exercises the full steady-state path:

```text
score against historical window
    ->
evict oldest fingerprint
    ->
decrement/remove old count
    ->
enqueue current fingerprint
    ->
increment current count
```

Window sizes:

```text
128
1,024
8,192
```

### Results

| Window size | Cumulative | Sliding window | Ratio | Absolute overhead | Managed allocation |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 128 | 35.77 ns | 44.11 ns | 1.23x | +8.34 ns | none observed |
| 1,024 | 36.18 ns | 43.95 ns | 1.21x | +7.77 ns | none observed |
| 8,192 | 36.37 ns | 44.14 ns | 1.21x | +7.77 ns | none observed |

## Interpretation of steady-state cost

The adaptive model adds approximately:

```text
7.8–8.3 ns/event
```

over the cumulative baseline.

Relative overhead is approximately:

```text
21–23%
```

for the tested workload.

The important scaling result is that the sliding-window latency remains essentially constant as the configured window grows from 128 to 8,192 events:

```text
44.11 ns
43.95 ns
44.14 ns
```

This is consistent with expected constant-time queue and dictionary operations in the steady-state path.

BenchmarkDotNet reported no managed allocations for either detector in these known-event steady-state measurements.

## Experiment B — Long-stream bounded-state churn

Benchmark:

`SlidingWindowBoundedStateBenchmarks`

Configuration:

```text
window size:             1,024
distinct event pool:     4,096
```

The stream continuously cycles through more fingerprints than can fit in the active window.

This forces repeated:

```text
new fingerprint insertion
    +
old fingerprint expiration
    +
dictionary removal
```

Two stream lengths were measured:

```text
8,192 events
65,536 events
```

The second stream is exactly 8x longer.

### Results

| Event count | Total mean | Approx. time/event | Approx. throughput | Allocated |
| ---: | ---: | ---: | ---: | ---: |
| 8,192 | 394.4 us | 48.14 ns | 20.77 M events/s | 107.93 KB |
| 65,536 | 3,196.8 us | 48.78 ns | 20.50 M events/s | 107.93 KB |

## Key observation — allocation is bounded by model capacity

The stream length increases by:

```text
8x
```

while total managed allocation remains:

```text
107.93 KB
```

for both benchmark cases.

This is strong evidence that allocation is dominated by the bounded detector state rather than by the number of processed events.

The active model repeatedly reuses its existing queue and dictionary storage after reaching its required capacity.

The benchmark therefore exhibits the intended bounded-memory behavior:

```text
stream length increases
        |
        +-- execution time increases approximately linearly
        |
        `-- total managed allocation remains approximately constant
```

The normalized allocation per processed event consequently falls as the stream becomes longer, because the same bounded initialization/state cost is amortized over more observations.

## Time scaling under churn

Processing cost per event remains nearly constant:

```text
8,192 events:
~48.14 ns/event

65,536 events:
~48.78 ns/event
```

The difference is about 1.3%.

This indicates approximately linear processing-time scaling for the tested churning workload.

The long-stream cost is higher than the known-event steady-state result (~44 ns/event) because churn exercises additional dictionary insertion/removal behavior rather than repeatedly updating one already-known fingerprint.

## GC observations

BenchmarkDotNet reported GC activity during the bounded-state benchmark.

However, Gen0/Gen1 counts from different benchmark durations should not be interpreted as retained-memory measurements or compared as direct event-level allocation rates.

The stronger allocation result is the total allocated memory:

```text
107.93 KB
```

which is unchanged when event count grows from 8,192 to 65,536.

No claim is made here about the exact internal lifetime or generation of individual `Queue<T>` or `Dictionary<TKey,TValue>` backing arrays.

## Statistical/performance trade-off

The measured trade-off is now concrete.

### Cumulative model

```text
~36 ns/event
no steady-state allocation observed
unbounded historical influence
poor concept-drift adaptation
```

### Sliding-window model

```text
~44 ns/event
no steady-state allocation observed
bounded active history
fast concept-drift adaptation
```

The adaptive behavior therefore costs roughly:

```text
+8 ns/event
```

in this workload.

For the current project goals, this is a small runtime cost for a materially different statistical behavior.

## Decision

Keep the current `Queue<ulong>` + `Dictionary<ulong, int>` implementation.

Do not replace the queue with a custom ring buffer at this stage.

The current evidence shows:

- constant-time steady-state behavior over tested window sizes;
- no observed steady-state managed allocation;
- bounded total allocation under a long churning stream;
- approximately linear processing-time scaling;
- only ~21–23% latency overhead versus the cumulative baseline.

A custom state container would add implementation complexity without evidence of a material bottleneck.

## M4 implication

The sliding-window detector is now a stronger adaptive statistical baseline than the cumulative detector.

The cumulative detector remains useful as:

- a reference implementation;
- a non-adaptive baseline;
- a comparison point for concept-drift experiments.

The sliding-window detector is the preferred candidate for the hybrid fast path when bounded recent-history behavior is required.

## Limitations

The benchmark does not yet evaluate:

- detection quality on labeled anomaly datasets;
- optimal window-size selection;
- different rates of concept drift;
- burst anomalies;
- millions of simultaneously active fingerprints;
- multithreaded detector access;
- file-backed end-to-end detection;
- semantic-stage candidate selection quality.

The benchmark also uses one Apple M5 / ARM64 environment.

Absolute numbers should not be directly compared with the separate Windows x64 machine.

## Conclusion

The adaptive detector achieves bounded state with modest steady-state overhead:

```text
cumulative:
~36 ns/event

sliding window:
~44 ns/event

adaptive overhead:
~8 ns/event
```

Under continuous fingerprint churn, processing remains approximately:

```text
~48 ns/event
```

while managed allocation remains fixed at approximately:

```text
107.93 KB
```

even when the processed stream grows by 8x.

This validates the sliding-window model as the current M4 adaptive statistical baseline.
