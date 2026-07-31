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

## Code Quality

### C# / .NET

- Always use curly braces for loops and conditionals.
- Use file-scoped namespaces and 4-space indentation.
- Use test naming format: `Method_Should_Outcome_When_Condition`.
- Do not write vacuous assertions (`Assert.NotNull` alone is not a complete test).
- If a test requires excessive stubs and reflection, use a more appropriate test tier.
- Do not use em dash style sentences in docs, comments, or XML documentation. Use commas, periods, parentheses, or colons instead.
- Do not make new parameters optional just to avoid updating call sites. A parameter should only be optional when it has a sensible semantic default and the API is frequently used (where call-site brevity outweighs explicitness). If a parameter is logically required, make it required and update all call sites.

### Testing

- Prefer snapshot tests over manual `Assert` calls using CookieCrumble.
- Use CookieCrumble native snapshot support for `IExecutionResult`, `GraphQLHttpResponse`, and related core types.
- For small snapshots, prefer inline snapshots (`MatchInlineSnapshot`).
- For tests with multiple assertions, use markdown snapshots (`MatchMarkdownSnapshot`).
- Avoid `Assert.DoesNotContain` as it is a weak assertion that easily goes out of date, it only proves something is absent without verifying what *is* present. Prefer `Assert.Equal` to check the entire string value, or `Assert.Collection` to verify the complete contents of a collection.
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
