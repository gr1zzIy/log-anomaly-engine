# Research Result 0009 — Online Frequency Detector Performance

## Status

Validated M4 baseline detector benchmark.

## Date

2026-09-20

## Context

M4 introduced `FrequencyAnomalyDetector`, a deterministic online baseline detector built on normalized event fingerprints.

For each observed `LogEventView`, the detector:

1. computes an `EventFingerprint`;
2. looks up the fingerprint in a `Dictionary<ulong, long>`;
3. evaluates the event against the historical state;
4. computes empirical frequency and rarity;
5. updates the occurrence count and total event count.

Two performance modes must be treated separately:

```text
steady state:
known fingerprint -> lookup + update

model growth:
new fingerprint -> insert + possible dictionary growth
```

Combining both behaviors into one benchmark would hide the difference between normal online processing and state expansion.

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

## Experiment A — Known-event steady state

Benchmark:

`FrequencyAnomalyDetectorBenchmarks`

The detector is seeded before measurement so that the measured event always maps to an existing fingerprint.

### Result

| Method | Mean | StdDev | Managed allocation |
| --- | ---: | ---: | ---: |
| KnownEvent | 35.59 ns | 0.051 ns | none observed |

The measured operation includes:

```text
LogEventView
    |
    v
EventFingerprint
    |
    v
Dictionary lookup
    |
    v
count update
    |
    v
frequency + rarity calculation
```

### Interpretation

The detector processes a known event in approximately:

```text
35.6 ns/event
```

on the tested Apple M5 system.

BenchmarkDotNet reported no managed allocation for this measured steady-state operation.

This means the normal update path can execute without per-event managed allocation once the fingerprint is already present in the model.

## Experiment B — Model growth

Benchmark:

`FrequencyAnomalyDetectorGrowthBenchmarks`

Each benchmark invocation creates an empty detector and inserts only previously unseen fingerprints.

Measured model sizes:

```text
128
1,024
8,192
```

### Results

| Distinct events | Total mean | Approx. time per inserted fingerprint | Allocated |
| ---: | ---: | ---: | ---: |
| 128 | 4.045 us | 31.6 ns | 9.95 KB |
| 1,024 | 37.679 us | 36.8 ns | 99.82 KB |
| 8,192 | 319.791 us | 39.0 ns | 440.9 KB |

Approximate cumulative allocation normalized by the number of inserted fingerprints:

| Distinct events | Approx. allocated bytes / inserted fingerprint |
| ---: | ---: |
| 128 | 79.6 B |
| 1,024 | 99.8 B |
| 8,192 | 55.1 B |

These normalized values describe cumulative managed allocation during model construction. They are **not** retained-memory-per-fingerprint measurements.

### Scaling

Model-build time increases approximately linearly over the tested range:

```text
128 events    ->   4.045 us
1,024 events  ->  37.679 us
8,192 events  -> 319.791 us
```

The amortized insertion cost remains in the approximate range:

```text
31.6–39.0 ns / distinct fingerprint
```

The increase with model size is modest for the tested range and includes:

- fingerprint computation;
- dictionary lookup;
- insertion;
- occasional internal dictionary resize;
- rarity calculation;
- detector construction cost amortized across the run.

## GC behavior

BenchmarkDotNet reported:

```text
128:
Gen0 activity

1,024:
Gen0 activity

8,192:
Gen0 + Gen1 + Gen2 activity
```

The larger model therefore crosses a point where model construction creates enough managed state to involve higher GC generations during repeated benchmark execution.

The benchmark does not isolate the exact internal allocation responsible for each generation, so the Gen2 result should not be attributed to a specific `Dictionary` array or to the large-object heap without a dedicated memory experiment.

The important result is that state growth is not allocation-free, while steady-state updates are.

## Key distinction

The detector has two different memory behaviors.

### Known event

```text
existing fingerprint
    |
    v
lookup + update
    |
    v
no managed allocation observed
```

### New event structure

```text
new fingerprint
    |
    v
dictionary insertion
    |
    v
model state grows
    |
    v
managed allocation may occur
```

This distinction is expected and is important for interpreting future end-to-end anomaly benchmarks.

## Decision

Keep the current `Dictionary<ulong, long>` implementation as the M4 statistical baseline.

The current evidence does not justify implementing a custom hash table or pre-allocation policy.

Reasons:

- known-event processing is approximately 35.6 ns/event;
- no managed allocation was observed on the known-event hot path;
- model-growth cost remains approximately linear over 128–8,192 distinct fingerprints;
- the current implementation is simple and deterministic.

Optimization of state storage should be considered only if later workloads demonstrate that:

- the number of distinct fingerprints becomes very large;
- GC pressure materially affects end-to-end throughput;
- retained model memory becomes excessive.

## Statistical limitation of the current model

Performance validation does not remove an important algorithmic limitation.

The current detector uses cumulative historical frequency:

```text
fingerprintCount / totalCount
```

All observations therefore have permanent influence on the model.

If the normal behavior of a system changes over time, old observations continue to dominate the estimated frequency.

This creates a **concept drift** problem.

Example:

```text
historical period:
Pattern A is common
Pattern B is rare

later period:
Pattern B becomes normal
Pattern A disappears
```

A cumulative model adapts slowly because the old counts are never forgotten.

The next M4 research step should therefore compare the cumulative baseline with an adaptive model such as a bounded sliding window or decayed frequency estimator.

## Allocation statement

The correct result is:

> No managed allocations were observed for known-fingerprint steady-state observations. Model growth allocates managed memory as new fingerprint state is added.

It would be incorrect to describe the complete detector as globally allocation-free.

## Limitations

The benchmark does not yet measure:

- retained model memory;
- millions of distinct fingerprints;
- fingerprint collisions;
- multithreaded access;
- end-to-end structured reader + detector throughput;
- concept drift;
- bounded-memory behavior;
- adaptive frequency estimation.

The measurements represent one Apple M5 / ARM64 environment.

Cross-platform reproducibility should later be checked on the Windows x64 development system using same-machine before/after comparisons rather than absolute Mac-versus-Windows timings.

## Conclusion

The cumulative frequency detector is a suitable first statistical baseline:

```text
known-event hot path:
~35.6 ns/event
no managed allocation observed

model growth:
~31.6–39.0 ns per inserted fingerprint
managed allocation grows with model state
```

The next M4 work should focus on statistical adaptability rather than low-level state-container optimization.
