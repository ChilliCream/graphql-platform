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
- **Delegate by default.** Any task with a clear spec and a checkable output goes to a subagent. Keep for yourself only: planning, ambiguous judgment, architecture decisions, and reviewing subagent output.
- **Escalation valve.** Execute directly only when a task has no checkable output, or when a subagent has failed the same task twice.
- **Write a spec before delegating.** Subagents run in a fresh context window and cannot see this conversation. For each delegated task, state: inputs, expected output, and acceptance criteria. Vague instructions cause weaker models to wander.
- **Context window discipline**: When told "let it cook" or "don't inspect" — trust the subagent, don't re-read its output.
- **Team composition**: Minimum for non-trivial work is lead developer + devil's advocate.

## Team

Delegate execution to the subagents defined in `.claude/agents/`. Route by complexity, not habit:

- `implementer` (sonnet): writes and edits code against a clear spec.
- `test-runner` (haiku): runs filtered tests, reports pass/fail plus failure detail.
- `code-reviewer` (sonnet): quality, security, and convention review of a diff.
- `devils-advocate` (inherit): stress-tests the plan and surfaces risks before work starts. This is high-value reasoning with no checkable output, so it runs on the top model, same tier as you.

You (the orchestrator) keep: task decomposition, spec-writing, reviewing results against acceptance criteria, and final judgment. Run independent subagents in parallel. Always report which subagent handled which part.

Adjust this list to match the agents you actually have; the orchestrator can only route to agents named here.

## Verification

- "Done" means: compiles, tests pass, verified by running the relevant tests.
- Never mark work complete without proving it works.
- Verification is delegated to `test-runner`. Review its result against the acceptance criteria you set. Do not re-run or re-read work you were told to trust.
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
- XML docs should describe the contract and concepts, not internals like pooling, iteration mechanics or leak other implementation detail.
- Do not make new parameters optional just to avoid updating call sites. A parameter should only be optional when it has a sensible semantic default and the API is frequently used (where call-site brevity outweighs explicitness). If a parameter is logically required, make it required and update all call sites.

### Testing

- Prefer snapshot tests over manual `Assert` calls, use **CookieCrumble** for snapshots
- CookieCrumble has native snapshot support for `IExecutionResult`, `GraphQLHttpResponse`, and other core types
- For smaller snapshots, prefer **inline snapshots** (`MatchInlineSnapshot`) over snapshot files
- For a collection of results (for example a stream of subscription events), snapshot the list with `MatchInlineSnapshots` (a parallel list of per-element inline snapshots). Do NOT concatenate with `string.Join("---", values).MatchInlineSnapshot(...)`: a manual separator hides element boundaries and reinvents what the collection overload does natively.
- For tests with multiple assertions, use **Markdown snapshots** (`MatchMarkdownSnapshot`)
- Hard limit: a single test method must contain at most 5 `Assert.*` calls. Anything beyond that is too hard to reason about in review, switch to a snapshot (Markdown for multi-shape state, inline or file for a single output)
- Use the AAA section marker style. Each section starts with a single-line comment, the test name documents intent, no paragraph-style block comments above sections:

  ```csharp
  // arrange
  // optional one-line description, only when the next code is non-obvious
  ... arrange code ...

  // act
  ... act code ...

  // assert
  ... assert code ...
  ```

- Avoid `Assert.DoesNotContain` as it is a weak assertion that easily goes out of date, it only proves something is absent without verifying what *is* present. Prefer `Assert.Equal` to check the entire string value, or `Assert.Collection` to verify the complete contents of a collection.
- Snapshot tests: update from `__mismatch__/` directory, understand ordering issues before updating
- Filter tests during iteration, never run the full suite unnecessarily
- Real databases in integration tests, not mocks (unless explicitly instructed otherwise)

## Performance

### C# / .NET

This is framework code — performance matters. Aim for zero allocations on hot paths.

- Use `ChunkedArrayWriter` or `PooledArrayWriter` when you need an `IBufferWriter<byte>` for in-memory byte writing.

## Tools

### C# / .NET

If you need to search for packages on nuget.org use the `dotnet` cli, eg `dotnet package search HotChocolate`.
