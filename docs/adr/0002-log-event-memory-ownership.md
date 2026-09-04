# ADR 0002 — Log Event Memory Ownership

## Status

Accepted.

## Context

`StreamingLogReader` exposes each log line as `ReadOnlySpan<byte>` backed by a reusable buffer rented from `ArrayPool<byte>`.

The span is valid only while the reader callback is executing. After the callback returns, the same buffer may be overwritten by the next stream read and is eventually returned to the pool.

A structured log-event representation must therefore avoid pretending that the underlying memory has a longer lifetime than it actually has.

At the same time, the hot path should avoid allocating strings or copying every log line because the planned anomaly-detection architecture processes all events through a cheap first-stage detector and sends only suspicious candidates to more expensive processing.

## Decision

Use a two-level ownership model.

### 1. Ephemeral zero-copy event view

The primary structured representation is `LogEventView`.

`LogEventView` is a `readonly ref struct` that references the current UTF-8 log line and exposes structured fields as slices of that line.

The initial view contains:

- raw UTF-8 line;
- timestamp slice;
- parsed log level;
- source/category slice;
- message slice.

The view does not own memory.

Its lifetime is restricted to the synchronous processing scope of the current reader callback.

Conceptually:

```text
ArrayPool<byte> buffer
        |
        v
ReadOnlySpan<byte> line
        |
        v
LogEventView
        |
        +-- timestamp slice
        +-- level
        +-- source slice
        +-- message slice
```

No strings are created merely to represent an event.

### 2. Owned representation only when retention is required

A future owned event or anomaly-candidate type will be introduced only for events that need to outlive the callback.

Examples include:

- suspicious events selected by the statistical fast path;
- events queued for semantic analysis;
- events retained for reporting;
- events crossing an asynchronous boundary.

That owned representation may copy only the required UTF-8 bytes or materialize selected strings.

The important rule is:

> copying is performed when ownership is required, not for every input event.

This aligns memory cost with the hybrid anomaly-detection architecture.

## Consequences

### Positive

- no per-event string allocation is required on the normal streaming hot path;
- the memory lifetime is explicit in the type system;
- accidental storage of pooled-buffer-backed spans in normal heap objects is prevented;
- structured parsing can remain zero-copy;
- expensive ownership is reserved for suspicious candidates;
- the design is compatible with Native AOT.

### Restrictions

Because `LogEventView` is a `ref struct`, it cannot be:

- boxed;
- stored in a normal class field;
- captured by a lambda or closure;
- used across `await`;
- used across `yield`;
- retained after the reader callback completes.

These restrictions are intentional and reflect the actual lifetime of the underlying pooled buffer.

## Rejected alternatives

### Store `ReadOnlyMemory<byte>` in a heap object

Rejected for the streaming hot path.

A `ReadOnlyMemory<byte>` instance can outlive the callback while still referencing the pooled array. That would make it possible to retain memory that is later overwritten or returned to `ArrayPool<byte>`.

The type would therefore imply ownership/lifetime guarantees that do not exist.

### Copy every log line into a new byte array

Rejected because it introduces allocation and memory-copy cost for every event, including the expected majority of normal events.

This conflicts with the high-throughput first-stage pipeline.

### Convert every field to `string`

Rejected because UTF-8 decoding and string allocation would become mandatory even when later stages do not require text objects.

### Keep pooled buffers alive per event

Rejected because it complicates ownership, increases pool pressure, and makes lifetime management substantially more error-prone.

## Pipeline implication

The intended M3+ processing model is:

```text
StreamingLogReader
        |
        v
ReadOnlySpan<byte>
        |
        v
Structured parser
        |
        v
LogEventView
        |
        v
Statistical fast path
        |
        +-- normal event ------> discard / aggregate
        |
        `-- suspicious event --> create owned candidate
                                  |
                                  v
                              semantic stage
```

The zero-copy view is therefore the default representation, while owned data is an explicit escalation step.

## Future work

A later M3 issue will define the owned candidate representation once the requirements of the statistical and semantic stages are concrete.

The project should not introduce that type prematurely.
