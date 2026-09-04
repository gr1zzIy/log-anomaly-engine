# Log Anomaly Engine

High-performance streaming log analysis and anomaly detection engine built with .NET 10.

The project is being developed as both an engineering project and a research platform for a master's qualification work in Computer Science / Artificial Intelligence Systems.

## Project goal

The goal is to build a CLI tool capable of processing large text log files with minimal memory allocations and detecting suspicious or unusual events using a hybrid approach:

- fast streaming parsing;
- statistical anomaly detection;
- semantic analysis for selected candidates;
- Native AOT deployment;
- SIMD acceleration where it is proven to improve performance.

The main research idea is not to run an ML model for every log entry. Instead, the engine should use a fast path to cheaply filter normal events and send only suspicious candidates to a more expensive semantic detector.

## Current status

The repository currently contains the engineering foundation:

- .NET 10 solution;
- CLI project;
- reusable Core library;
- xUnit tests;
- BenchmarkDotNet project;
- Native AOT compatibility checks;
- CI for Windows x64 and macOS ARM64;
- shared analyzers and code-style rules.

No anomaly-detection algorithm has been implemented yet.

## Supported development targets

| Platform | RID | Architecture |
|---|---|---|
| Windows | `win-x64` | x64 |
| macOS | `osx-arm64` | Apple Silicon |

The application is intended to remain cross-platform. Platform-specific optimizations may be introduced only when they have a measurable benefit and a portable fallback exists.

## Solution structure

```text
log-anomaly-engine/
├── benchmarks/
│   └── LogAnomalyEngine.Benchmarks/
├── docs/
│   ├── adr/
│   ├── architecture/
│   └── research/
├── src/
│   ├── LogAnomalyEngine.Cli/
│   └── LogAnomalyEngine.Core/
├── tests/
│   └── LogAnomalyEngine.Tests/
├── .github/
│   └── workflows/
├── Directory.Build.props
├── global.json
└── LogAnomalyEngine.slnx
```

The solution intentionally starts small. New projects will be extracted only when a real boundary appears in the codebase.

## Build

Requirements:

- .NET SDK 10.0.111 or a compatible patch version;
- Native AOT toolchain for the target operating system.

Build and test:

```bash
dotnet restore
dotnet build
dotnet test
```

## Native AOT

Windows x64:

```powershell
dotnet publish src/LogAnomalyEngine.Cli/LogAnomalyEngine.Cli.csproj `
  -c Release `
  -r win-x64
```

macOS Apple Silicon:

```bash
dotnet publish src/LogAnomalyEngine.Cli/LogAnomalyEngine.Cli.csproj \
  -c Release \
  -r osx-arm64
```

Native AOT compatibility is checked continuously in CI. A feature that breaks AOT support must either be replaced or explicitly justified.

## Performance principles

Performance claims in this repository must be measurable.

We do not consider an implementation faster because it looks more low-level or uses SIMD. A performance optimization should be accepted only after comparison against a baseline under controlled conditions.

The main metrics are expected to include:

- throughput;
- latency;
- allocated bytes;
- peak memory usage;
- CPU utilization;
- anomaly-detection precision;
- recall;
- F1 score;
- false-positive rate.

GitHub-hosted CI runners are used for correctness and compatibility checks, not for publishing scientific benchmark numbers.

## Development roadmap

### M0 — Engineering Foundation
- solution structure;
- tests;
- benchmarks;
- analyzers;
- Native AOT;
- cross-platform CI;
- initial documentation.

### M1 — High-Speed Log Reader
- streaming input;
- scalar baseline;
- chunk-boundary handling;
- pooled buffers;
- throughput benchmarks.

### M2 — SIMD Parser
- scalar reference implementation;
- vectorized delimiter scanning;
- Vector128/Vector256/Vector512 where appropriate;
- benchmark comparison.

### M3 — Structured Event Pipeline
- timestamps;
- log levels;
- sources;
- message extraction;
- template representation.

### M4 — Statistical Anomaly Detection
- online statistics;
- frequency anomalies;
- unseen-event detection;
- temporal anomaly scoring.

### M5 — Semantic Anomaly Detection
- embeddings;
- vector similarity;
- candidate analysis;
- semantic anomaly score.

### M6 — Hybrid Detection Engine
- fast path;
- semantic slow path;
- combined scoring;
- threshold calibration;
- ablation experiments.

### M7 — Production CLI
- files and stdin;
- filtering;
- terminal output;
- JSON output;
- Native AOT release binaries.

### M8 — Research Evaluation
- datasets;
- baseline methods;
- reproducible experiments;
- performance and ML metrics;
- statistical analysis.

### M9 — Thesis Release
- stable implementation;
- reproducible results;
- final technical documentation;
- release `v1.0.0`.

## Research direction

The working research direction is:

> A hybrid streaming method for anomaly detection in large-scale text logs based on a low-cost statistical fast path and semantic analysis of selected candidate events.

The exact thesis title and research hypothesis may change as experimental evidence accumulates.

See:

- `docs/research/research-plan.md`
- `docs/research/benchmark-methodology.md`
- `docs/architecture/overview.md`
- `docs/adr/0001-cross-platform-native-aot.md`

## License

A license will be selected before the first public release.
