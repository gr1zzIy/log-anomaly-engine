# Research Plan

## Working research topic

**Streaming anomaly detection in large-scale text logs using statistical and semantic analysis.**

The implementation platform is .NET 10. SIMD and Native AOT are engineering mechanisms used to achieve predictable performance, but they are not treated as the scientific novelty by themselves.

## Problem statement

Modern systems generate large volumes of application, infrastructure, security, and diagnostic logs.

A purely semantic approach can detect events that are difficult to capture with fixed rules, but running a machine-learning model for every log event can become the dominant computational bottleneck.

A purely statistical approach is significantly cheaper, but may miss semantically unusual events that are syntactically common.

The project investigates whether a hybrid two-stage detector can reduce expensive semantic processing while preserving useful anomaly-detection quality.

## Working hypothesis

A two-stage streaming detector that combines a low-cost statistical candidate-selection stage with semantic analysis of only suspicious events can reduce computational cost and improve throughput compared with an embedding-only approach while maintaining comparable anomaly-detection quality.

This hypothesis must be validated experimentally and may be refined as the project evolves.

## Proposed method

The current conceptual pipeline is:

```text
Log stream
    |
    v
Streaming parser
    |
    v
Fast statistical detector
    |
    +--------------------+
    | normal             | suspicious
    v                    v
skip semantic path   semantic detector
                         |
                         v
                  combined anomaly score
                         |
                         v
                       result
```

The important idea is selective semantic processing.

The fast stage should be inexpensive enough to execute for every event. The slow stage may use embeddings or another semantic representation, but only for a subset of candidate events.

## Research questions

Initial research questions:

1. How much throughput is lost when semantic analysis is applied to every event?
2. What percentage of events can be rejected by the fast stage without materially reducing recall?
3. Which statistical features are most useful for candidate selection?
4. How does the hybrid detector compare with:
   - statistical-only detection;
   - semantic-only detection;
   - simple rule-based baselines?
5. How does the candidate threshold affect:
   - throughput;
   - recall;
   - precision;
   - false-positive rate?
6. How does the implementation behave on different hardware architectures:
   - x64;
   - ARM64?
7. Which low-level optimizations materially improve throughput, and which only increase implementation complexity?

## Candidate fast-path features

Potential features to evaluate:

- log-level frequency;
- event-template frequency;
- unseen templates;
- token-frequency changes;
- message-length deviations;
- source/component frequency;
- time-window frequency changes;
- inter-arrival timing;
- burst detection.

These are candidates, not commitments. Every feature should earn its place through experiments.

## Candidate semantic stage

Potential options include:

- compact text embeddings;
- sentence-level embeddings;
- local ONNX model;
- quantized model;
- distance to recent normal-event clusters;
- nearest-neighbor similarity;
- centroid-based anomaly scoring.

The first implementation should favor reproducibility and local execution over model complexity.

## Baselines

At minimum, research evaluation should include:

### Baseline A — Rule / keyword search

Examples:

- `grep`;
- regular expressions;
- fixed severity filters.

This is not expected to be a strong anomaly detector, but it provides a practical reference point.

### Baseline B — Statistical-only detector

The same fast path used without semantic analysis.

### Baseline C — Semantic-only detector

Semantic analysis applied to every eligible log event.

### Proposed — Hybrid detector

Statistical candidate selection followed by semantic analysis.

## Evaluation metrics

Detection quality:

- precision;
- recall;
- F1 score;
- false-positive rate;
- false-negative rate.

Performance:

- throughput in MB/s or GB/s;
- events per second;
- p50/p95/p99 processing latency where meaningful;
- allocated bytes per event;
- peak RSS;
- CPU utilization;
- model inference time;
- percentage of events sent to semantic analysis.

## Ablation studies

The final research should include ablation experiments.

Examples:

- without semantic stage;
- without template-frequency feature;
- without temporal feature;
- different candidate thresholds;
- different embedding dimensions;
- scalar versus vectorized parsing;
- FP32 versus quantized semantic model, if applicable.

Ablation studies are important because they show which components of the proposed method are actually responsible for the observed result.

## Datasets

Dataset selection is not fixed yet.

Requirements for research datasets:

- legally usable;
- documented provenance;
- sufficiently large;
- contain or allow construction of anomaly labels;
- representative of realistic log structures.

Synthetic anomaly injection may be used for controlled experiments, but it should not be the only source of evaluation data.

If synthetic anomalies are used, the generation procedure must be documented and reproducible.

## Reproducibility

Every final experiment should record:

- Git commit SHA;
- dataset version/hash;
- operating system;
- CPU;
- RAM;
- .NET SDK/runtime version;
- build configuration;
- runtime identifier;
- benchmark parameters;
- detector configuration;
- random seed where applicable.

Final thesis numbers must be reproducible from the repository or from archived experiment metadata.

## What is not scientific novelty

The following technologies are implementation mechanisms and must not be presented as scientific novelty by themselves:

- .NET 10;
- Native AOT;
- `Span<T>`;
- `Memory<T>`;
- `ArrayPool<T>`;
- AVX2;
- Vector128/256/512;
- ONNX;
- BenchmarkDotNet.

Scientific contribution should be formulated around the proposed detection method, feature combination, candidate-selection strategy, scoring method, or another experimentally validated algorithmic contribution.

## Expected research contribution

A possible formulation, subject to experimental validation:

> An improved streaming anomaly-detection method that reduces the amount of semantic processing through statistical candidate selection while preserving a target level of anomaly-detection quality.

This wording should be treated as a working formulation until the experiments support it.
