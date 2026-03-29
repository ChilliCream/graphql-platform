# Approach: Pattern Buckets

## Core Idea

Group the 32 remaining commands by **behavioral pattern** rather than by domain category. All "list" commands share the same test skeleton regardless of whether they list APIs, clients, or workspaces. All "create" commands share the same mutation error shape. By organizing work around these patterns, agents can specialize in one pattern, build muscle memory, and produce consistent output.

---

## 1. Pattern Taxonomy

### Bucket A: List Commands (8 commands)

Commands that query a paginated collection with cursor support.

| Command | Class | Scoping | Notes |
|---------|-------|---------|-------|
| client list-versions | `ListClientVersionsCommand` | api-id + client-id | Nested list |
| client list-published-versions | `ListClientPublishedVersionsCommand` | api-id + client-id | Nested list |
| mcp list | `ListMcpFeatureCollectionCommand` | api-id | Standard |
| openapi list | `ListOpenApiCollectionCommand` | api-id | Standard |
| mock list | `ListMockCommand` | api-id | Hidden command |
| pat list | `ListPersonalAccessTokenCommand` | session | No workspace scope |
| stage list | `ListStagesCommand` | api-id | Standard |
| workspace list | `ListWorkspaceCommand` | session | No api scope |

**Canonical test set per command (~10-14 tests):**
1. Help snapshot
2. No auth (Theory x3)
3. No workspace/missing scoping option (Theory x2-3)
4. Success with scoping option (Interactive)
5. Success with session scope (NonInteractive + JsonOutput shared Theory)
6. Success with cursor (NonInteractive + JsonOutput shared Theory)
7. Empty results (Interactive)
8. Empty results (NonInteractive + JsonOutput shared Theory)
9. Client exception (Theory x3)
10. Client authorization exception (Theory x3)

**Reference implementation:** `ListApiCommandTests`, `ListClientCommandTests`

---

### Bucket B: Create / Simple Mutation Commands (9 commands)

Commands that call a single mutation, return a result, and have typed error branches.

| Command | Class | Required Options | Notes |
|---------|-------|-----------------|-------|
| workspace create | `CreateWorkspaceCommand` | name | Simple |
| workspace set-default | `SetDefaultWorkspaceCommand` | workspace-id | Simple mutation |
| pat create | `CreatePersonalAccessTokenCommand` | name + scope | Has expiry |
| pat revoke | `RevokePersonalAccessTokenCommand` | id | Delete-style mutation |
| mcp create | `CreateMcpFeatureCollectionCommand` | api-id + name | Standard |
| openapi create | `CreateOpenApiCollectionCommand` | api-id + name | Standard |
| stage delete | `DeleteStageCommand` | id | Delete with confirmation |
| mcp delete | `DeleteMcpFeatureCollectionCommand` | id | Delete |
| openapi delete | `DeleteOpenApiCollectionCommand` | id | Delete |

**Canonical test set per command (~15-25 tests):**
1. Help snapshot
2. No auth (Theory x3)
3. No workspace/missing scoping option (Theory x2-3)
4. Missing required option (NonInteractive + JsonOutput)
5. Missing required option with prompting (Interactive) -- if applicable
6. Success with all options (NonInteractive)
7. Success (JsonOutput)
8. Success (Interactive) -- if different from NI
9. Mutation returns no result (Theory x3 or per-mode)
10. Each typed mutation error branch x3 modes (Fact per mode, or MemberData Theory)
11. Client exception (Theory x3 or per-mode Facts)
12. Client authorization exception (Theory x3 or per-mode Facts)

**Sub-patterns within this bucket:**
- **Create** commands: `CreateApiCommandTests`, `CreateClientCommandTests` (interactive prompting, workspace scoping)
- **Delete** commands: `DeleteApiCommandTests`, `DeleteClientCommandTests` (confirmation prompt, force flag, not-found check)
- **Update/Set** commands: `SetApiSettingsCommandTests` (no prompting, all options required)

**Reference implementations:** `CreateApiCommandTests`, `DeleteApiCommandTests`, `CreateClientCommandTests`

---

### Bucket C: Show / Query Commands (3 commands)

Commands that fetch a single resource by ID with no mutation.

| Command | Class | Argument | Notes |
|---------|-------|----------|-------|
| workspace show | `ShowWorkspaceCommand` | workspace-id | Standard |
| workspace current | `CurrentWorkspaceCommand` | (none) | Uses session |
| mock update | `UpdateMockCommand` | id + files | Mutation + file I/O (hybrid) |

**Canonical test set per command (~8-12 tests):**
1. Help snapshot
2. No auth (Theory x3)
3. Resource not found (Theory x3)
4. Success (Theory x3 -- output identical across modes)
5. Client exception (Theory x3)
6. Client authorization exception (Theory x3)

**Reference implementations:** `ShowApiCommandTests`, `ShowClientCommandTests`

**Note:** `workspace current` is special -- it reads from session, not from API. `mock update` is a hybrid (mutation + file I/O) but its test shape is closer to show/update than to subscription commands.

---

### Bucket D: Upload Commands (5 commands)

Commands that read a file from disk and upload it via a mutation. No subscription involved -- the upload itself is the mutation.

| Command | Class | File Option | Notes |
|---------|-------|-------------|-------|
| schema upload | `UploadSchemaCommand` | --schema-file | Stream upload + metadata |
| client upload | `UploadClientCommand` | --operations-file | Stream upload |
| fusion upload | `FusionUploadCommand` | --schema-file | Archive upload |
| mcp upload | `UploadMcpFeatureCollectionCommand` | --file | Archive upload |
| openapi upload | `UploadOpenApiCollectionCommand` | --file | Archive upload |

**Canonical test set per command (~15-20 tests):**
1. Help snapshot
2. No auth (Theory x3)
3. Missing scoping option (Theory x2-3)
4. Missing file option (NonInteractive + JsonOutput)
5. File not found / unreadable (Theory x3)
6. Success (NonInteractive)
7. Success (JsonOutput)
8. Each typed mutation error branch x3 modes
9. Client exception (Theory x3)
10. Client authorization exception (Theory x3)

**Key difference from Bucket B:** These commands require `IFileSystem` mock setup and stream mocking. The file-read pattern is consistent across all upload commands.

**Reference implementation:** None completed yet. Schema upload is the simplest starting point.

---

### Bucket E: Download Commands (3 commands)

Commands that fetch data from the API and write to a file or stdout.

| Command | Class | Output | Notes |
|---------|-------|--------|-------|
| schema download | `DownloadSchemaCommand` | File or stdout | Stream download |
| client download | `DownloadClientCommand` | File | JSON stream |
| fusion download | `FusionDownloadCommand` | File | Archive download |

**Canonical test set per command (~12-16 tests):**
1. Help snapshot
2. No auth (Theory x3)
3. Missing scoping option (Theory x2-3)
4. Success -- write to file (NonInteractive)
5. Success -- write to stdout (if supported)
6. Success (JsonOutput)
7. Resource not found (Theory x3)
8. Client exception (Theory x3)
9. Client authorization exception (Theory x3)

**Key difference:** These mock `IFileSystem.OpenWriteStream()` instead of `OpenReadStream()`.

---

### Bucket F: Subscription Commands -- Validate (5 commands)

Commands that start a validation via mutation, then subscribe to status updates. These are the most complex non-publish commands.

| Command | Class | Subscription Method | Current State |
|---------|-------|-------------------|---------------|
| schema validate | `ValidateSchemaCommand` | `SubscribeToSchemaValidationAsync` | Partially migrated |
| client validate | `ValidateClientCommand` | `SubscribeToClientValidationAsync` | Uses PrintMutationErrorsAndExit |
| openapi validate | `ValidateOpenApiCollectionCommand` | `SubscribeToOpenApiCollectionValidationAsync` | Uses PrintMutationErrorsAndExit |
| mcp validate | `ValidateMcpFeatureCollectionCommand` | `SubscribeToMcpFeatureCollectionValidationAsync` | Uses PrintMutationErrorsAndExit |
| fusion validate | `FusionValidateCommand` | `SubscribeToSchemaVersionValidationUpdatedAsync` | Uses PrintMutationErrorsAndExit |

**Canonical test set per command (~20-28 tests):**

**Phase 1: Initial mutation tests (same as Bucket B)**
1. Help snapshot
2. No auth (Theory x3)
3. Missing scoping option (Theory x2-3)
4. Missing file option (NonInteractive + JsonOutput)
5. Each initial mutation error branch x3 modes
6. Null request ID (Theory x3)

**Phase 2: Subscription lifecycle tests**
7. Subscription success path (x3 modes)
8. Subscription validation failure with rich errors (x3 modes)
9. Subscription in-progress states -- activity.Update called
10. Subscription unknown state -- default handler with upgrade message
11. Subscription ends without terminal state -- fallthrough to activity.Fail() + ExitCodes.Error

**Phase 3: Activity lifecycle**
12. Every path ends in activity.Fail() or activity.Success()

**Key infrastructure needed:**
- Mock `IAsyncEnumerable<T>` factory that yields a sequence of subscription events
- Helper to create typed validation failure events with nested error objects
- Helper to create rich error types (`IInvalidGraphQLSchemaError`, `IOpenApiCollectionValidationError`, etc.)

---

### Bucket G: Subscription Commands -- Publish (5 commands)

Commands that start a publish via mutation, then subscribe to status updates. Superset of validate pattern -- adds queue, approval, and deployment states.

| Command | Class | Subscription Method | Current State |
|---------|-------|-------------------|---------------|
| schema publish | `PublishSchemaCommand` | `SubscribeToSchemaPublishAsync` | Partially migrated |
| client publish | `PublishClientCommand` | `SubscribeToClientPublishAsync` | Uses PrintMutationErrorsAndExit |
| openapi publish | `PublishOpenApiCollectionCommand` | `SubscribeToOpenApiCollectionPublishAsync` | Uses PrintMutationErrorsAndExit |
| mcp publish | `PublishMcpFeatureCollectionCommand` | `SubscribeToMcpFeatureCollectionPublishAsync` | Uses PrintMutationErrorsAndExit |
| fusion publish validate | `FusionConfigurationPublishValidateCommand` | `SubscribeToFusionConfigurationPublishingTaskChangedAsync` | Uses PrintMutationErrorsAndExit |

**Canonical test set per command (~25-35 tests):**

**Phase 1: Initial mutation tests (same as Bucket B/F)**
1-6. Same as Bucket F

**Phase 2: Subscription lifecycle tests**
7. Subscription success path (x3 modes)
8. Subscription publish failure with rich errors (x3 modes)
9. Queue position state -- activity.Update with position
10. In-progress state -- activity.Update
11. Ready state -- console.Success
12. Wait-for-approval state -- deployment errors rendered + activity.Update
13. Approved state -- activity.Update
14. Unknown state -- default handler
15. Subscription ends without terminal state -- fallthrough

**Phase 3: Additional publish-specific**
16. Force option behavior
17. Wait-for-approval option behavior

**Key infrastructure needed:**
- Same as Bucket F, plus:
- Mock factory for publish-specific subscription events (queued, ready, approval, approved)
- Mock factory for deployment objects with nested errors

---

### Bucket H: Special Commands (4 commands)

Commands with unique patterns that don't fit neatly into the above buckets.

| Command | Class | Pattern | Notes |
|---------|-------|---------|-------|
| stage edit | `EditStagesCommand` | Complex interactive | Multi-step UI with SelectableTable |
| fusion run | `FusionRunCommand` | Process management | Spawns child process |
| fusion settings set | `FusionSettingsSetCommand` | Simple mutation | Could fit Bucket B but Fusion-specific |
| fusion publish (begin/start/commit/cancel) | 4 classes | Multi-step stateful flow | Sequential publish pipeline |

**Approach:** Handle these individually. Each gets a bespoke test plan rather than a template.

---

## 2. Shared Test Infrastructure Per Pattern

### Universal (all buckets)

Already exists:
- `CommandBuilder` (fluent test harness)
- `InteractionMode` enum
- `AssertSuccess()`, `AssertError()`, `AssertHelpOutput()`
- `InteractiveCommand` for interactive testing

### Bucket A (List) -- needs

- **Per-command `CreateListPage()` factory**: Returns `ConnectionPage<T>` with test data. Pattern established by `ApiCommandTestHelper.CreateListApisPage()`.
- No new shared infrastructure needed -- the pattern is already well-established.

### Bucket B (Create/Mutation) -- needs

- **Per-command payload factory methods**: `CreateSuccessPayload()`, `CreatePayloadWithErrors()`, `CreatePayloadWithNoResult()`. Pattern established by `CreateApiCommandTests` private methods.
- **Per-command `MutationErrorCases` TheoryData**: `TheoryData<TError, string>` for parametric error testing. Pattern established by `DeleteApiCommandTests.DeleteApiMutationErrorCases`.

### Bucket C (Show/Query) -- needs

- **Per-command `CreateShowNode()` factory**: Returns the query result node. Pattern established by `ApiCommandTestHelper.CreateShowApiNode()`.
- Minimal infrastructure -- these are the simplest tests.

### Bucket D (Upload) -- new infrastructure

- **`MockFileSystem` helper or extension**: Standardized setup for `IFileSystem.OpenReadStream()` that returns a `MemoryStream` with test content.
- Could be a simple extension method: `builder.AddFileSystem("test-content")` or inline setup per test.

### Bucket E (Download) -- new infrastructure

- **`MockFileSystem` write helper**: Setup for `IFileSystem.OpenWriteStream()` that captures written bytes.
- Could capture to `MemoryStream` and assert content.

### Bucket F & G (Subscription) -- new infrastructure needed

This is the biggest gap. Currently no tests exist for subscription commands.

**Required:**

1. **`AsyncEnumerableFactory<T>`** or equivalent: Creates `IAsyncEnumerable<T>` from a list of events for mocking subscription methods.
   ```csharp
   client.Setup(x => x.SubscribeToSchemaValidationAsync(requestId, It.IsAny<CancellationToken>()))
       .Returns(AsyncEnumerable.Of(
           new SchemaVersionValidationInProgress(),
           new SchemaVersionValidationSuccess()));
   ```

2. **Subscription event factories per command**: Create typed events like `ISchemaVersionValidationFailed`, `ISchemaVersionValidationSuccess`, `IProcessingTaskIsQueued`, etc.

3. **Rich error factories**: Create `IInvalidGraphQLSchemaError`, `IOpenApiCollectionValidationError`, `IMcpFeatureCollectionValidationError` etc. with nested structure for `PrintMutationError` testing.

---

## 3. Parallelization Strategy

### Independent buckets that can run simultaneously

```
Phase 1 (concurrent):
  Agent-1: Bucket A (List) -- 8 commands
  Agent-2: Bucket B (Create/Mutation) -- 9 commands
  Agent-3: Bucket C (Show/Query) -- 3 commands

Phase 2 (concurrent, starts after Phase 1 infrastructure):
  Agent-4: Bucket D (Upload) -- 5 commands
  Agent-5: Bucket E (Download) -- 3 commands

Phase 3 (concurrent, requires subscription infrastructure):
  Agent-6: Bucket F (Validate subscriptions) -- 5 commands
  Agent-7: Bucket G (Publish subscriptions) -- 5 commands

Phase 4 (sequential, bespoke):
  Agent-8: Bucket H (Special) -- 4 commands
```

**Why this ordering:**
- Phase 1 buckets use only existing infrastructure (CommandBuilder, Moq, snapshots)
- Phase 2 buckets need IFileSystem mocking -- small infrastructure addition
- Phase 3 buckets need subscription mocking -- largest infrastructure addition, plus the commands themselves may need migration from `PrintMutationErrorsAndExit` to the target pattern
- Phase 4 commands are bespoke and benefit from all prior patterns being established

**Within each bucket**, commands are independent and could be parallelized further (one agent per command), but the overhead of spinning up agents likely makes 2-3 commands per agent more efficient.

---

## 4. Template Approach

Each bucket should have a **concrete test template** that agents copy and adapt. The template is not a code generator -- it's a real, working test file that demonstrates every required test case for that pattern.

### Template structure per bucket

```
.work/templates/
  bucket-a-list-template.cs       -- based on ListApiCommandTests
  bucket-b-create-template.cs     -- based on CreateApiCommandTests
  bucket-b-delete-template.cs     -- based on DeleteApiCommandTests
  bucket-c-show-template.cs       -- based on ShowApiCommandTests
  bucket-d-upload-template.cs     -- to be created from first upload test
  bucket-e-download-template.cs   -- to be created from first download test
  bucket-f-validate-template.cs   -- to be created from first subscription validate test
  bucket-g-publish-template.cs    -- to be created from first subscription publish test
```

**Template content:** Each template file is a complete, commented test class with placeholder comments like:

```csharp
// ADAPT: Replace IApisClient with your command's client interface
// ADAPT: Replace "api" "list" with your command's argument path
// ADAPT: Replace CreateListApisPage with your command's page factory
```

**Agent instructions:** "Read the template for your bucket. Read the command implementation. Copy the template, replace all ADAPT markers with command-specific values. Run tests. Fix snapshot values."

### Why templates work here

The test patterns are **extremely repetitive**. The difference between `ListApiCommandTests` and `ListMcpFeatureCollectionCommand` tests is:
- Client interface name
- Argument path ("api list" vs "mcp list")
- Scoping option (workspace-id vs api-id)
- Payload factory types
- Snapshot values

Everything else is structural boilerplate. Templates eliminate the risk of agents missing mandatory test cases.

---

## 5. Subscription Bucket (Deep Dive)

The 10 subscription commands are split into Bucket F (validate) and Bucket G (publish) because publish is a strict superset of validate.

### Pre-work: Command Migration

Before tests can be written, the subscription commands may need migration:
- 8 of 10 use `PrintMutationErrorsAndExit` (banned pattern)
- 2 (`ValidateSchemaCommand`, `PublishSchemaCommand`) are partially migrated
- All need `activity.Fail()` / `activity.Success()` on every path

**Recommendation:** Migrate commands first, then write tests. Alternatively, write tests against current behavior and use test failures to drive the migration.

### Subscription Mock Infrastructure

```csharp
// Helper to create IAsyncEnumerable from events
public static class SubscriptionTestHelper
{
    public static IAsyncEnumerable<T> CreateSubscription<T>(params T[] events)
    {
        return events.ToAsyncEnumerable();
    }

    // Or if no LINQ async dependency:
    public static async IAsyncEnumerable<T> YieldEvents<T>(params T[] events)
    {
        foreach (var e in events)
        {
            yield return e;
        }
    }
}
```

### Test matrix for a validate command (e.g., ValidateSchemaCommand)

| Test | Mutation | Subscription Events | Expected |
|------|----------|-------------------|----------|
| Help | - | - | Help snapshot |
| No auth | - | - | Auth error |
| Missing file | - | - | Missing option error |
| Mutation error: UnauthorizedOperation | Error | - | stderr error message |
| Mutation error: ApiNotFound | Error | - | stderr error message |
| Mutation error: StageNotFound | Error | - | stderr error message |
| Mutation error: SchemaNotFound | Error | - | stderr error message |
| Mutation error: Generic | Error | - | "Unexpected mutation error: ..." |
| Null request ID | Success but null ID | - | ExitException |
| Validation success | Success | [InProgress, Success] | activity.Success |
| Validation failure (rich errors) | Success | [InProgress, Failed{errors}] | PrintMutationError + stderr summary |
| Validation in-progress | Success | [InProgress] then stream ends | activity.Fail, ExitCodes.Error |
| Unknown subscription event | Success | [UnknownEvent] then stream ends | upgrade message + ExitCodes.Error |
| Client exception | Throws | - | Error message |
| Auth exception | Throws | - | Unauthorized message |

### Test matrix for a publish command (adds to validate matrix)

| Additional Test | Subscription Events | Expected |
|-----------------|-------------------|----------|
| Queue position | [Queued{pos:3}] then stream ends | activity.Update with position |
| Ready state | [Ready] then stream ends | console.Success |
| Approval wait with deployment errors | [WaitForApproval{deployment}] | PrintMutationErrors + activity.Update |
| Approved state | [Approved] then stream ends | activity.Update |
| Force option | - | console.Log("[yellow]Force push is enabled[/]") |

---

## 6. Execution Order (Concrete Phases)

### Phase 0: Infrastructure (1 agent, ~30 min)
- Create `SubscriptionTestHelper` for async enumerable mocking
- Create `FileSystemTestHelper` for file I/O mocking
- Verify existing `CommandBuilder` infrastructure works for all patterns
- Create template files in `.work/templates/`

### Phase 1: Simple Patterns (3 agents in parallel)

**Agent 1 -- Bucket A (List): 8 commands**
1. `ListWorkspaceCommand` -- simplest (session-scoped, no api-id)
2. `ListPersonalAccessTokenCommand` -- session-scoped
3. `ListStagesCommand` -- api-id scoped
4. `ListMcpFeatureCollectionCommand` -- api-id scoped
5. `ListOpenApiCollectionCommand` -- api-id scoped (near-identical to MCP)
6. `ListMockCommand` -- hidden command
7. `ListClientVersionsCommand` -- nested (client-id)
8. `ListClientPublishedVersionsCommand` -- nested (client-id)

**Agent 2 -- Bucket B (Create/Mutation): 9 commands**
1. `CreateWorkspaceCommand` -- simplest create
2. `CreateMcpFeatureCollectionCommand` -- standard create
3. `CreateOpenApiCollectionCommand` -- standard create (parallel to MCP)
4. `SetDefaultWorkspaceCommand` -- update mutation
5. `CreatePersonalAccessTokenCommand` -- create with expiry
6. `RevokePersonalAccessTokenCommand` -- delete-style
7. `DeleteStageCommand` -- delete with confirmation
8. `DeleteMcpFeatureCollectionCommand` -- delete
9. `DeleteOpenApiCollectionCommand` -- delete

**Agent 3 -- Bucket C (Show/Query): 3 commands**
1. `ShowWorkspaceCommand`
2. `CurrentWorkspaceCommand`
3. `UpdateMockCommand` (hybrid but closest to show pattern)

### Phase 2: File I/O Patterns (2 agents in parallel)

**Agent 4 -- Bucket D (Upload): 5 commands**
1. `UploadSchemaCommand` -- simplest (single file stream)
2. `UploadClientCommand` -- single file stream
3. `UploadMcpFeatureCollectionCommand` -- archive upload
4. `UploadOpenApiCollectionCommand` -- archive upload
5. `FusionUploadCommand` -- archive upload

**Agent 5 -- Bucket E (Download): 3 commands**
1. `DownloadSchemaCommand` -- stream to file
2. `DownloadClientCommand` -- JSON stream to file
3. `FusionDownloadCommand` -- archive download

### Phase 3: Subscription Patterns (2 agents in parallel)

**Agent 6 -- Bucket F (Validate subscriptions): 5 commands**
1. `ValidateSchemaCommand` -- already partially migrated, start here
2. `ValidateClientCommand`
3. `ValidateOpenApiCollectionCommand`
4. `ValidateMcpFeatureCollectionCommand`
5. `FusionValidateCommand`

**Agent 7 -- Bucket G (Publish subscriptions): 5 commands**
1. `PublishSchemaCommand` -- already partially migrated, start here
2. `PublishClientCommand`
3. `PublishOpenApiCollectionCommand`
4. `PublishMcpFeatureCollectionCommand`
5. `FusionConfigurationPublishValidateCommand`

### Phase 4: Special Commands (1-2 agents, sequential)

**Agent 8 -- Bucket H: 4 commands**
1. `FusionSettingsSetCommand` -- simple mutation, do first
2. `EditStagesCommand` -- complex interactive
3. `FusionRunCommand` -- process management
4. Fusion publish flow (begin/start/commit/cancel) -- stateful multi-step

---

## 7. Pros and Cons

### Pros (vs. category-based grouping)

| Advantage | Explanation |
|-----------|-------------|
| **Agent specialization** | An agent working on Bucket A (lists) develops deep expertise in the list pattern. By command 3, it's producing consistent, high-quality tests with minimal false starts. |
| **Template reuse** | A template for "list command tests" applies to ALL 8 list commands. A category-based approach means each category needs its own mini-templates for list, create, delete, show, etc. |
| **Consistent coverage** | Harder to miss mandatory test cases when all list commands are done by the same agent using the same checklist. |
| **Better parallelization** | Buckets are naturally independent (no shared state between list tests and create tests). Category-based grouping creates artificial dependencies (e.g., MCP create tests might depend on MCP list helpers). |
| **Infrastructure focus** | Subscription infrastructure (Buckets F+G) is built once, used for all 10 commands. In category-based, each category team would independently solve the same subscription mocking problem. |
| **Deferred complexity** | Simple patterns (A, B, C) execute first while subscription infrastructure is built. Category-based forces each category to tackle its hardest commands (validate/publish) alongside easy ones. |
| **Clear phase boundaries** | Each phase has a clear "done" criteria. Phase 1 is done when all list, create, and show commands have passing tests. No ambiguity about what "MCP is 60% done" means. |

### Cons (vs. category-based grouping)

| Disadvantage | Explanation | Mitigation |
|--------------|-------------|------------|
| **Cross-category context switching** | An agent writing list tests jumps between MCP, OpenAPI, PAT, etc. It may need to understand each command's domain briefly. | Templates minimize domain knowledge needed. The agent reads the command implementation, fills in the template. |
| **Helper fragmentation** | Test helpers for MCP are split across buckets (list helper in Bucket A agent, create helper in Bucket B agent). | Each test file should be self-contained with its own private helpers. Shared helpers (`*CommandTestHelper.cs`) are only created when reuse is proven (3+ tests using same payload). |
| **Review complexity** | A reviewer wanting to see "all MCP tests" must look at files created by different agents. | Directory structure is still category-based (`Commands/Mcp/`). The grouping is only for agent assignment, not file organization. |
| **Subscription commands may need migration** | Testing subscription commands may reveal that the command itself needs fixing. The test agent for Bucket F/G may need to modify command source code, not just write tests. | Accept this. Phase 3 agents have dual mandate: fix commands to match target pattern AND write tests. This is actually an advantage -- the agent sees all subscription commands and can apply consistent fixes. |
| **Some commands are hybrid** | `mock update` is a mutation + file I/O. `client download` has special streaming. Not every command fits perfectly in one bucket. | Place hybrids in the bucket that covers their primary pattern. Note the deviation. The agent adapts the template for the specific case. |

### Net Assessment

Pattern Buckets is the stronger approach for this specific project because:

1. **The test patterns ARE the primary complexity.** The domain knowledge needed per command is minimal -- it's the test harness patterns, mock setup, snapshot mechanics, and coverage requirements that take the work. Grouping by pattern directly addresses the primary complexity.

2. **The ratio of pattern-types to commands is favorable.** 7 patterns for 32 commands means each pattern covers ~4.6 commands on average. Enough repetition to justify templates, not so much that the work becomes mindless.

3. **The subscription commands are the biggest risk.** Isolating them into their own phase (after simpler patterns are complete) de-risks the project. If subscription testing proves harder than expected, all other tests are already done.

4. **Category-based grouping would duplicate the hardest infrastructure work.** Every category with validate/publish commands (schema, client, openapi, mcp, fusion) would need to independently solve subscription mocking. Pattern Buckets solves it once.
