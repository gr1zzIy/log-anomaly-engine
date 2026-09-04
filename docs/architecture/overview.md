# Architecture Overview

## Current architecture

The project intentionally starts with a small dependency graph.

```text
                +---------------------------+
                | LogAnomalyEngine.Cli      |
                +-------------+-------------+
                              |
                              v
                +---------------------------+
                | LogAnomalyEngine.Core     |
                +---------------------------+
                     ^                 ^
                     |                 |
          +----------+---+       +-----+------------------+
          | Tests        |       | Benchmarks             |
          +--------------+       +------------------------+
```

There is no Clean Architecture, CQRS, mediator layer, repository layer, or infrastructure project at this stage.

Those abstractions are not required for the current problem.

## Responsibilities

### LogAnomalyEngine.Cli

Responsibilities:

- command-line entry point;
- argument parsing;
- input/output coordination;
- process exit codes;
- user-facing terminal output;
- composition of core components.

The CLI must not contain anomaly-detection algorithms.

### LogAnomalyEngine.Core

Responsibilities:

- streaming log processing;
- parsing;
- feature extraction;
- anomaly detection;
- reusable data structures;
- performance-sensitive algorithms.

Core is marked as AOT compatible.

Platform-specific code should be avoided unless a measurable benefit exists and a portable fallback is available.

### LogAnomalyEngine.Tests

Responsibilities:

- correctness tests;
- boundary-condition tests;
- regression tests;
- cross-platform behavioral verification.

Performance assertions generally do not belong in unit tests.

### LogAnomalyEngine.Benchmarks

Responsibilities:

- BenchmarkDotNet microbenchmarks;
- comparison of alternative implementations;
- measurement of allocations;
- controlled performance experiments.

Benchmark code must not be referenced by production projects.

## Planned processing pipeline

The expected direction is:

```text
Input
  |
  v
Streaming Reader
  |
  v
Log Entry Framing
  |
  v
Parser
  |
  v
Structured Event
  |
  v
Feature Extraction
  |
  v
Fast Statistical Detector
  |
  +------------------------+
  | normal                 | candidate
  v                        v
Output / skip        Semantic Detector
                              |
                              v
                     Combined Anomaly Score
                              |
                              v
                           Output
```

This architecture is conceptual. Components will be extracted only when their responsibilities become clear in real code.

## Performance-sensitive boundaries

Expected hot-path areas:

- byte-buffer scanning;
- line/event framing;
- token parsing;
- template hashing;
- feature extraction;
- fast anomaly scoring.

Hot-path APIs should prefer:

- spans;
- pooled buffers where useful;
- value types where they improve measured behavior;
- explicit ownership of memory;
- minimal encoding conversions.

However, low-level techniques are not goals by themselves.

## Encoding strategy

The first log-processing stages should operate on UTF-8 bytes whenever possible.

Reasons:

- log files are commonly UTF-8;
- converting every line into a managed `string` creates avoidable allocations;
- many structural parsing operations only need ASCII delimiters;
- semantic conversion can be deferred until a candidate actually requires text-level processing.

This is a design direction, not a requirement that every component avoid strings.

## Error handling

The engine should distinguish:

- malformed log input;
- unsupported encoding;
- invalid CLI arguments;
- I/O failures;
- internal processing errors.

Malformed individual log events should not necessarily terminate processing of an entire large file.

The exact policy will be defined during the reader/parser milestones.

## Cross-platform policy

Primary targets:

```text
Windows x64     win-x64
macOS ARM64     osx-arm64
```

The codebase should remain portable between both development environments.

OS-specific behavior must be isolated and documented.

## Native AOT policy

The production CLI is published with Native AOT.

Core is continuously checked for AOT compatibility.

A dependency that breaks AOT support should not be adopted casually. Before accepting it, evaluate:

1. whether the feature is necessary;
2. whether an AOT-compatible alternative exists;
3. whether the dependency can be isolated outside the production path;
4. whether losing AOT is worth the trade-off.

## Project extraction policy

Do not split `Core` preemptively.

A separate project may be introduced when at least one of the following becomes true:

- it has a clear independent responsibility;
- it has different dependencies;
- it has different AOT requirements;
- it has different release/runtime requirements;
- isolation significantly improves testing or benchmarking.

Possible future projects include:

```text
LogAnomalyEngine.Parsing
LogAnomalyEngine.Detection
LogAnomalyEngine.Semantics
```

but none of them should exist merely to make the solution look architecturally sophisticated.

## Dependency direction

Production dependency direction must remain simple:

```text
CLI -> Core
```

Tests and benchmarks may depend on Core.

Core must never depend on:

- CLI;
- Tests;
- Benchmarks.

## Design principle

Prefer the simplest architecture that allows:

- correct behavior;
- measurable performance;
- clear ownership;
- reproducible experiments;
- future extension.

Architecture should follow observed complexity, not anticipate hypothetical complexity.
