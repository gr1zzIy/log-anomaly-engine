# Research Result 0001 — Avoiding Re-Scanning of Carried Log-Line Fragments

## Status

Validated performance optimization.

## Date

2026-09-04

## Context

`StreamingLogReader` processes UTF-8 log data in reusable byte buffers. When a log line spans multiple stream reads, the incomplete tail is carried into the next iteration.

The original implementation scanned the complete available buffer from the beginning after each read. As a result, bytes belonging to an already scanned carried fragment were scanned again before newly read bytes were processed.

Conceptually, the old behavior was:

```text
read N:
[ complete lines ][ partial line ]
                         |
                         +-- scanned to the end

read N+1:
[ carried partial line ][ new bytes ]
^
+-- scanning started here again
```

The hypothesis was that repeated scanning of already processed bytes caused measurable overhead for oversized and misaligned log records.

## Optimization

The reader now remembers how many bytes were already scanned before the next stream read.

Delimiter search starts from the first newly read byte:

```text
[ already scanned fragment ][ newly read bytes ]
                             ^
                             search starts here
```

The full line still starts at the beginning of the carried fragment, so line framing semantics remain unchanged.

The optimization:

- does not change the public API;
- does not add managed allocations;
- does not require unsafe code;
- does not change CRLF or EOF behavior;
- preserves the existing `ReadOnlySpan<byte>` callback contract.

## Controlled benchmark

Benchmark:

`OversizedLineCarryoverBenchmarks`

Test configuration:

```text
Dataset size:       64,000,000 bytes
Line length:        100,000 bytes
Reader buffer:      64 KiB
BenchmarkDotNet:    0.15.8
```

Two scenarios were compared:

### UnalignedReads

Stream reads may cross log-line boundaries.

This creates carried fragments and exercises the path where already scanned data can be processed again.

### LineAlignedReads

Stream reads are constrained to end on log-line boundaries.

This acts as a control scenario and largely avoids partial-line carryover.

Both scenarios process the same bytes and the same log lines through the same production reader.

## Environment

```text
OS:                 macOS Tahoe 26.6.2 (25G83)
Kernel:             Darwin 25.6.0
CPU:                Apple M5
Physical cores:     10
Logical cores:      10
Architecture:       ARM64
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
JIT:                RyuJIT armv8.0-a
BenchmarkDotNet:    0.15.8
```

## Results before optimization

| Method | Mean | StdDev | Ratio | Managed allocations |
| --- | ---: | ---: | ---: | ---: |
| UnalignedReads | 19.75 ms | 0.007 ms | 1.00 | none observed |
| LineAlignedReads | 15.01 ms | 0.011 ms | 0.76 | none observed |

The unaligned carryover path required approximately 31.6% more time than the aligned control:

```text
19.75 / 15.01 ≈ 1.316
```

## Results after optimization

| Method | Mean | StdDev | Ratio | Managed allocations |
| --- | ---: | ---: | ---: | ---: |
| UnalignedReads | 15.20 ms | 0.039 ms | 1.00 | none observed |
| LineAlignedReads | 15.00 ms | 0.015 ms | 0.99 | none observed |

The aligned control remained effectively unchanged:

```text
15.01 ms -> 15.00 ms
```

The optimized unaligned path changed from:

```text
19.75 ms -> 15.20 ms
```

which corresponds to approximately:

```text
23.0% lower processing time
```

or, expressed as throughput improvement:

```text
approximately 29.9% higher throughput
```

for this controlled benchmark scenario.

## Interpretation

The result strongly supports the hypothesis that repeated scanning of carried fragments was the dominant source of the previously observed carryover penalty.

The control scenario is important: `LineAlignedReads` remained effectively unchanged while `UnalignedReads` improved substantially. This indicates that the improvement targeted the intended execution path rather than producing a general benchmark-wide speedup.

After the optimization, the remaining difference between aligned and unaligned reads is approximately 1.3%, which suggests that buffer compaction itself is comparatively inexpensive for this workload.

## Conclusion

The optimization is retained.

The experiment demonstrates that avoiding redundant work can produce a substantial improvement without introducing:

- unsafe code;
- additional abstractions;
- a ring buffer;
- a more complex memory ownership model;
- public API changes.

This result also establishes a project rule for future performance work:

> optimize only after a controlled benchmark demonstrates a measurable problem, and validate the change using the same workload and hardware.

## Limitations

The result applies to the tested workload and hardware.

It must not be generalized as:

- a universal 23% improvement for all log files;
- an ARM64 versus x64 comparison;
- a guarantee that every oversized-line workload behaves identically.

The same before/after experiment should later be repeated on the Windows x64 development machine to test whether the direction of the improvement is reproducible across platforms.

Absolute benchmark results from the Apple M5 and the separate Windows x64 machine must not be directly compared as a CPU-architecture benchmark because the machines differ in processor, memory subsystem, operating system, power management, and runtime environment.
