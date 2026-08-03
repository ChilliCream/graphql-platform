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

## Code Quality

### C# / .NET

- Always use curly braces for loops and conditionals, no exceptions
- File-scoped namespaces, 4-space indent
- Test naming: `Method_Should_Outcome_When_Condition`
- No vacuous assertions (`Assert.NotNull` alone is not a test)
- If you need 8 stubs + reflection, you're at the wrong test tier
- Do not use em dash style sentences in docs, comments, or XML documentation. Use commas, periods, parentheses, or colons instead.
- XML docs should describe the contract and concepts, not internals like pooling, iteration mechanics or leak other implementation detail.
- XML docs and comments are 1-2 sentences stating the contract: what it is, what null or edge values mean. No rationale, no use-case examples, no design justification. If a sentence explains why the design is right instead of what the member promises, delete it. The same applies to docs pages: every sentence must inform the reader, none may justify the design.
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

### Nitro persisted operations (Fusion Aspire)

After adding or editing any `.graphql` document under `src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/Operations`, regenerate the `.sha256` sidecars and verify them:

```bash
.github/scripts/nitro-aspire-operations.sh update \
    --source src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/Operations
.github/scripts/nitro-aspire-operations.sh verify \
    --source src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/Operations \
    --output /tmp/nitro-aspire-operations.json
```

Never hand-write or hand-edit a `.sha256` sidecar. The `update` command is the only source of sidecar content, and `verify` must pass before handoff.
