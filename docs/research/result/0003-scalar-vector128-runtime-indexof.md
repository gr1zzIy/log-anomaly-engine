# Research Result 0003 — Scalar vs Vector128 vs Runtime IndexOf

## Status

Validated SIMD scanner comparison.

## Date

2026-09-04

## Context

The scalar line-feed scanner established during M1 is intentionally implemented as an explicit byte-by-byte loop so that it can serve as a non-vectorized reference baseline.

During M2, a `Vector128<byte>` implementation was introduced to evaluate explicit SIMD acceleration. A third implementation based on `ReadOnlySpan<byte>.IndexOf` was benchmarked as a production-oriented reference because the .NET runtime may use architecture-specific vectorized implementations internally.

The objective was not to prove that handwritten SIMD is inherently faster, but to determine which implementation provides the best measured behavior on the target runtime and hardware.

## Implementations

### Scalar

Explicit byte-by-byte search.

```text
byte -> compare -> next byte
```

This implementation is retained as the correctness oracle and non-vectorized baseline.

### Vector128

Explicit `Vector128<byte>` comparison with a 16-byte vector width.

The implementation:

- loads 16 bytes at a time;
- compares them against LF (`0x0A`);
- extracts a bit mask;
- uses trailing-zero count to locate the first matching byte;
- falls back to scalar processing for the remaining tail.

### Runtime IndexOf

Uses:

```csharp
span.IndexOf((byte)'\n')
```

This delegates implementation details to the .NET runtime and allows the runtime to select architecture-specific optimizations.

## Environment

```text
Platform:           macOS ARM64
CPU:                Apple M5
.NET SDK:           10.0.302
.NET Runtime:       10.0.10
BenchmarkDotNet:    0.15.8
```

## Benchmark design

Buffer lengths:

```text
32 B
128 B
1 KiB
16 KiB
64 KiB
```

Two cases were measured:

- no LF exists in the buffer;
- LF is placed at the final byte.

Both cases require scanning essentially the complete buffer.

No managed allocations were observed in the measured operations.

## Results — no delimiter

| Buffer | Scalar | Vector128 | Runtime IndexOf | Vector128 speedup vs scalar | Runtime speedup vs scalar |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 32 B | 7.104 ns | 0.444 ns | 0.486 ns | 16.0x | 14.6x |
| 128 B | 33.926 ns | 3.004 ns | 1.813 ns | 11.3x | 18.7x |
| 1 KiB | 240.568 ns | 32.192 ns | 17.465 ns | 7.47x | 13.78x |
| 16 KiB | 3,662.127 ns | 532.085 ns | 248.760 ns | 6.88x | 14.72x |
| 64 KiB | 14,613.155 ns | 2,133.945 ns | 952.919 ns | 6.85x | 15.34x |

## Results — delimiter at final byte

| Buffer | Scalar | Vector128 | Runtime IndexOf | Vector128 speedup vs scalar | Runtime speedup vs scalar |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 32 B | 7.219 ns | 0.412 ns | 0.466 ns | 17.5x | 15.5x |
| 128 B | 33.038 ns | 3.406 ns | 2.016 ns | 9.70x | 16.39x |
| 1 KiB | 237.952 ns | 32.698 ns | 19.163 ns | 7.28x | 12.42x |
| 16 KiB | 3,655.616 ns | 540.880 ns | 250.738 ns | 6.76x | 14.58x |
| 64 KiB | 14,640.842 ns | 2,139.949 ns | 956.499 ns | 6.84x | 15.31x |

## Key observations

### Explicit SIMD is substantially faster than the scalar reference

For large buffers, `Vector128` is approximately 6.8x faster than the scalar implementation.

This validates the original hypothesis that delimiter scanning is highly suitable for SIMD acceleration.

### Runtime IndexOf outperforms handwritten Vector128 for practical buffer sizes

Starting at 128 bytes, `RuntimeIndexOf` is consistently faster than the explicit `Vector128` implementation.

At 64 KiB without a delimiter:

```text
Vector128:      2,133.945 ns
RuntimeIndexOf:   952.919 ns
```

The runtime implementation is therefore approximately:

```text
2.24x faster than the handwritten Vector128 implementation
```

for that workload.

### The 32-byte result should not drive production design

At 32 bytes, explicit `Vector128` is slightly faster than `RuntimeIndexOf`.

However, the absolute difference is only a few hundredths of a nanosecond in this benchmark. That difference is too small to justify a separate production dispatch path by itself.

### Match and no-match behavior are consistent

Placing LF at the final byte produces results close to the no-match case.

This is expected because both scenarios require scanning nearly the complete buffer.

### No managed allocations were observed

All three scanner implementations operate without observed managed allocations in the measured benchmark scenarios.

## Decision

Do not replace the production scalar reader directly with the handwritten `Vector128LineScanner` yet.

The explicit `Vector128` implementation should be retained as:

- a research implementation;
- a correctness comparison target;
- an explicit SIMD baseline;
- a useful cross-platform experiment for ARM64 and x64.

For production integration, `ReadOnlySpan<byte>.IndexOf` is currently the strongest candidate because it:

- substantially outperforms the scalar implementation;
- outperforms the handwritten Vector128 implementation for practical buffer sizes;
- keeps production code simpler;
- allows the .NET runtime to choose architecture-specific SIMD optimizations.

## Next experiment

The next step is an end-to-end reader A/B/C comparison:

```text
StreamingLogReader + ScalarLineScanner
StreamingLogReader + Vector128LineScanner
StreamingLogReader + Runtime IndexOf
```

The purpose is to determine how much scanner-level speedup survives at the complete reader level.

A microbenchmark improvement must not be assumed to translate proportionally to end-to-end throughput.

## Limitations

These measurements apply to the tested Apple M5 / ARM64 environment and runtime version.

The experiment should later be repeated on the Windows x64 development system.

Absolute results between Apple M5 and the Windows machine must not be interpreted as an ARM64-versus-x64 CPU comparison. The relevant cross-platform question is whether the relative ordering and speedup trends reproduce on both systems.
