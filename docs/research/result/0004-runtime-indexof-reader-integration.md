# Research Result 0004 — Runtime IndexOf Integration in StreamingLogReader

## Status

Validated production optimization.

## Date

2026-09-04

## Context

M2 introduced three line-feed scanner implementations:

- `ScalarLineScanner` — explicit byte-by-byte reference implementation;
- `Vector128LineScanner` — explicit SIMD research implementation;
- `RuntimeLineScanner` — `ReadOnlySpan<byte>.IndexOf` based implementation.

The scanner microbenchmark showed that `RuntimeLineScanner` substantially outperformed both the scalar reference and the handwritten `Vector128` implementation on the tested Apple M5 / ARM64 environment.

The next question was whether that scanner-level advantage would survive at the complete streaming-reader level.

To answer that, the production `StreamingLogReader` was evaluated before and after replacing:

```text
ScalarLineScanner
```

with:

```text
RuntimeLineScanner
```

No other reader behavior was changed.

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
Physical cores:     10
Logical cores:      10
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

The before and after measurements were executed on the same machine and runtime generation.

The scalar baseline was measured from a separate Git worktree pointing at `main`.

## Experiment A — StreamingLogReaderBenchmarks

Dataset:

```text
16 MiB synthetic UTF-8 log dataset
MemoryStream-backed input
```

Measured reader buffer sizes:

```text
4 KiB
64 KiB
1 MiB
```

### Results

| Buffer size | Scalar reader | Runtime IndexOf reader | Speedup | Time reduction |
| ---: | ---: | ---: | ---: | ---: |
| 4 KiB | 4.790 ms | 1.704 ms | 2.81x | 64.4% |
| 64 KiB | 4.884 ms | 1.710 ms | 2.86x | 65.0% |
| 1 MiB | 4.901 ms | 1.786 ms | 2.74x | 63.6% |

Approximate in-memory throughput:

| Buffer size | Scalar | Runtime IndexOf |
| ---: | ---: | ---: |
| 4 KiB | 3.26 GiB/s | 9.17 GiB/s |
| 64 KiB | 3.20 GiB/s | 9.14 GiB/s |
| 1 MiB | 3.19 GiB/s | 8.75 GiB/s |

No managed allocations were observed in the measured operations.

## Experiment B — LineLengthImpactBenchmarks

Dataset:

```text
16,000,000 bytes
64 KiB reader buffer
MemoryStream-backed input
```

Measured line lengths:

```text
125 B
1,000 B
16,000 B
100,000 B
```

### Results

| Line length | Scalar reader | Runtime IndexOf reader | Speedup | Time reduction |
| ---: | ---: | ---: | ---: | ---: |
| 125 B | 4.465 ms | 1.3247 ms | 3.37x | 70.3% |
| 1,000 B | 3.957 ms | 0.5720 ms | 6.92x | 85.5% |
| 16,000 B | 3.797 ms | 0.4855 ms | 7.82x | 87.2% |
| 100,000 B | 3.861 ms | 0.5331 ms | 7.24x | 86.2% |

Approximate in-memory throughput:

| Line length | Scalar | Runtime IndexOf |
| ---: | ---: | ---: |
| 125 B | 3.34 GiB/s | 11.25 GiB/s |
| 1,000 B | 3.77 GiB/s | 26.05 GiB/s |
| 16,000 B | 3.92 GiB/s | 30.69 GiB/s |
| 100,000 B | 3.86 GiB/s | 27.95 GiB/s |

These throughput numbers describe a memory-resident synthetic framing workload. They are not disk-throughput or complete application-throughput claims.

## Interpretation

### Scanner optimization survives at reader level

The `StreamingLogReaderBenchmarks` result shows that replacing the scalar scanner with the runtime-optimized scanner reduces total reader time by approximately 64–65% for the tested buffer sizes.

This demonstrates that delimiter scanning was a major component of the scalar reader's total execution cost.

### Line-length sensitivity changes significantly

The improvement is especially large for medium and long records.

For the 16,000-byte workload:

```text
3.797 ms -> 0.4855 ms
```

which is approximately:

```text
7.82x faster
```

The shorter 125-byte workload improves by a smaller factor:

```text
3.37x
```

because per-line overhead such as callback invocation and framing bookkeeping represents a larger fraction of total work when the dataset contains many more individual records.

### Runtime implementation is preferred over handwritten SIMD

The previous scanner microbenchmark established that `RuntimeLineScanner` outperformed the explicit `Vector128LineScanner` for practical buffer sizes.

The end-to-end results now show that the runtime-based approach also produces a major improvement in the complete streaming reader.

This supports using the runtime implementation in production while retaining the explicit `Vector128` scanner as a research and comparison implementation.

## Decision

Use `RuntimeLineScanner` in `StreamingLogReader`.

Retain:

- `ScalarLineScanner` as the non-vectorized correctness and performance baseline;
- `Vector128LineScanner` as the explicit SIMD research implementation;
- `RuntimeLineScanner` as the production scanner.

Do not add a custom scanner-dispatch abstraction to the hot path at this stage.

The production dependency remains simple:

```text
StreamingLogReader
    |
    v
RuntimeLineScanner
    |
    v
ReadOnlySpan<byte>.IndexOf
    |
    v
runtime-selected optimized implementation
```

## Allocation behavior

BenchmarkDotNet reported no managed allocations for the measured steady-state operations.

The correct interpretation is:

> no managed allocations were observed in these benchmark scenarios.

This is not a universal claim about every invocation, every stream type, or the complete future anomaly-detection pipeline.

## Limitations

These results apply to the tested:

- Apple M5 system;
- macOS ARM64 environment;
- .NET SDK 10.0.302;
- .NET Runtime 10.0.10;
- synthetic memory-resident datasets.

The very high GiB/s values in the line-length experiment are possible because the dataset is repeatedly processed from memory and may benefit from the processor cache hierarchy. They must not be presented as physical storage throughput.

The same before/after experiment should later be repeated on the Windows x64 development machine.

Absolute Apple M5 and Windows x64 results must not be used as a direct CPU-architecture comparison because the systems differ in CPU, memory subsystem, operating system, power management, and other hardware characteristics.

## Next step

The next validation should measure the optimized scanner in the file-backed reader benchmark.

This will determine how much of the in-memory speedup remains when the pipeline includes:

```text
FileStream
    ->
StreamingLogReader
    ->
RuntimeLineScanner
```

After that experiment, the runtime scanner integration can be considered fully validated for M2 production use.
