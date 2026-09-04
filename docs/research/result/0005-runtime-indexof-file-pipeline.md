# Research Result 0005 — Runtime IndexOf in the File-Backed Streaming Pipeline

## Status

Validated production optimization.

## Date

2026-09-04

## Context

Previous M2 experiments established that the runtime-optimized line-feed scanner based on `ReadOnlySpan<byte>.IndexOf` substantially outperforms the explicit scalar scanner in both scanner microbenchmarks and the in-memory `StreamingLogReader` pipeline.

The final validation step was to determine how much of that improvement remains when the reader processes data through `FileStream`.

The experiment compares the same file-backed benchmark with:

```text
BEFORE: StreamingLogReader + ScalarLineScanner
AFTER:  StreamingLogReader + RuntimeLineScanner
```

No benchmark-specific reader implementation was introduced.

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

The before and after measurements were performed on the same machine and runtime generation.

## Benchmark

Benchmark:

`FileStreamingLogReaderBenchmarks`

Dataset:

```text
64 MiB synthetic UTF-8 log file
FileStream-backed input
Sequential access
```

Reader buffer sizes:

```text
4 KiB
64 KiB
1 MiB
```

Because the same temporary file is processed repeatedly during the benchmark, these measurements represent warm file-backed sequential processing and may benefit from the operating-system filesystem cache.

They must not be interpreted as raw physical SSD throughput.

## Results

| Buffer size | Scalar reader | Runtime reader | Speedup | Time reduction |
| ---: | ---: | ---: | ---: | ---: |
| 4 KiB | 24.78 ms | 12.219 ms | 2.03x | 50.7% |
| 64 KiB | 20.92 ms | 8.342 ms | 2.51x | 60.1% |
| 1 MiB | 21.03 ms | 8.228 ms | 2.56x | 60.9% |

Approximate warm file-backed throughput:

| Buffer size | Scalar reader | Runtime reader |
| ---: | ---: | ---: |
| 4 KiB | 2.52 GiB/s | 5.12 GiB/s |
| 64 KiB | 2.99 GiB/s | 7.49 GiB/s |
| 1 MiB | 2.97 GiB/s | 7.60 GiB/s |

Managed allocations reported by BenchmarkDotNet:

| Buffer size | Scalar reader | Runtime reader |
| ---: | ---: | ---: |
| 4 KiB | 167 B | 164 B |
| 64 KiB | 167 B | 164 B |
| 1 MiB | 167 B | 164 B |

The three-byte difference is negligible and should not be attributed to the scanner implementation without additional evidence.

## Interpretation

### The scanner optimization remains significant with FileStream

The runtime scanner reduces total file-backed processing time by approximately:

```text
50.7% to 60.9%
```

across the tested reader buffer sizes.

The strongest result is the 1 MiB configuration:

```text
21.03 ms -> 8.228 ms
```

which corresponds to approximately:

```text
2.56x speedup
```

This confirms that delimiter scanning remained a substantial part of the total pipeline cost even after adding `FileStream` and filesystem activity.

### The file-backed speedup is smaller than the scanner microbenchmark speedup

This is expected.

The file-backed pipeline contains additional work:

```text
FileStream
    +
OS/filesystem interaction
    +
buffer management
    +
line framing
    +
delimiter scanning
```

Only the delimiter-scanning component was changed.

Therefore, the end-to-end speedup is necessarily bounded by the fraction of execution time originally spent scanning delimiters.

### Larger reader buffers perform better in this workload

With the runtime scanner:

```text
4 KiB  -> 12.219 ms
64 KiB ->  8.342 ms
1 MiB  ->  8.228 ms
```

The 64 KiB and 1 MiB results are close, while 4 KiB is materially slower.

For this workload, increasing the buffer from 64 KiB to 1 MiB yields only a small additional improvement:

```text
8.342 ms -> 8.228 ms
```

approximately 1.4%.

This does not justify changing the project's 64 KiB default buffer solely on the basis of this experiment.

### Allocation behavior is effectively unchanged

The scalar and runtime configurations report approximately the same tiny per-operation managed allocation:

```text
167 B vs 164 B
```

The optimization therefore improves execution time without introducing a meaningful managed-allocation cost in the measured scenario.

## Decision

The `RuntimeLineScanner` integration into `StreamingLogReader` is considered fully validated for the current M2 scope.

Production path:

```text
FileStream / MemoryStream
        |
        v
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

Retain:

- `ScalarLineScanner` as the correctness and non-vectorized performance baseline;
- `Vector128LineScanner` as the explicit SIMD research implementation;
- `RuntimeLineScanner` as the production implementation.

Do not introduce a custom runtime scanner-dispatch abstraction at this stage.

Do not increase the default reader buffer from 64 KiB solely because 1 MiB is marginally faster in this benchmark.

## M2 conclusion

The M2 scanner work demonstrates three important engineering results:

1. Explicit SIMD substantially outperforms the scalar byte-by-byte baseline.
2. The .NET runtime implementation outperforms the handwritten `Vector128` implementation for practical buffer sizes on the tested Apple M5 system.
3. The runtime scanner produces substantial end-to-end improvements in both memory-backed and file-backed reader pipelines.

The runtime-optimized scanner is therefore the selected production implementation.

## Limitations

The result applies to the tested:

- Apple M5 system;
- macOS ARM64 environment;
- .NET SDK 10.0.302;
- .NET Runtime 10.0.10;
- synthetic 64 MiB file-backed workload;
- warm filesystem-cache benchmark behavior.

These values are not raw SSD throughput measurements.

The same relative before/after experiment should later be repeated on the Windows x64 development machine to evaluate cross-platform reproducibility.

Absolute performance numbers from the Apple M5 and Windows x64 systems must not be directly compared as a CPU-architecture benchmark because the machines differ in CPU, memory subsystem, operating system, and other hardware characteristics.
