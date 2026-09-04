# Benchmark Methodology

## Purpose

This document defines how performance claims are measured in the Log Anomaly Engine project.

The goal is to prevent misleading conclusions caused by noisy environments, hidden SIMD usage, cold-start effects, unrealistic datasets, or comparisons between nonequivalent implementations.

## Core rule

**No performance claim without a baseline and a reproducible measurement.**

Examples of acceptable claims:

- `VectorizedScanner` is 1.8x faster than `ScalarScanner` on dataset X and hardware Y.
- The optimized parser allocates 0 B per processed line in steady state.
- Native AOT reduces startup time under the tested conditions.

Examples of unacceptable claims:

- SIMD is faster.
- `Span<T>` is zero-allocation.
- Native AOT is more performant.
- This parser can process several GB/s.

Those statements may be true in a specific context, but they must be demonstrated.

## Benchmark categories

### Microbenchmarks

Use BenchmarkDotNet for isolated operations such as:

- newline scanning;
- delimiter scanning;
- timestamp parsing;
- log-level parsing;
- tokenization;
- template hashing;
- anomaly-score calculation.

Microbenchmarks should minimize unrelated I/O.

### End-to-end benchmarks

Measure realistic pipelines including:

- file reading;
- chunk processing;
- parsing;
- feature extraction;
- anomaly detection.

These tests should report total throughput and memory behavior.

### Research experiments

Research experiments may combine performance and detection metrics.

They must use controlled datasets and record all relevant environment metadata.

## Scalar baseline

For SIMD-related experiments, the scalar implementation must genuinely be scalar.

Do not use APIs such as `Span<T>.IndexOf` as the scalar reference when the runtime may internally vectorize them.

The scalar baseline should use an explicit element-by-element loop so that the comparison is methodologically valid.

Example conceptual baseline:

```text
for each byte:
    if byte == delimiter:
        record position
```

The optimized implementation can then be compared against explicit vectorized variants.

## Benchmark environment

Scientific benchmark numbers must not be collected from GitHub-hosted CI runners.

CI runners are suitable for:

- compilation;
- tests;
- AOT compatibility;
- smoke benchmarks;
- basic regression detection.

They are not considered a controlled performance environment.

For thesis results, record:

```text
Machine:
CPU:
Physical cores:
Logical cores:
RAM:
OS:
Architecture:
Power mode:
.NET SDK:
.NET Runtime:
Git commit:
Dataset:
```

Where possible:

- close unnecessary applications;
- use stable power settings;
- avoid thermal throttling;
- repeat experiments;
- use the same machine for baseline and candidate measurements.

## Windows x64 and macOS ARM64

The project supports both:

- `win-x64`;
- `osx-arm64`.

Cross-platform comparisons are useful, but they must not be interpreted as direct CPU architecture comparisons unless hardware differences are controlled.

The main purpose is to verify that conclusions remain directionally consistent across platforms.

## Build configuration

Performance measurements must use:

```text
Release
```

Do not publish Debug benchmark results.

For relevant experiments compare:

- regular JIT build;
- Native AOT build.

The comparison must state clearly whether startup time, steady-state throughput, or both are being measured.

## Warmup and iteration

BenchmarkDotNet defaults should be preferred unless a specific research reason requires custom settings.

Any custom:

- warmup count;
- iteration count;
- invocation count;
- launch count;

must be documented.

## Allocation measurements

Allocation metrics should distinguish between:

- initialization;
- steady-state hot path;
- model initialization;
- per-event processing.

A project-wide claim of "zero allocation" should not be used unless it is literally demonstrated.

Preferred wording:

> zero managed allocations on the measured parsing hot path

when that is what the benchmark actually proves.

## Input sizes

Microbenchmarks should cover multiple input sizes.

Suggested categories:

- short line: ~80 B;
- medium line: ~256 B;
- large line: ~1 KB;
- very large line: several KB.

End-to-end tests should include files large enough to reduce the influence of startup overhead.

## Metrics

Primary parser metrics:

- MB/s or GB/s;
- events/s;
- ns/op where meaningful;
- allocated bytes/op.

Pipeline metrics:

- total throughput;
- CPU utilization;
- peak RSS;
- p50/p95/p99 latency where meaningful.

AI-stage metrics:

- inference latency;
- events/s;
- percentage of candidates passed to semantic stage;
- total pipeline throughput.

## Reporting speedup

Report both absolute and relative results.

Example:

```text
Scalar:      1.21 GB/s
Vectorized:  2.08 GB/s
Speedup:     1.72x
```

Avoid reporting only percentages.

## Statistical treatment

For important thesis results:

- perform repeated benchmark runs;
- inspect variance;
- report central tendency;
- report dispersion or confidence intervals where practical;
- investigate outliers instead of silently deleting them.

BenchmarkDotNet statistical output may be used for microbenchmarks.

For custom end-to-end experiments, raw measurements should be retained.

## Benchmark artifacts

Generated benchmark output should not normally be committed directly to the source tree.

Final research results that are cited in the thesis should be archived in a dedicated, versioned form such as:

```text
docs/research/results/
```

or an external release artifact.

Each published result must be traceable to:

- source commit;
- dataset;
- environment;
- configuration.

## Performance optimization workflow

The expected workflow is:

```text
1. Write correct baseline
2. Add tests
3. Measure baseline
4. Implement optimization
5. Run correctness tests
6. Measure optimized implementation
7. Compare results
8. Keep optimization only if justified
```

Complex low-level code must not be accepted solely because it is theoretically faster.

Maintainability is part of the engineering trade-off.
