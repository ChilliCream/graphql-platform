# AGENTS.md - OpenAI Codex Configuration

This file provides guidance to OpenAI Codex and other coding agents when working with this repository.

## Build

### Website

Use `yarn` instead of `npm`.

```bash
cd website
yarn
```

### C# Source Code

Build full solution:

```bash
dotnet build src/All.slnx
```

Build or test a subset directly (each area has its own solution file):

```bash
dotnet test src/HotChocolate/Fusion
```

## Orchestration

- You are the orchestrator, not the worker.
- Keep the main context window focused on decisions.
- Do not do work yourself that a subagent could do.
- Context-window discipline: when instructed to "let it cook" or "don't inspect", trust the subagent and do not re-read its output.
- For non-trivial work, minimum team composition is lead developer plus devil's advocate.

## Verification

- "Done" means the code compiles, tests pass, and results are verified by running relevant tests.
- Never mark work complete without proving it works.
- During iteration, use `--filter` and avoid running the full suite unnecessarily.

## Core Principles

- Simplicity first: make every change as simple as possible and keep impact minimal.
- No lazy fixes: find root causes and avoid temporary patches.
- Minimal impact: touch only what is necessary and avoid regressions.

## Code Quality

### C# / .NET

- Always use curly braces for loops and conditionals.
- Use file-scoped namespaces and 4-space indentation.
- Use test naming format: `Method_Should_Outcome_When_Condition`.
- Do not write vacuous assertions (`Assert.NotNull` alone is not a complete test).
- If a test requires excessive stubs and reflection, use a more appropriate test tier.
- Do not use em dash style sentences in docs, comments, or XML documentation. Use commas, periods, parentheses, or colons instead.

### Testing

- Prefer snapshot tests over manual `Assert` calls using CookieCrumble.
- Use CookieCrumble native snapshot support for `IExecutionResult`, `GraphQLHttpResponse`, and related core types.
- For small snapshots, prefer inline snapshots (`MatchInlineSnapshot`).
- For tests with multiple assertions, use markdown snapshots (`MatchMarkdownSnapshot`).
- For snapshot updates, use `__mismatch__/` and understand ordering issues before updating snapshots.
- Filter tests during iteration and avoid full-suite runs unless necessary.
- Use real databases in integration tests instead of mocks unless explicitly instructed otherwise.

## Performance

### C# / .NET

This is framework code. Performance matters; optimize for low allocations on hot paths.

- Use `ChunkedArrayWriter` or `PooledArrayWriter` when an in-memory `IBufferWriter<byte>` is required.

## Tools

### C# / .NET

Use `dotnet` CLI to search NuGet packages, for example:

```bash
dotnet package search HotChocolate
```
