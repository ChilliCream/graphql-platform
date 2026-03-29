# Category Waves: Test Implementation Approach

## Overview

32 commands need test suites. This approach groups them into **category waves** -- batches of related commands within the same domain that share client interfaces, mock patterns, and test helper infrastructure. Each wave is a parallelizable unit: commands within a wave can be implemented by independent agents simultaneously because they share the same client interface and test helper.

---

## 1. Shared Infrastructure Needed Before Starting

### 1a. No New Shared Infrastructure Required

The existing test infrastructure is complete and battle-tested:

- **CommandBuilder** -- fluent test harness with `ExecuteAsync()` / `Start()` / `RunToCompletionAsync()`
- **InteractionMode** -- `Interactive`, `NonInteractive`, `JsonOutput`
- **CommandExecutionResultExtensions** -- `AssertSuccess()`, `AssertError()`, `AssertHelpOutput()`
- **InteractiveCommand** -- `Input()`, `SelectOption()`, `Confirm()`
- **Mock patterns** -- `MockBehavior.Strict` + `VerifyAll()` established convention

### 1b. Per-Category Test Helpers (Created During Each Wave)

Each category wave creates its own `*CommandTestHelper.cs` when mock payloads are reused 3+ times across the category's commands. Based on existing patterns (e.g., `ApiCommandTestHelper.cs`), helpers provide:

- Static factory methods for success payloads
- Static factory methods for error payloads
- `ConnectionPage<T>` builders for list commands

**Decision rule**: If a command category has 3+ commands sharing the same client interface, create the helper first as the wave's first task.

### 1c. Subscription Test Pattern (New)

The 10 subscription-based commands need a mock pattern for `IAsyncEnumerable<T>`. This is **not** a shared utility -- each command mocks its own `SubscribeTo*Async` method by returning a pre-built async enumerable. The pattern:

```csharp
// Mock the subscription method to yield a sequence of updates
client.Setup(x => x.SubscribeToSchemaValidationAsync(
        "request-id",
        It.IsAny<CancellationToken>()))
    .Returns(ToAsyncEnumerable(new IOnSchemaVersionValidationUpdated[] { successUpdate }));

// Helper (inline or in test helper)
static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
    IEnumerable<T> items,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    foreach (var item in items)
    {
        yield return item;
    }
}
```

**Strategy**: Add a `ToAsyncEnumerable<T>` helper method to the first subscription test helper created (likely `SchemaCommandTestHelper.cs` in Wave A) and make it `internal static` so other categories can reference it. Alternatively, each category can define its own inline version -- the pattern is trivial.

### 1d. File I/O Mock Pattern

Commands using `IFileSystem` (upload, download, validate) need stream mocking:

```csharp
var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
fileSystem.Setup(x => x.OpenReadStream("schema.graphql"))
    .Returns(new MemoryStream(Encoding.UTF8.GetBytes("type Query { hello: String }")));
```

This pattern already exists implicitly in the codebase (IFileSystem is injected and mockable). No new infrastructure needed.

---

## 2. Wave Grouping

Commands are grouped by **category** (shared client interface + domain). Within each wave, commands can be implemented in parallel by separate agents.

### Wave A: Schemas (4 commands) -- HIGHEST PRIORITY
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| schema download | `DownloadSchemaCommand` | File I/O (query + write) | 8-10 |
| schema upload | `UploadSchemaCommand` | File I/O + mutation | 12-15 |
| schema validate | `ValidateSchemaCommand` | Subscription | 15-18 |
| schema publish | `PublishSchemaCommand` | Subscription | 18-22 |

**Rationale**: Research notes these commands are **already aligned to COMMAND_IMPLEMENTATION_GUIDELINES.md** (typed mutation errors, auth assertions, activity fail/success semantics). They are ready for tests without implementation changes. This wave also establishes the subscription test pattern that other waves will follow.

**Shared helper**: `SchemaCommandTestHelper.cs` for mock payloads + `ToAsyncEnumerable` utility.

**Est. total**: ~53-65 tests

### Wave B: Workspaces (5 commands)
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| workspace create | `CreateWorkspaceCommand` | Mutation + prompts | 12-15 |
| workspace current | `CurrentWorkspaceCommand` | Query | 6-8 |
| workspace list | `ListWorkspaceCommand` | List pattern | 10-12 |
| workspace set-default | `SetDefaultWorkspaceCommand` | Mutation + interactive | 10-12 |
| workspace show | `ShowWorkspaceCommand` | Query | 8-10 |

**Rationale**: Standard CRUD commands. Well-understood patterns from API command reference. `CreateWorkspaceCommand` uses `PrintMutationErrorsAndExit` (legacy) -- **needs implementation update first** before tests.

**Note**: `CreateWorkspaceCommand` and `SetDefaultWorkspaceCommand` use `PrintMutationErrorsAndExit` which is banned. These commands need to be migrated to the typed switch pattern before testing. The migration doc says schema commands are already aligned, but workspace commands are **not**.

**Shared helper**: `WorkspaceCommandTestHelper.cs`

**Est. total**: ~46-57 tests

### Wave C: Stages (3 commands)
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| stage delete | `DeleteStageCommand` | Mutation | 12-15 |
| stage edit | `EditStagesCommand` | Complex interactive | 20-25 |
| stage list | `ListStagesCommand` | List pattern | 10-12 |

**Rationale**: Small category. `EditStagesCommand` is the most complex interactive command in the CLI -- has JSON config parsing, multi-step UI with `SelectableTable`, and state management. This is a high-risk command that benefits from focused attention.

**Shared helper**: `StageCommandTestHelper.cs`

**Est. total**: ~42-52 tests

### Wave D: PAT (3 commands) + Mock (3 commands)
These two small categories can run as a single wave with two parallel tracks.

**Track 1: PAT**
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| pat create | `CreatePersonalAccessTokenCommand` | Mutation | 12-15 |
| pat list | `ListPersonalAccessTokenCommand` | List pattern | 10-12 |
| pat revoke | `RevokePersonalAccessTokenCommand` | Mutation | 10-12 |

**Track 2: Mock**
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| mock create | `CreateMockCommand` | Mutation + file I/O | 15-18 |
| mock list | `ListMockCommand` | List pattern | 10-12 |
| mock update | `UpdateMockCommand` | Mutation + file I/O | 12-15 |

**Rationale**: Both are small, independent categories with standard patterns. PAT is straightforward mutation/list. Mock is hidden commands with file I/O but follows standard patterns. Pairing keeps wave utilization high.

**Note**: Mock commands use `PrintMutationErrorsAndExit` -- need implementation update. PAT commands (`CreatePersonalAccessTokenCommand`, `RevokePersonalAccessTokenCommand`) also use `PrintMutationErrorsAndExit` -- need implementation update.

**Shared helpers**: `PatCommandTestHelper.cs`, `MockCommandTestHelper.cs`

**Est. total**: ~69-84 tests

### Wave E: MCP (6 commands) + OpenAPI (6 commands)
These categories are **structurally identical** -- they mirror each other in command types, client interfaces, and error handling. They MUST run in the same wave so patterns established for one carry directly to the other.

**Track 1: MCP**
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| mcp create | `CreateMcpFeatureCollectionCommand` | Mutation | 12-15 |
| mcp delete | `DeleteMcpFeatureCollectionCommand` | Mutation | 10-12 |
| mcp list | `ListMcpFeatureCollectionCommand` | List pattern | 10-12 |
| mcp publish | `PublishMcpFeatureCollectionCommand` | Subscription | 18-22 |
| mcp upload | `UploadMcpFeatureCollectionCommand` | File upload | 12-15 |
| mcp validate | `ValidateMcpFeatureCollectionCommand` | Subscription | 15-18 |

**Track 2: OpenAPI**
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| openapi create | `CreateOpenApiCollectionCommand` | Mutation | 12-15 |
| openapi delete | `DeleteOpenApiCollectionCommand` | Mutation | 10-12 |
| openapi list | `ListOpenApiCollectionCommand` | List pattern | 10-12 |
| openapi publish | `PublishOpenApiCollectionCommand` | Subscription | 18-22 |
| openapi upload | `UploadOpenApiCollectionCommand` | File upload | 12-15 |
| openapi validate | `ValidateOpenApiCollectionCommand` | Subscription | 15-18 |

**Rationale**: Identical structure means the agent implementing MCP can be the template for OpenAPI (or vice versa). Both have subscription commands (validate, publish) that follow the exact same pattern as the schema subscription commands from Wave A.

**Note**: All MCP and OpenAPI commands extensively use `PrintMutationErrorsAndExit` -- these need implementation updates first.

**Shared helpers**: `McpCommandTestHelper.cs`, `OpenApiCommandTestHelper.cs`

**Est. total**: ~154-188 tests

### Wave F: Client Remaining (7 commands)
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| client download | `DownloadClientCommand` | File I/O + stream | 10-12 |
| client list-published-versions | `ListClientPublishedVersionsCommand` | List pattern | 10-12 |
| client list-versions | `ListClientVersionsCommand` | List pattern | 10-12 |
| client publish | `PublishClientCommand` | Subscription | 18-22 |
| client unpublish | `UnpublishClientCommand` | Mutation | 10-12 |
| client upload | `UploadClientCommand` | File upload | 12-15 |
| client validate | `ValidateClientCommand` | Subscription | 15-18 |

**Rationale**: These complete the client category. Existing `IClientsClient` mock patterns from completed client tests (create, delete, list, show) are already established. Two subscription commands follow patterns from Wave A.

**Note**: `ValidateClientCommand` and `PublishClientCommand` use `PrintMutationErrorsAndExit` + `PrintMutationErrors` -- need implementation update. `UnpublishClientCommand` uses `PrintMutationErrorsAndExit` -- needs update.

**Shared helper**: Extend existing client test infrastructure (or create `ClientCommandTestHelper.cs` if not already present).

**Est. total**: ~85-103 tests

### Wave G: Fusion Remaining (6 commands)
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| fusion download | `FusionDownloadCommand` | File I/O | 8-10 |
| fusion publish | `FusionPublishCommand` | Multi-step mutation | 15-18 |
| fusion run | `FusionRunCommand` | Process management | 8-12 |
| fusion settings set | `FusionSettingsSetCommand` | Simple mutation | 8-10 |
| fusion upload | `FusionUploadCommand` | File upload | 12-15 |
| fusion validate | `FusionValidateCommand` | Subscription (multi-phase) | 20-25 |

**Rationale**: Fusion commands are the most varied in complexity. `FusionValidateCommand` is unique because it does multi-phase validation (compose then validate each subgraph via subscription). `FusionRunCommand` spawns a local process, which is special.

**Shared helper**: `FusionCommandTestHelper.cs`

**Est. total**: ~71-90 tests

### Wave H: Fusion Publish Flow (5 commands)
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| fusion publish begin | `FusionConfigurationPublishBeginCommand` | Mutation | 10-12 |
| fusion publish start | `FusionConfigurationPublishStartCommand` | Mutation | 10-12 |
| fusion publish validate | `FusionConfigurationPublishValidateCommand` | Subscription | 15-18 |
| fusion publish commit | `FusionConfigurationPublishCommitCommand` | Mutation | 10-12 |
| fusion publish cancel | `FusionConfigurationPublishCancelCommand` | Mutation | 8-10 |

**Rationale**: These form a stateful multi-step flow (begin -> start -> validate -> commit/cancel). They share `IFusionConfigurationClient` and `FusionConfigurationPublishingState` file-based state. Testing them together ensures the state transitions are coherent.

**Note**: `FusionConfigurationPublishValidateCommand` has unique subscription behavior -- it throws `ExitException` for unexpected states (queued, already failed, already published) rather than returning error codes.

**Shared helper**: `FusionPublishCommandTestHelper.cs`

**Est. total**: ~53-64 tests

### Wave I: Standalone (3 commands) -- LOWEST PRIORITY
| Command | Class | Type | Est. Tests |
|---------|-------|------|-----------|
| launch | `LaunchCommand` | Browser open | 3-5 |
| login | `LoginCommand` | Auth flow | 8-12 |
| logout | `LogoutCommand` | Session clear | 5-7 |

**Rationale**: These are special-case commands that don't follow the standard mutation/list/subscription patterns. `LaunchCommand` calls `SystemBrowser.Open()` (may need mocking strategy). `LoginCommand` has browser-based auth flow with workspace selection delegation. These are deferred to last because they require unique approaches.

**Est. total**: ~16-24 tests

---

## 3. Parallelization Strategy

### Within a Wave: Full Parallelism
Every command within a wave can be implemented by an independent agent simultaneously. They share the same `*Client` interface for mocking but write to separate test files. The only serialization point is the test helper creation -- one agent creates the helper first, then the others reference it.

**Practical approach**: The first command in each wave creates the test helper class. Remaining commands in the wave can start immediately -- they create inline mocks initially and extract to the helper in a final cleanup pass.

### Across Waves: Sequential with Overlap

```
Phase 1:  [Wave A: Schemas]
Phase 2:  [Wave B: Workspaces] [Wave C: Stages]
Phase 3:  [Wave D: PAT + Mock]
Phase 4:  [Wave E: MCP + OpenAPI]
Phase 5:  [Wave F: Client Remaining] [Wave G: Fusion Remaining]
Phase 6:  [Wave H: Fusion Publish Flow]
Phase 7:  [Wave I: Standalone]
```

**Why this ordering**:
- **Phase 1**: Schemas first because they're already aligned (no implementation changes needed), and they establish the subscription test pattern.
- **Phase 2**: Workspaces and Stages are independent -- can run in parallel. Both are standard patterns.
- **Phase 3**: PAT + Mock are small, can be a single focused wave.
- **Phase 4**: MCP + OpenAPI are identical twins -- doing them together maximizes reuse.
- **Phase 5**: Client remaining and Fusion remaining are independent, can run in parallel.
- **Phase 6**: Fusion publish flow depends on understanding Fusion patterns from Wave G.
- **Phase 7**: Standalone commands are lowest priority, unique patterns.

### Implementation Updates (Pre-Requisite)
Commands using `PrintMutationErrorsAndExit` need implementation migration before tests can be written. This affects:
- **Wave B**: `CreateWorkspaceCommand`
- **Wave D**: `CreatePersonalAccessTokenCommand`, `RevokePersonalAccessTokenCommand`, `CreateMockCommand`, `UpdateMockCommand`
- **Wave E**: All MCP commands (create, delete, validate, publish), all OpenAPI commands (create, delete, validate, publish)
- **Wave F**: `ValidateClientCommand`, `PublishClientCommand`, `UnpublishClientCommand`
- **Wave G**: `FusionUploadCommand`, `FusionValidateCommand`
- **Wave H**: `FusionConfigurationPublishValidateCommand`

**Strategy**: Each wave's first task is to update any commands using `PrintMutationErrorsAndExit` to the typed switch pattern. The schema commands (Wave A) are already done, which is why they're first.

---

## 4. Subscription Command Handling

### The 10 Subscription Commands

| # | Command | Subscription Method | Complexity |
|---|---------|-------------------|------------|
| 1 | schema validate | `SubscribeToSchemaValidationAsync` | Medium |
| 2 | schema publish | `SubscribeToSchemaPublishAsync` | High (queue, approval, deployment) |
| 3 | client validate | `SubscribeToClientValidationAsync` | Medium |
| 4 | client publish | `SubscribeToClientPublishAsync` | High (queue, approval, deployment) |
| 5 | openapi validate | `SubscribeToOpenApiCollectionValidationAsync` | Medium |
| 6 | openapi publish | `SubscribeToOpenApiCollectionPublishAsync` | High (queue, approval, deployment) |
| 7 | mcp validate | `SubscribeToMcpFeatureCollectionValidationAsync` | Medium |
| 8 | mcp publish | `SubscribeToMcpFeatureCollectionPublishAsync` | High (queue, approval, deployment) |
| 9 | fusion validate | `SubscribeToSchemaVersionValidationUpdatedAsync` | High (multi-phase) |
| 10 | fusion publish validate | `SubscribeToFusionConfigurationPublishingTaskChangedAsync` | High (state machine) |

### Test Pattern for Subscription Commands

Each subscription command test suite must cover these scenarios (per the Subscription Command Test Checklist in research.md):

1. **Help output** -- standard
2. **Auth errors** -- standard Theory x 3 modes
3. **Initial mutation errors** -- typed switch branches (same as regular mutations)
4. **Subscription success path** -- mock subscription yields success update
5. **Subscription validation failure** -- mock subscription yields failure with errors
6. **Subscription in-progress states** -- verify activity.Update called
7. **Subscription queue states** (publish only) -- queue position shown
8. **Subscription approval states** (publish only) -- approval message + deployment errors
9. **Subscription unknown state** -- default handler with upgrade message
10. **Subscription ends without terminal state** -- fallthrough to activity.Fail() + ExitCodes.Error
11. **Client exception** -- standard Theory x 3 modes
12. **Authorization exception** -- standard Theory x 3 modes

### Mock Strategy for Subscriptions

Mock the subscription method to return a pre-built `IAsyncEnumerable<T>`:

```csharp
// Success scenario
client.Setup(x => x.SubscribeToSchemaValidationAsync("req-1", It.IsAny<CancellationToken>()))
    .Returns(ToAsyncEnumerable<IOnSchemaVersionValidationUpdated>(
        new ISchemaVersionValidationSuccess[] { successMock.Object }));

// Failure scenario
client.Setup(x => x.SubscribeToSchemaValidationAsync("req-1", It.IsAny<CancellationToken>()))
    .Returns(ToAsyncEnumerable<IOnSchemaVersionValidationUpdated>(
        new ISchemaVersionValidationFailed[] { failureMock.Object }));

// Multi-step scenario (in-progress then success)
client.Setup(x => x.SubscribeToSchemaValidationAsync("req-1", It.IsAny<CancellationToken>()))
    .Returns(ToAsyncEnumerable<IOnSchemaVersionValidationUpdated>(new object[]
    {
        inProgressMock.Object,
        successMock.Object
    }));

// Empty subscription (no terminal state)
client.Setup(x => x.SubscribeToSchemaValidationAsync("req-1", It.IsAny<CancellationToken>()))
    .Returns(AsyncEnumerable.Empty<IOnSchemaVersionValidationUpdated>());
```

### Validate vs Publish Command Differences

**Validate commands** (5): simpler state machine
- States: InProgress, ValidationInProgress, Success, Failed
- No queue, approval, or deployment states
- Est. ~15-18 tests each

**Publish commands** (5): complex state machine
- States: Queued, InProgress, Ready, Approved, WaitForApproval, Success, Failed
- Deployment errors in approval state
- Est. ~18-22 tests each

---

## 5. Execution Order -- Concrete Phases

### Phase 1: Schema Commands (Wave A)
**Duration**: Single focused wave
**Parallelism**: 4 agents (one per command)

**Tasks**:
1. Create `SchemaCommandTestHelper.cs` with `ToAsyncEnumerable<T>` utility
2. Implement `DownloadSchemaCommandTests.cs` (simplest -- establishes file I/O pattern)
3. Implement `UploadSchemaCommandTests.cs` (mutation + file I/O)
4. Implement `ValidateSchemaCommandTests.cs` (first subscription test -- establishes pattern)
5. Implement `PublishSchemaCommandTests.cs` (complex subscription -- queue, approval)

**Why first**: Schema commands are already aligned to guidelines. No implementation changes needed. Establishes subscription test pattern for all future waves.

### Phase 2: Workspaces + Stages (Waves B + C)
**Duration**: Two parallel tracks
**Parallelism**: Up to 8 agents (5 workspace + 3 stage)

**Pre-requisite**: Migrate `CreateWorkspaceCommand` away from `PrintMutationErrorsAndExit`. Similarly check and update `EditStagesCommand` and `DeleteStageCommand`.

**Tasks Track 1 (Workspace)**:
1. Update command implementations
2. Create `WorkspaceCommandTestHelper.cs`
3. Implement 5 test suites

**Tasks Track 2 (Stages)**:
1. Update command implementations
2. Create `StageCommandTestHelper.cs`
3. Implement 3 test suites (note: `EditStagesCommand` is high complexity)

### Phase 3: PAT + Mock (Wave D)
**Duration**: Two parallel tracks
**Parallelism**: Up to 6 agents

**Pre-requisite**: Migrate PAT and Mock commands away from `PrintMutationErrorsAndExit`.

### Phase 4: MCP + OpenAPI (Wave E)
**Duration**: Two parallel tracks
**Parallelism**: Up to 12 agents (6 MCP + 6 OpenAPI)

**Pre-requisite**: Migrate all MCP and OpenAPI commands away from `PrintMutationErrorsAndExit`. This is the largest migration batch.

**Strategy**: Implement MCP first (it's the template), then copy patterns to OpenAPI.

### Phase 5: Client Remaining + Fusion Remaining (Waves F + G)
**Duration**: Two parallel tracks
**Parallelism**: Up to 13 agents (7 client + 6 fusion)

**Pre-requisite**: Migrate remaining client and fusion commands away from `PrintMutationErrorsAndExit`.

### Phase 6: Fusion Publish Flow (Wave H)
**Duration**: Single focused wave
**Parallelism**: 5 agents (one per command)

**Pre-requisite**: Fusion remaining (Wave G) must complete first to establish fusion test patterns.

### Phase 7: Standalone (Wave I)
**Duration**: Single focused wave
**Parallelism**: 3 agents

**Special handling**:
- `LaunchCommand`: Mock `SystemBrowser.Open()` or the underlying process launch
- `LoginCommand`: Mock browser auth flow and `ISessionService.LoginAsync()`
- `LogoutCommand`: Mock `ISessionService.LogoutAsync()`

---

## 6. Estimated Test Counts

| Wave | Category | Commands | Est. Tests (Low) | Est. Tests (High) |
|------|----------|----------|------------------|-------------------|
| A | Schema | 4 | 53 | 65 |
| B | Workspace | 5 | 46 | 57 |
| C | Stage | 3 | 42 | 52 |
| D | PAT + Mock | 6 | 69 | 84 |
| E | MCP + OpenAPI | 12 | 154 | 188 |
| F | Client Remaining | 7 | 85 | 103 |
| G | Fusion Remaining | 6 | 71 | 90 |
| H | Fusion Publish Flow | 5 | 53 | 64 |
| I | Standalone | 3 | 16 | 24 |
| **Total** | | **51** | **589** | **727** |

Note: 32 new command suites + existing 17 done + parent/group commands = 49 leaf commands total. The count discrepancy (51 vs 32) is because Wave F/G include some overlap with existing done commands -- only the "not-started" commands within those categories need new suites.

**Adjusted for 32 new suites only**: ~500-620 new test methods.

---

## 7. Risk Areas

### Highest Risk

1. **`EditStagesCommand`** (Wave C)
   - **Why**: Most complex interactive command. JSON config parsing, multi-step `SelectableTable` UI, in-memory state management (add/delete/edit stages). Has local record types and file-scoped static extension classes.
   - **Mitigation**: Dedicate a senior agent. Break into sub-tasks: JSON config path tests, interactive UI tests, error handling tests. May need 20+ test methods.

2. **`FusionValidateCommand`** (Wave G)
   - **Why**: Multi-phase validation. Composes source schemas first, then validates each subgraph via subscription. Unique two-level activity tracking. Sets `isValid` boolean rather than returning exit code directly from subscription.
   - **Mitigation**: Study the full command implementation carefully. Test each phase independently.

3. **`FusionConfigurationPublishValidateCommand`** (Wave H)
   - **Why**: Throws `ExitException` for unexpected states (queued, already failed, already published) rather than returning error codes. Unique among subscription commands.
   - **Mitigation**: Test each `ExitException` path explicitly.

### Medium Risk

4. **Subscription commands needing implementation migration** (Waves E, F)
   - **Why**: Commands using `PrintMutationErrorsAndExit` and `PrintMutationErrors` (to stdout) need migration to typed switch + activity.Fail() + stderr. 10 commands affected.
   - **Mitigation**: Phase the migration: update implementation first, verify it compiles, then write tests.

5. **`FusionRunCommand`** (Wave G)
   - **Why**: Process management command. Spawns a local Fusion gateway process. May require mocking process creation and lifecycle.
   - **Mitigation**: Inspect the actual implementation to determine what's mockable. May need `IProcessFactory` or similar abstraction.

6. **`LoginCommand`** (Wave I)
   - **Why**: Browser-based authentication flow. Delegates to `SetDefaultWorkspaceCommand.ExecuteAsync()` for workspace selection. Complex integration with `ISessionService.LoginAsync()`.
   - **Mitigation**: Mock the session service and workspace client. Focus on the command-level flow, not the browser interaction.

7. **`CreateWorkspaceCommand`** (Wave B)
   - **Why**: Uses `PrintMutationErrorsAndExit` (legacy). Also has conditional `OptionOrConfirmAsync` for setting default workspace only when interactive + session exists. The dual-path logic adds test surface.
   - **Mitigation**: Migrate implementation first. Cover both paths (with/without session, with/without confirmation).

### Lower Risk

8. **Archive-based upload commands** (MCP upload, OpenAPI upload, Fusion upload)
   - **Why**: These create archive streams from directories before uploading. Need to mock `IFileSystem` directory operations.
   - **Mitigation**: Check whether the archive creation is part of the command or the client. If client-side, the command just passes a stream.

9. **Download commands** (schema, client, fusion)
   - **Why**: Write files to disk via `IFileSystem`. Need to mock file creation and stream writing.
   - **Mitigation**: Standard `IFileSystem` mock pattern. Return `MemoryStream` from client, verify it's written.

10. **`LaunchCommand`** (Wave I)
    - **Why**: Calls `SystemBrowser.Open()` which is a static utility. May not be mockable without abstraction.
    - **Mitigation**: Check if `SystemBrowser` is injectable or if the test needs a different approach. Worst case: test the command parsing and argument handling, mark the browser-open as integration-only.

---

## Summary

The Category Waves approach:
- **9 waves** covering 32 commands (~500-620 new tests)
- **Phases 1-7** for sequential ordering with parallelism within phases
- **Schema commands first** (already aligned, establishes patterns)
- **Subscription pattern** established once in Wave A, reused in Waves E, F, G, H
- **Implementation migration** required for ~20 commands using `PrintMutationErrorsAndExit`
- **Highest risk**: `EditStagesCommand`, `FusionValidateCommand`, `FusionConfigurationPublishValidateCommand`
- **MCP + OpenAPI** are structural twins -- implementing one gives you the template for the other
