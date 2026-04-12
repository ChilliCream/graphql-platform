# CLAUDE.md - Claude Code Configuration

This file provides guidance to Claude Code when working with this repository.

## Build

### Website

Use `yarn` instead of `npm`.

```bash
cd website
yarn
```

### C# Source Code

```bash
dotnet build src/All.slnx
```

Each area has its own solution file, so you can build or test a subset directly:

```bash
dotnet test src/HotChocolate/Fusion
```

## Orchestration

- **You are the orchestrator, not the worker.** Keep the main context window clean for decision-making. Never do work yourself that a subagent could do.
- **Context window discipline**: When told "let it cook" or "don't inspect" — trust the subagent, don't re-read its output.
- **Team composition**: Minimum for non-trivial work is lead developer + devil's advocate.

## Verification

- "Done" means: compiles, tests pass, verified by running the relevant tests.
- Never mark work complete without proving it works.
- Run tests with `--filter` during iteration — never the full suite unnecessarily.

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.

## Code Quality

### C# / .NET

- Always use curly braces for loops and conditionals, no exceptions
- File-scoped namespaces, 4-space indent
- Test naming: `Method_Should_Outcome_When_Condition`
- No vacuous assertions (`Assert.NotNull` alone is not a test)
- If you need 8 stubs + reflection, you're at the wrong test tier
- Do not use em dash style sentences in docs, comments, or XML documentation. Use commas, periods, parentheses, or colons instead.

### Testing

- Prefer snapshot tests over manual `Assert` calls — use **CookieCrumble** for snapshots
- CookieCrumble has native snapshot support for `IExecutionResult`, `GraphQLHttpResponse`, and other core types
- For smaller snapshots, prefer **inline snapshots** (`MatchInlineSnapshot`) over snapshot files
- For tests with multiple assertions, use **Markdown snapshots** (`MatchMarkdownSnapshot`)
- Snapshot tests: update from `__mismatch__/` directory, understand ordering issues before updating
- Filter tests during iteration — never run the full suite unnecessarily
- Real databases in integration tests, not mocks (unless explicitly instructed otherwise)

## Performance

### C# / .NET

This is framework code — performance matters. Aim for zero allocations on hot paths.

- Use `ChunkedArrayWriter` or `PooledArrayWriter` when you need an `IBufferWriter<byte>` for in-memory byte writing.

## Tools

### C# / .NET

If you need to search for packages on nuget.org use the `dotnet` cli, eg `dotnet package search HotChocolate`.
