# Research Result 0002 — Optimized Reader Line-Length Sensitivity

## Status

Validated characterization result for the optimized scalar streaming reader.

## Date

2026-09-04

## Context

After eliminating redundant re-scanning of carried log-line fragments, the streaming reader was re-evaluated across several fixed log-line lengths.

The purpose of this experiment was to determine whether oversized records still exhibit a substantial throughput penalty after the carryover optimization.

This is a characterization benchmark, not a comparison between different machines or CPU architectures.

## Benchmark

Benchmark:

`LineLengthImpactBenchmarks`

Configuration:

```text
Dataset size:       16,000,000 bytes
Reader buffer:      64 KiB
Line lengths:       125 B
                    1,000 B
                    16,000 B
                    100,000 B
BenchmarkDotNet:    0.15.8
```

The total number of processed bytes is held constant while only the line length changes.

The benchmark callback performs no per-line decoding or string allocation.

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

## Results

| Line length | Mean | StdDev | Approx. throughput | Managed allocations |
| ---: | ---: | ---: | ---: | ---: |
| 125 B | 4.446 ms | 0.0061 ms | 3.35 GiB/s | none observed |
| 1,000 B | 3.927 ms | 0.0194 ms | 3.79 GiB/s | none observed |
| 16,000 B | 3.734 ms | 0.0146 ms | 3.99 GiB/s | none observed |
| 100,000 B | 3.789 ms | 0.0039 ms | 3.93 GiB/s | none observed |

Approximate throughput is calculated as:

```text
dataset bytes / mean execution time
```

and converted to GiB/s using `2^30` bytes per GiB.

## Interpretation

### Short lines

The 125-byte case is the slowest of the tested configurations.

This is expected because a fixed-size dataset contains many more line delimiters and therefore causes more:

- scalar delimiter checks;
- callback invocations;
- line-framing bookkeeping.

The result indicates measurable per-line overhead even when total processed bytes remain constant.

### Medium and large lines

The 1,000-byte and 16,000-byte cases provide the highest throughput among the tested workloads.

The 16,000-byte case reaches approximately 3.99 GiB/s in this controlled in-memory benchmark.

### Oversized lines

The 100,000-byte case is larger than the initial 64 KiB reader buffer and therefore exercises the oversized-record path.

Its throughput is approximately 3.93 GiB/s, only about 1.5% below the 16,000-byte case:

```text
3.93 / 3.99 ≈ 0.985
```

This indicates that oversized records no longer exhibit a substantial penalty in this workload after the carried-fragment re-scan optimization.

This result is consistent with the controlled carryover experiment documented in:

```text
docs/research/results/0001-tail-rescan-optimization.md
```

where the difference between aligned and unaligned oversized reads was reduced to approximately 1%.

## Allocation behavior

BenchmarkDotNet reported no managed allocations for the measured steady-state benchmark operations.

This must be interpreted narrowly:

> no managed allocations were observed for these measured benchmark scenarios.

It is not a project-wide guarantee that every reader invocation or every possible workload is allocation-free.

## Conclusion

The optimized scalar streaming reader behaves consistently across medium, large, and oversized log records in the tested in-memory workload.

The remaining performance sensitivity is primarily visible for very short lines, where per-line processing overhead becomes more significant.

No additional oversized-line buffer-compaction redesign is justified by the current evidence.

The current reader implementation is therefore considered a suitable scalar baseline for the next stage of the project.

## Decision

Do not introduce:

- ring buffers;
- segmented line representations;
- unsafe memory management;
- additional buffer-compaction abstractions;

for the current reader implementation.

The next performance work should focus on delimiter scanning itself and compare explicit scalar and SIMD implementations.

## Limitations

The result applies to:

- the tested Apple M5 system;
- .NET SDK 10.0.302 / .NET Runtime 10.0.10;
- the synthetic fixed-width dataset;
- a 64 KiB reader buffer;
- in-memory processing through `MemoryStream`.

It must not be interpreted as:

- end-to-end disk throughput;
- a universal throughput guarantee;
- an ARM64 versus x64 comparison;
- evidence that line length can never affect other future parsing or anomaly-detection stages.

The same benchmark may later be repeated on the Windows x64 development system to verify whether the qualitative behavior is reproducible across platforms.
