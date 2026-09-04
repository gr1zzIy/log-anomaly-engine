# ADR-0001: Cross-Platform Native AOT as a Project Constraint

- Status: Accepted
- Date: 2026-09-04

## Context

Log Anomaly Engine is intended to be developed and used on two primary environments:

- Windows x64;
- macOS on Apple Silicon.

The project also targets a high-performance command-line deployment model with low startup overhead, predictable packaging, and no requirement for a separately installed .NET runtime.

Native AOT is therefore attractive for the production CLI.

However, Native AOT introduces constraints around:

- reflection;
- dynamic code generation;
- trimming;
- runtime-loaded assemblies;
- third-party dependencies.

If AOT compatibility is considered only near release time, dependencies selected earlier in development may make migration expensive or impossible.

## Decision

Native AOT compatibility is treated as a project-level engineering constraint from the beginning.

The production CLI uses:

```xml
<PublishAot>true</PublishAot>
```

The reusable Core library uses:

```xml
<IsAotCompatible>true</IsAotCompatible>
```

CI verifies Native AOT publishing for:

- `win-x64`;
- `osx-arm64`.

The project does not rely on cross-compiling Native AOT binaries between these operating systems. Each target is built on a compatible runner.

## Supported targets

```text
Windows x64
RID: win-x64

macOS Apple Silicon
RID: osx-arm64
```

## Consequences

### Positive

- AOT-breaking dependencies are discovered early.
- Release packaging remains simple.
- Startup characteristics can be evaluated throughout development.
- Production dependencies are pressured toward predictable runtime behavior.
- Windows and Apple Silicon remain first-class targets.

### Negative

- Some reflection-heavy libraries may be difficult to use.
- Some ML or serialization libraries may require additional evaluation.
- CI takes longer because AOT publishing is checked separately.
- Native binaries are platform-specific.

## Alternatives considered

### Add Native AOT only before release

Rejected.

This creates a high risk of discovering incompatible dependencies after the architecture has already formed around them.

### Do not use Native AOT

Rejected for now.

The CLI nature of the project makes Native AOT relevant enough to evaluate continuously.

This decision may be revisited if a future mandatory dependency makes AOT impractical and the measured benefits do not justify the cost.

### Support only Windows

Rejected.

Development is expected on both Windows x64 and macOS Apple Silicon, and the project should remain portable across both environments.

## Performance note

Native AOT is not assumed to make steady-state processing faster.

Any claims about:

- startup time;
- executable size;
- memory usage;
- throughput;

must be benchmarked independently.

Native AOT is primarily an engineering and deployment decision unless experimental results demonstrate a measurable performance effect.
