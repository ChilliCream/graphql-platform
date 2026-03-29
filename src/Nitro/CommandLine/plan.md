# Nitro CLI Test Migration Plan

## Problem Statement

49 leaf commands exist in the Nitro CLI. 17 have test suites (35%). 4 are excluded from testing (`LoginCommand`, `LogoutCommand`, `FusionRunCommand`, `LaunchCommand`). The remaining **47** need test suites.

Tier classification:
- **Tier A (11)**: No `PrintMutationErrorsAndExit`, test immediately (though many need `AssertHasAuthentication` — requires adding `ISessionService` + `parseResult` to `ExecuteAsync` signature)
- **Tier B (7)**: Use `PrintMutationErrorsAndExit` directly or via shared helpers, need migration before testing
- **Tier C (10)**: Subscription commands needing full migration of both initial mutation and subscription error handling

The goal: complete test suites for all 47 remaining commands, migrating implementations where necessary, with maximum parallelism and consistent quality.

---

## Preparatory Task: ConsoleHelpers Refactor

**Before any stream starts**, a single preparatory task refactors `ConsoleHelpers.cs`:

1. **Rename typed overloads** to reflect what they render:
   - `PrintMutationError(ISchemaChangeViolationError)` → `PrintSchemaChangeViolations(...)`
   - `PrintMutationError(ISchemaVersionChangeViolationError)` → `PrintSchemaVersionChangeViolations(...)`
   - `PrintMutationError(IMcpFeatureCollectionValidationError)` → `PrintMcpFeatureCollectionValidationErrors(...)`
   - `PrintMutationError(IOpenApiCollectionValidationError)` → `PrintOpenApiCollectionValidationErrors(...)`
   - `PrintMutationError(IPersistedQueryValidationError)` → `PrintPersistedQueryValidationErrors(...)`
   - `PrintMutationError(IInvalidGraphQLSchemaError)` → `PrintGraphQLSchemaErrors(...)`
   - `PrintMutationError(IStagesHavePublishedDependenciesError)` → `PrintStagePublishedDependencies(...)`
2. **Update callers** of the renamed methods (the generic dispatcher + any direct callers)
3. **Mark as `[Obsolete]`**: `PrintMutationErrorsAndExit`, `PrintMutationErrors`, `PrintMutationError(object)`
4. **Final cleanup** (after all 28 commands are done): delete the 3 obsolete methods

This keeps the build green — existing callers still compile (with warnings), agents see deprecation, and the generic methods are removed as the last task.

---

## Migration Tier Classification

### Tier A: Test Immediately (11 commands)

Already follow COMMAND_IMPLEMENTATION_GUIDELINES.md or have minimal deviations. Many need `AssertHasAuthentication` added — this requires injecting `ISessionService`, passing `parseResult` to `ExecuteAsync`, and calling `parseResult.AssertHasAuthentication(sessionService)` at the top. All commands must use this standardized pattern.

| Command | Class | Category | Notes |
|---------|-------|----------|-------|
| schema download | `DownloadSchemaCommand` | Schema | Compliant |
| schema upload | `UploadSchemaCommand` | Schema | Compliant |
| workspace current | `CurrentWorkspaceCommand` | Workspace | Compliant |
| workspace show | `ShowWorkspaceCommand` | Workspace | Compliant |
| workspace list | `ListWorkspaceCommand` | Workspace | Needs auth standardization |
| workspace set-default | `SetDefaultWorkspaceCommand` | Workspace | Compliant |
| pat list | `ListPersonalAccessTokenCommand` | PAT | Needs auth standardization |
| client list-versions | `ListClientVersionsCommand` | Client | Needs auth standardization |
| client list-published-versions | `ListClientPublishedVersionsCommand` | Client | Needs auth standardization |
| client download | `DownloadClientCommand` | Client | Needs auth standardization |
| client upload | `UploadClientCommand` | Client | Needs auth standardization |

### Tier B: Migrate Then Test — Non-Subscription (7 commands)

Use `PrintMutationErrorsAndExit` directly or via shared helpers (`FusionPublishHelpers`). Migration: replace with typed error switch, wrap in activity, add auth check, standardize `ExecuteAsync` signature.

| Command | Class | Category | Issues |
|---------|-------|----------|--------|
| workspace create | `CreateWorkspaceCommand` | Workspace | `PrintMutationErrorsAndExit` |
| pat create | `CreatePersonalAccessTokenCommand` | PAT | `PrintMutationErrorsAndExit` |
| pat revoke | `RevokePersonalAccessTokenCommand` | PAT | `PrintMutationErrorsAndExit` |
| mock create | `CreateMockCommand` | Mock | `PrintMutationErrorsAndExit` |
| mock update | `UpdateMockCommand` | Mock | `PrintMutationErrorsAndExit` |
| stage delete | `DeleteStageCommand` | Stage | `PrintMutationErrorsAndExit` |
| stage edit | `EditStagesCommand` | Stage | `PrintMutationErrorsAndExit` |
| mcp create | `CreateMcpFeatureCollectionCommand` | MCP | `PrintMutationErrorsAndExit` |
| mcp delete | `DeleteMcpFeatureCollectionCommand` | MCP | `PrintMutationErrorsAndExit` |
| openapi create | `CreateOpenApiCollectionCommand` | OpenAPI | `PrintMutationErrorsAndExit` |
| openapi delete | `DeleteOpenApiCollectionCommand` | OpenAPI | `PrintMutationErrorsAndExit` |
| client unpublish | `UnpublishClientCommand` | Client | `PrintMutationErrorsAndExit` |
| fusion upload | `FusionUploadCommand` | Fusion | `PrintMutationErrorsAndExit` |
| fusion publish begin | `FusionConfigurationPublishBeginCommand` | Fusion | Via `FusionPublishHelpers` |
| fusion publish start | `FusionConfigurationPublishStartCommand` | Fusion | Via `FusionPublishHelpers` |
| fusion publish commit | `FusionConfigurationPublishCommitCommand` | Fusion | Via `FusionPublishHelpers` |
| fusion publish cancel | `FusionConfigurationPublishCancelCommand` | Fusion | Via `FusionPublishHelpers` |
| fusion publish | `FusionPublishCommand` | Fusion | Via `FusionPublishHelpers` |

### Tier C: Full Subscription Migration Then Test (10 commands)

Replace `PrintMutationErrors` in subscription handlers with **inline `foreach` + `switch`** matching each concrete error type from that command's `.graphql` file. Error routing:
- **Simple errors** (message only) → `await console.Error.WriteLineAsync(error.Message)` (stderr)
- **Rich errors** (tree/structured) → call the renamed typed method (e.g., `console.PrintSchemaChangeViolations(error)`) (stdout)
- **Always end with** one stderr summary line (e.g., `"Validation failed."`)

Also need: `activity.Fail()`/`activity.Success()`, `AssertHasAuthentication` with standardized signature.

Note: `ValidateSchemaCommand` and `PublishSchemaCommand` already have typed error switches for the initial mutation and already have `AssertHasAuthentication`. Their migration scope is limited to the subscription handler only.

| Command | Class | Category | Subscription Method |
|---------|-------|----------|---------------------|
| schema validate | `ValidateSchemaCommand` | Schema | `SubscribeToSchemaValidationAsync` |
| schema publish | `PublishSchemaCommand` | Schema | `SubscribeToSchemaPublishAsync` |
| client validate | `ValidateClientCommand` | Client | `SubscribeToClientValidationAsync` |
| client publish | `PublishClientCommand` | Client | `SubscribeToClientPublishAsync` |
| mcp validate | `ValidateMcpFeatureCollectionCommand` | MCP | `SubscribeToMcpFeatureCollectionValidationAsync` |
| mcp publish | `PublishMcpFeatureCollectionCommand` | MCP | `SubscribeToMcpFeatureCollectionPublishAsync` |
| openapi validate | `ValidateOpenApiCollectionCommand` | OpenAPI | `SubscribeToOpenApiCollectionValidationAsync` |
| openapi publish | `PublishOpenApiCollectionCommand` | OpenAPI | `SubscribeToOpenApiCollectionPublishAsync` |
| fusion validate | `FusionValidateCommand` | Fusion | `SubscribeToSchemaVersionValidationUpdatedAsync` |
| fusion publish validate | `FusionConfigurationPublishValidateCommand` | Fusion | `SubscribeToFusionConfigurationPublishingTaskChangedAsync` |

**Excluded from testing** (4 commands): `LoginCommand`, `LogoutCommand`, `FusionRunCommand`, `LaunchCommand`.

**`FusionConfigurationPublishValidateCommand` special note**: Keep its `throw Exit(...)` calls for pipeline state errors (queued, already failed, already published) — these are guard clauses, not subscription error handling. Only migrate the `PrintMutationErrorsAndExit` (initial mutation) and `PrintMutationErrors` (subscription failure) calls.

---

## Command Pattern Types

| Pattern | Description | Canonical Reference | Est. Tests/Cmd |
|---------|-------------|-------------------|----------------|
| **List** | Paginated query with cursor | `ListApiCommandTests` | 10-14 |
| **Show/Query** | Single resource fetch by ID | `ShowApiCommandTests` | 8-12 |
| **Create/Mutation** | Single mutation with typed errors | `CreateApiCommandTests` | 15-20 |
| **Delete** | Mutation with confirmation prompt | `DeleteApiCommandTests` | 12-16 |
| **Upload** | File read + mutation | (none yet — schema upload first) | 12-18 |
| **Download** | Query + file write | (none yet — schema download first) | 10-14 |
| **Subscription (Validate)** | Mutation + subscribe to validation updates | (none yet — schema validate first) | 18-25 |
| **Subscription (Publish)** | Superset of validate: adds queue, approval, deployment | (none yet — schema publish first) | 22-30 |

---

## Shared Infrastructure

### Already Exists (No Changes Needed)

- **CommandBuilder** — fluent test harness with `ExecuteAsync()` / `Start()` / `RunToCompletionAsync()`
- **InteractionMode** — `Interactive`, `NonInteractive`, `JsonOutput`
- **CommandResult extensions** — `AssertSuccess()`, `AssertError()`, `AssertHelpOutput()`
- **InteractiveCommand** — `Input()`, `SelectOption()`, `Confirm()`
- **Mock convention** — `MockBehavior.Strict` + `VerifyAll()`

### Must Be Built (During Schema Commands)

**1. `ToAsyncEnumerable<T>` helper** — for subscription mock setup. Created as the **first task** of Stream 1 Phase 3 (`ValidateSchemaCommandTests`). Placed in a shared location (e.g., `test/CommandLine.Tests/TestHelpers.cs`). **Hard gate**: no other stream's Phase 3 starts until this helper exists and compiles.

```csharp
internal static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
    IEnumerable<T> items,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    foreach (var item in items)
    {
        yield return item;
    }
}
```

**2. Per-category `*CommandTestHelper.cs` files** — created on demand when the second command in a category shares payloads with the first.

---

## Parallel Streams

5 agents across 4 streams. All streams start in parallel from day 1 (after the preparatory ConsoleHelpers refactor).

```
  PREPARATORY TASK (before streams start):
    Refactor ConsoleHelpers.cs — rename typed overloads,
    mark generics [Obsolete]. Single agent, single PR.

  ============================================================

  STREAM 1 (Schema + Workspace + PAT)        12 commands
  1 agent
  ----------------------------------------
  Phase 1: [schema download] [schema upload]       Tier A
           [ws current] [ws show] [ws list]        Tier A
           [ws set-default]                        Tier A
           [pat list]                              Tier A
  Phase 2: [ws create] [pat create] [pat revoke]   Tier B (migrate + test)
  Phase 3: [schema validate] [schema publish]      Tier C (subscription)
                                                   ^^ first task: create
                                                      ToAsyncEnumerable<T> helper
                                                      + establish subscription
                                                      test pattern

  STREAM 2 (MCP + OpenAPI)                    12 commands
  2 agents: MCP lead + OpenAPI trail
  ----------------------------------------
  MCP agent works ahead. OpenAPI agent waits for
  corresponding MCP command to complete, then copies pattern.

  Phase 1: [mcp list] -> [openapi list]            Tier A
  Phase 2: [mcp create] [mcp delete]               Tier B (migrate + test)
        -> [openapi create] [openapi delete]       Tier B (copy pattern)
           [mcp upload] -> [openapi upload]        Tier A
  Phase 3: [mcp validate] -> [openapi validate]    Tier C (subscription)
           [mcp publish] -> [openapi publish]      Tier C (subscription)

  STREAM 3 (Client + Mock + Stage)            13 commands
  1 agent
  ----------------------------------------
  Phase 1: [client list-versions]                  Tier A
           [client list-published-versions]        Tier A
           [client download] [client upload]       Tier A
           [mock list] [stage list]                Tier A
  Phase 2: [client unpublish]                      Tier B (migrate + test)
           [mock create] [mock update]             Tier B (migrate + test)
           [stage delete]                          Tier B (migrate + test)
  Phase 2b:[stage edit]                            Tier B (DEDICATED — most
                                                   complex interactive command,
                                                   20+ tests, needs focused
                                                   attention separate from
                                                   simpler Phase 2 commands)
  Phase 3: [client validate] [client publish]      Tier C (subscription)

  STREAM 4 (Fusion)                           10 commands
  1 agent
  ----------------------------------------
  Phase 1: [fusion download]                       Tier A
           [fusion settings set]                   Tier A
  Phase 2: Migrate FusionPublishHelpers FIRST      Prerequisite for all below
           [fusion upload]                         Tier B (migrate + test)
           [fusion publish begin]                  Tier B (via helpers)
           [fusion publish start]                  Tier B (via helpers)
           [fusion publish commit]                 Tier B (via helpers)
           [fusion publish cancel]                 Tier B (via helpers)
  Phase 3: [fusion validate]                       Tier C (compose + subscribe)
           [fusion publish]                        Tier B+C (compose + helpers + subscribe)
           [fusion publish validate]               Tier C (subscription, keeps throw Exit)

  ============================================================

  CROSS-STREAM GATE: Before any stream enters Phase 3,
  Stream 1 must complete the FIRST task of its Phase 3:
  create ToAsyncEnumerable<T> helper + complete
  ValidateSchemaCommandTests. This is the only cross-stream
  dependency.

  FINAL CLEANUP (after all streams complete):
    Delete the 3 [Obsolete] methods from ConsoleHelpers.cs.
    Verify no PrintMutationErrorsAndExit calls remain.
```

### Stream Rationale

| Stream | Why These Together |
|--------|-------------------|
| **1: Schema + Workspace + PAT** | Schema is highest priority (already mostly compliant, establishes subscription pattern). Workspace and PAT are standard CRUD with independent client interfaces. Stream 1 Phase 3 is the critical path for all other streams' subscription tests. |
| **2: MCP + OpenAPI** | Structural twins — identical command sets, mirrored type names. 2 agents: MCP lead establishes pattern per command pair, OpenAPI trail copies and adapts. Sequential internally, parallel vs other streams. |
| **3: Client + Mock + Stage** | Client commands share `IClientsClient`. Mock and Stage inject `IApisClient` as secondary. `EditStagesCommand` gets dedicated Phase 2b. |
| **4: Fusion** | All commands share `IFusionConfigurationClient`. `FusionPublishHelpers` migration is prerequisite for Phase 2. `FusionValidateCommand` and `FusionPublishCommand` both test compose+subscribe (reference `FusionComposeCommandTests` for compose failure patterns). |

---

## Subscription Command Test Matrix

### Validate Commands (5 commands)

| Test Case | Initial Mutation | Subscription Events | Expected Outcome |
|-----------|-----------------|-------------------|-----------------|
| Help snapshot | — | — | Help output |
| No auth (Theory x3) | — | — | Auth error on stderr |
| Missing required option | — | — | Option error |
| Each mutation error branch (Fact per mode) | Typed error | — | `activity.Fail()` + error on stderr |
| Null request ID (Theory x3) | Success, null ID | — | `ExitException` |
| Success path | Success | [InProgress, Success] | `activity.Success()` |
| Validation failure with errors | Success | [InProgress, Failed{errors}] | `activity.Fail()` + rich errors to stdout + summary to stderr |
| In-progress only, stream ends | Success | [InProgress] | `activity.Fail()` + `ExitCodes.Error` |
| Unknown event | Success | [UnknownType] | upgrade message + `activity.Fail()` |
| Client exception (Theory x3) | Throws `NitroClientException` | — | Error message |
| Auth exception (Theory x3) | Throws `AuthorizationException` | — | Unauthorized message |

### Publish Commands (5 commands)

All validate tests above, PLUS:

| Additional Test | Subscription Events | Expected Outcome |
|----------------|-------------------|-----------------|
| Queue position | [Queued{pos:3}] | `activity.Update` with position |
| Ready state | [Ready] | `console.Success` |
| Wait-for-approval + deployment errors | [WaitForApproval{deployment}] | Rich errors to stdout + `activity.Update` |
| Approved state | [Approved] | `activity.Update` |
| Force option behavior | — | `console.Log("[yellow]Force push is enabled[/]")` |

### Error Loop Rule

**`return ExitCodes.Error` must ALWAYS be AFTER the foreach loop** — print ALL errors before returning:

```csharp
// CORRECT — all errors printed
foreach (var error in data.Errors)
{
    var errorMessage = error switch { ... };
    await console.Error.WriteLineAsync(errorMessage);
}
return ExitCodes.Error;

// WRONG — only first error printed
foreach (var error in data.Errors)
{
    var errorMessage = error switch { ... };
    await console.Error.WriteLineAsync(errorMessage);
    return ExitCodes.Error;  // ← BUG
}
```

### Subscription Error Handling Pattern (inline switch)

```csharp
case IValidationFailed { Errors: var errors }:
    activity.Fail();
    foreach (var error in errors)
    {
        switch (error)
        {
            // Rich errors → stdout via renamed typed methods
            case ISchemaChangeViolationError e:
                console.PrintSchemaChangeViolations(e);
                break;
            // Simple errors → stderr
            case IConcurrentOperationError e:
                await console.Error.WriteLineAsync(e.Message);
                break;
            // ... all error types from .graphql
            case IError e:
                await console.Error.WriteLineAsync("Unexpected error: " + e.Message);
                break;
        }
    }
    await console.Error.WriteLineAsync("Validation failed.");
    return ExitCodes.Error;  // ← AFTER the loop
```

---

## Quality Gates

### Gate 1: Before Starting a Command

- [ ] Command implementation reviewed against COMMAND_IMPLEMENTATION_GUIDELINES.md
- [ ] `ExecuteAsync` signature standardized: accepts `ParseResult` + `ISessionService`
- [ ] `parseResult.AssertHasAuthentication(sessionService)` at top of `ExecuteAsync`
- [ ] If Tier B/C: migration applied and build verified (`dotnet build`)

### Gate 2: Per-Command Test Completion

- [ ] Help output snapshot test (write empty, fail, paste actual)
- [ ] No-auth test passes (Theory x 3 modes)
- [ ] Client exception test passes (Theory x 3 modes)
- [ ] Authorization exception test passes (Theory x 3 modes)
- [ ] All mode-specific success paths covered
- [ ] All mutation error branches covered (per mode where output differs)
- [ ] Interactive prompting tested (if applicable)
- [ ] `client.VerifyAll()` called in every test with mocks
- [ ] Tests pass: `dotnet test --filter "FullyQualifiedName~{TestClass}"`

### Gate 2.5: Subscription Infrastructure (before any stream enters Phase 3)

- [ ] `ToAsyncEnumerable<T>` helper created in shared test infrastructure
- [ ] `ValidateSchemaCommandTests` complete and passing
- [ ] `SchemaCommandTestHelper.cs` has subscription event factory methods as reference

### Gate 3: Subscription Command Extension (Tier C only)

All of Gate 2, PLUS:

- [ ] Subscription handler uses inline `foreach` + `switch` per error type from `.graphql`
- [ ] Rich errors rendered to stdout via renamed typed methods
- [ ] Simple errors written to stderr
- [ ] Summary error line written to stderr
- [ ] Subscription success path tested
- [ ] Subscription in-progress state tested (`activity.Update`)
- [ ] Subscription queue/approval states tested (publish commands only)
- [ ] Subscription unknown state tested (default handler)
- [ ] Subscription fallthrough (no terminal state) tested (`activity.Fail()` + `ExitCodes.Error`)
- [ ] Activity lifecycle verified: every code path ends in `Fail()` or `Success()`

### Gate 4: Stream Completion

- [ ] All commands in stream pass Gate 2 (or Gate 3 for subscription commands)
- [ ] Per-category test helper files complete and consistent
- [ ] No `[Fact]` tests that should be `[Theory]` (mode coverage)
- [ ] Full stream test run passes: `dotnet test --filter "FullyQualifiedName~{CategoryNamespace}"`

### Gate 5: Project Completion

- [ ] `dotnet test` passes for all CommandLine.Tests
- [ ] COMMAND_TEST_MIGRATION_PROGRESS.md updated: all 47 commands marked `done`
- [ ] 3 `[Obsolete]` methods deleted from `ConsoleHelpers.cs`
- [ ] No remaining `PrintMutationErrorsAndExit` calls in any command
- [ ] All subscription commands use inline error switches

---

## Risk Areas

### Highest Risk

**1. `EditStagesCommand` (Stream 3, Phase 2b — dedicated)**
- Most complex interactive command. JSON config parsing, multi-step `SelectableTable` UI, in-memory state management. Local record types and file-scoped static extension classes.
- **Mitigation**: Dedicated phase (2b). Break into sub-tasks: JSON config path tests, interactive UI flow tests, error handling tests. May need 20+ tests.

**2. `FusionValidateCommand` (Stream 4, Phase 3)**
- Composes source schemas into archive, then validates via subscription. Unique two-level activity tracking.
- **Mitigation**: Test full compose+subscribe flow. Reference `FusionComposeCommandTests` for compose failure patterns. Test: compose failure, validation success, validation failure, mixed results.

**3. `FusionConfigurationPublishValidateCommand` (Stream 4, Phase 3)**
- Throws `ExitException` for unexpected pipeline states. Unique among subscription commands.
- **Mitigation**: Keep `throw Exit(...)` as-is. Test each ExitException path explicitly. Only migrate `PrintMutationErrorsAndExit` and `PrintMutationErrors` calls.

### Medium Risk

**4. MCP/OpenAPI subscription migration (Stream 2, Phase 3)**
- 4 subscription commands need full migration.
- **Mitigation**: Schema subscription pattern from Stream 1 Phase 3 serves as template. MCP and OpenAPI are twins — migrate one, copy to other.

**5. `FusionPublishHelpers` migration (Stream 4, Phase 2 prerequisite)**
- Shared by 5 fusion publish flow commands. Contains 3 `PrintMutationErrorsAndExit` calls and 1 `PrintMutationErrors` call.
- **Mitigation**: Migrate as the first task of Stream 4 Phase 2, before any of the 5 commands that depend on it. Single file, contained scope.

### Lower Risk

**6. Fusion publish flow state management (Stream 4, Phase 2)**
- 5 commands sharing `FusionConfigurationPublishingState` file-based state.
- **Testing strategy**: All mock `IFileSystem`. Each test independent — no shared state, no real files. Mock `FileExists()`, `ReadAllTextAsync()`, `WriteAllTextAsync()` to simulate state.

**7. Archive-based upload commands (MCP, OpenAPI, Fusion)**
- Create archive streams before uploading. Need `IFileSystem` mock.
- **Mitigation**: Check if archive creation is command or client layer. If client-side, mock is straightforward.

---

## Cross-Client Dependencies

| Commands | Primary Client | Secondary Client | Why |
|----------|---------------|-----------------|-----|
| mcp create | `IMcpClient` | `IApisClient` | API selection prompt |
| openapi create | `IOpenApiClient` | `IApisClient` | API selection prompt |
| mock create, mock update | `IMocksClient` | `IApisClient` | API selection prompt |
| stage edit, stage delete | `IStagesClient` | `IApisClient` | API selection prompt |

`IApisClient` mock patterns are established in `ApiCommandTestHelper.cs`. Reuse those.

---

## Estimated Test Counts

| Stream | Category | Commands | Est. Tests |
|--------|----------|----------|-----------|
| 1 | Schema | 4 | 70-90 |
| 1 | Workspace | 5 | 55-70 |
| 1 | PAT | 3 | 38-48 |
| 2 | MCP | 6 | 90-115 |
| 2 | OpenAPI | 6 | 90-115 |
| 3 | Client Remaining | 7 | 95-120 |
| 3 | Mock | 3 | 38-48 |
| 3 | Stage | 3 | 48-62 |
| 4 | Fusion | 10 | 120-160 |
| **Total** | | **47 new** | **~500-650 new tests** |

---

## Inline Fix Strategy

Every command migration follows the same template. The agent fixes the implementation, verifies the build, then writes tests — all as one atomic unit.

### Tier B Migration Checklist

1. Standardize `ExecuteAsync` signature: add `ParseResult` + `ISessionService` if missing
2. Add `parseResult.AssertHasAuthentication(sessionService)` at top of `ExecuteAsync`
3. Replace `PrintMutationErrorsAndExit` with typed error switch (match error types from `.graphql`)
4. Wrap mutation in activity (`console.StartActivity`)
5. Add `activity.Fail()` before error output
6. **`return ExitCodes.Error` must be AFTER the foreach loop** — print ALL errors before returning, not just the first
7. **Do NOT call `console.WriteLine()` before `resultHolder.SetResult(...)`** — the blank line is added automatically by the result formatting infrastructure in `RootCommandExtensions.cs`
8. Add `activity.Success("...")` on success path
8. Use `ExitCodes.Success` / `ExitCodes.Error` (no raw integers)
9. Verify: `dotnet build`

### Tier C Migration Checklist

All of Tier B, PLUS:

1. Replace `PrintMutationErrors(errors)` in subscription handler with inline `foreach` + `switch`
2. Each error type from the command's `.graphql` file gets its own switch arm
3. Rich errors (tree/structured) → call renamed typed method to stdout (e.g., `console.PrintSchemaChangeViolations(e)`)
4. Simple errors (message only) → `await console.Error.WriteLineAsync(e.Message)` to stderr
5. Always end error block with one stderr summary line (e.g., `"Validation failed."`)
6. Add `activity.Fail()` before subscription error output
7. Add `activity.Success("...")` on subscription success
8. Ensure fallthrough (stream ends without terminal state) calls `activity.Fail()` + returns `ExitCodes.Error`
9. Verify: `dotnet build`

---

## MCP/OpenAPI Twin Exploitation

2 agents: MCP lead + OpenAPI trail. Within each command pair, MCP goes first and OpenAPI copies.

| MCP | OpenAPI | Pattern |
|-----|---------|---------|
| `CreateMcpFeatureCollectionCommand` | `CreateOpenApiCollectionCommand` | Create/Mutation |
| `DeleteMcpFeatureCollectionCommand` | `DeleteOpenApiCollectionCommand` | Delete |
| `ListMcpFeatureCollectionCommand` | `ListOpenApiCollectionCommand` | List |
| `UploadMcpFeatureCollectionCommand` | `UploadOpenApiCollectionCommand` | Upload |
| `ValidateMcpFeatureCollectionCommand` | `ValidateOpenApiCollectionCommand` | Subscription (Validate) |
| `PublishMcpFeatureCollectionCommand` | `PublishOpenApiCollectionCommand` | Subscription (Publish) |

**Execution order** (OpenAPI agent waits for corresponding MCP command to complete):

1. `mcp list` → then `openapi list`
2. `mcp create` + `mcp delete` → then `openapi create` + `openapi delete`
3. `mcp upload` → then `openapi upload`
4. `mcp validate` → then `openapi validate`
5. `mcp publish` → then `openapi publish`
