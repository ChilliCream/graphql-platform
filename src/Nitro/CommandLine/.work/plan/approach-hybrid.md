# Hybrid Dependency-Driven Approach

## Executive Summary

32 commands remain untested across 9 categories. Rather than testing by category alone (which ignores shared infrastructure) or by pattern alone (which ignores domain context), this approach groups work by **client interface dependency chains**, identifies commands that need **implementation fixes before testing**, and designs **parallel work streams** that minimize blocking dependencies.

The key insight: 10 of 32 commands use the legacy `PrintMutationErrorsAndExit` pattern and must be migrated before tests can be written. These 10 commands are also the subscription-based commands, which are the most complex to test. By front-loading the simple non-subscription commands across parallel streams and deferring subscription commands until their simpler siblings are done, we maximize parallelism and build testing infrastructure incrementally.

---

## 1. Dependency Analysis

### 1.1 Client Interface Groupings

Each command depends on a primary client interface. Commands sharing the same client share mock setup patterns, error types, and payload structures.

| Client Interface | Commands (Remaining) | Count |
|---|---|---|
| `ISchemasClient` | download, upload, validate*, publish* | 4 |
| `IClientsClient` | download, list-versions, list-published-versions, publish*, unpublish, upload, validate* | 7 |
| `IMcpClient` + `IApisClient` | create, delete, list, publish*, upload, validate* | 6 |
| `IOpenApiCollectionsClient` + `IApisClient` | create, delete, list, publish*, upload, validate* | 6 |
| `IWorkspacesClient` | create, current, list, set-default, show | 5 |
| `IPersonalAccessTokensClient` | create, list, revoke | 3 |
| `IMocksClient` + `IApisClient` | create, list, update | 3 |
| `IStagesClient` + `IApisClient` | delete, edit, list | 3 |
| `IFusionConfigurationClient` | download, publish, run, settings-set, upload, validate*, publish-begin, publish-start, publish-validate*, publish-commit, publish-cancel | 11 |
| (none / special) | launch, login, logout | 3 |

**\* = subscription command requiring implementation migration**

### 1.2 Structural Isomorphism: MCP and OpenAPI

MCP and OpenAPI are **structurally identical** -- same command set (create, delete, list, publish, upload, validate), same patterns, same error types, mirrored type names. This is the strongest case for shared test helpers and parallel implementation.

| MCP Command | OpenAPI Command | Shared Pattern |
|---|---|---|
| `CreateMcpFeatureCollectionCommand` | `CreateOpenApiCollectionCommand` | Mutation + `PrintMutationErrorsAndExit` |
| `DeleteMcpFeatureCollectionCommand` | `DeleteOpenApiCollectionCommand` | Mutation + `PrintMutationErrorsAndExit` |
| `ListMcpFeatureCollectionCommand` | `ListOpenApiCollectionCommand` | List + pagination |
| `UploadMcpFeatureCollectionCommand` | `UploadOpenApiCollectionCommand` | File upload + mutation |
| `ValidateMcpFeatureCollectionCommand` | `ValidateOpenApiCollectionCommand` | Subscription + `PrintMutationErrorsAndExit` |
| `PublishMcpFeatureCollectionCommand` | `PublishOpenApiCollectionCommand` | Subscription + `PrintMutationErrorsAndExit` |

### 1.3 Cross-Client Dependencies

Some commands inject **multiple** client interfaces:

- **MCP create** uses both `IMcpClient` and `IApisClient` (for API selection prompt)
- **OpenAPI create** uses both `IOpenApiCollectionsClient` and `IApisClient`
- **Mock create/update** uses both `IMocksClient` and `IApisClient`
- **Stage edit** uses both `IStagesClient` and `IApisClient`
- **Login** uses `IWorkspacesClient` and calls `SetDefaultWorkspaceCommand.ExecuteAsync()` directly

This means `IApisClient` mock patterns (already established in completed api tests) are reused across MCP, OpenAPI, Mock, and Stage test suites.

---

## 2. Implementation-First Identification

### 2.1 Commands Requiring Code Changes Before Testing

**Criterion**: Uses `PrintMutationErrorsAndExit` (legacy) or `PrintMutationErrors` (legacy in subscription context), lacks `AssertHasAuthentication`, or violates activity lifecycle rules.

#### Tier 1: Subscription Commands (10) -- Must Migrate Error Handling + Activity Lifecycle

These all use `PrintMutationErrorsAndExit` for initial mutation errors and `PrintMutationErrors` within subscription handlers. Both patterns violate the guidelines (write to stdout, no `activity.Fail()`, generic `ExitException`).

| Command | Issues |
|---|---|
| `ValidateSchemaCommand` | Uses `PrintMutationErrors` in subscription handler (stdout, no stderr summary) |
| `PublishSchemaCommand` | Uses `PrintMutationErrors` in subscription handler, `console.WriteLine` before error |
| `ValidateClientCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors`, no `activity.Fail()` on subscription error, missing `AssertHasAuthentication` |
| `PublishClientCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors`, no `activity.Fail()` on subscription error, missing `AssertHasAuthentication` |
| `ValidateOpenApiCollectionCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors`, no `activity.Fail()` on subscription error |
| `PublishOpenApiCollectionCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors`, no `activity.Fail()` on subscription error |
| `ValidateMcpFeatureCollectionCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors`, no `activity.Fail()` on subscription error |
| `PublishMcpFeatureCollectionCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors`, no `activity.Fail()` on subscription error |
| `FusionValidateCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors`, no `activity.Fail()` in inner method |
| `FusionConfigurationPublishValidateCommand` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |

#### Tier 2: Non-Subscription Commands Using `PrintMutationErrorsAndExit` (11) -- Simpler Migration

These use the legacy error pattern but don't have subscription complexity.

| Command | Issues |
|---|---|
| `CreateWorkspaceCommand` | `PrintMutationErrorsAndExit`, no activity wrapping mutation, missing `AssertHasAuthentication` |
| `CreateMcpFeatureCollectionCommand` | `PrintMutationErrorsAndExit`, no activity wrapping, missing `AssertHasAuthentication` |
| `DeleteMcpFeatureCollectionCommand` | `PrintMutationErrorsAndExit`, missing `AssertHasAuthentication` |
| `CreateOpenApiCollectionCommand` | `PrintMutationErrorsAndExit`, no activity wrapping, missing `AssertHasAuthentication` |
| `DeleteOpenApiCollectionCommand` | `PrintMutationErrorsAndExit`, missing `AssertHasAuthentication` |
| `CreatePersonalAccessTokenCommand` | `PrintMutationErrorsAndExit`, no activity wrapping, missing `AssertHasAuthentication` (uses API key auth via `--api-key` check) |
| `RevokePersonalAccessTokenCommand` | `PrintMutationErrorsAndExit`, missing `AssertHasAuthentication` |
| `CreateMockCommand` | `PrintMutationErrorsAndExit`, missing `AssertHasAuthentication` |
| `UpdateMockCommand` | `PrintMutationErrorsAndExit`, missing `AssertHasAuthentication` |
| `DeleteStageCommand` | `PrintMutationErrorsAndExit`, missing `AssertHasAuthentication` |
| `EditStagesCommand` | `PrintMutationErrorsAndExit`, missing `AssertHasAuthentication` |

#### Tier 3: Commands That Are Already Guidelines-Compliant (11) -- Test Immediately

These commands already follow the guidelines or have minimal deviations that don't affect testability.

| Command | Status |
|---|---|
| `DownloadSchemaCommand` | Compliant (has `AssertHasAuthentication`, typed errors, activity lifecycle) |
| `UploadSchemaCommand` | Compliant |
| `DownloadClientCommand` | Needs verification (file I/O pattern) |
| `ListClientVersionsCommand` | Needs verification (list pattern) |
| `ListClientPublishedVersionsCommand` | Needs verification (list pattern) |
| `UnpublishClientCommand` | Uses `PrintMutationErrorsAndExit` but simple mutation |
| `UploadClientCommand` | Needs verification |
| `ListMcpFeatureCollectionCommand` | Needs verification (list pattern) |
| `ListOpenApiCollectionCommand` | Needs verification (list pattern) |
| `ListMockCommand` | Needs verification (list pattern) |
| `ListStagesCommand` | Needs verification |

**Note on "needs verification"**: These commands may or may not fully comply. A quick review during implementation will determine whether a code fix is needed. The fix is always the same pattern (add `AssertHasAuthentication`, replace `PrintMutationErrorsAndExit` with typed switch, wrap in activity).

### 2.2 Commands Requiring No Code Changes (Standalone)

| Command | Notes |
|---|---|
| `LaunchCommand` | Simple, no client, no auth |
| `LogoutCommand` | Simple, session clear |
| `LoginCommand` | Special auth flow, delegates to `SetDefaultWorkspaceCommand.ExecuteAsync()` |
| `CurrentWorkspaceCommand` | Simple query |
| `ShowWorkspaceCommand` | Simple query |
| `ListWorkspaceCommand` | List pattern |
| `SetDefaultWorkspaceCommand` | Simple mutation |
| `ListPersonalAccessTokenCommand` | List pattern |
| `FusionSettingsSetCommand` | Simple mutation |
| `FusionDownloadCommand` | File I/O |
| `FusionRunCommand` | Process management (special) |

---

## 3. Parallel Work Streams

### Stream Design Principles

1. **No stream depends on another stream's output** -- all streams can run fully in parallel
2. **Within a stream, non-subscription commands come first** -- they build mock helpers reused by subscription tests
3. **Implementation fixes are done inline** -- fix the command, then write the test, in the same unit of work
4. **Each stream has a clear "done" definition** with a quality gate

### Stream A: Schema + Workspace + PAT + Standalone (16 commands)

**Rationale**: These categories have independent client interfaces (`ISchemasClient`, `IWorkspacesClient`, `IPersonalAccessTokensClient`) with no cross-dependencies between them. Schema commands are already guidelines-aligned per the migration doc.

**Phase A1 -- Guideline-Compliant Commands (test immediately)**
1. `schema download` (DownloadSchemaCommand) -- file I/O, `ISchemasClient`, compliant
2. `schema upload` (UploadSchemaCommand) -- file upload, `ISchemasClient`, compliant
3. `workspace create` (CreateWorkspaceCommand) -- fix `PrintMutationErrorsAndExit` + add auth, then test
4. `workspace current` (CurrentWorkspaceCommand) -- simple query, test immediately
5. `workspace show` (ShowWorkspaceCommand) -- simple query, test immediately
6. `workspace list` (ListWorkspaceCommand) -- list pattern, test immediately
7. `workspace set-default` (SetDefaultWorkspaceCommand) -- simple mutation, test immediately
8. `pat create` (CreatePersonalAccessTokenCommand) -- fix `PrintMutationErrorsAndExit`, then test
9. `pat list` (ListPersonalAccessTokenCommand) -- list pattern, test immediately
10. `pat revoke` (RevokePersonalAccessTokenCommand) -- fix `PrintMutationErrorsAndExit`, then test
11. `launch` (LaunchCommand) -- special: mock `SystemBrowser.Open`
12. `logout` (LogoutCommand) -- simple session clear
13. `login` (LoginCommand) -- special: mock browser auth + workspace selection

**Phase A2 -- Subscription Commands (migrate + test)**
14. `schema validate` (ValidateSchemaCommand) -- fix subscription error handling, then test
15. `schema publish` (PublishSchemaCommand) -- fix subscription error handling, then test

**Shared Helper**: `SchemaCommandTestHelper.cs` (upload/download payloads reused in validate/publish).

**Command count**: 15 commands (4 schema + 5 workspace + 3 PAT + 3 standalone)

### Stream B: MCP + OpenAPI (12 commands)

**Rationale**: MCP and OpenAPI are structurally isomorphic. Building them together means the test patterns developed for one directly transfer to the other. They share the `IApisClient` dependency for the `create` command's API selection prompt.

**Phase B1 -- Non-Subscription Commands (6, parallel between MCP and OpenAPI)**
1. `mcp create` (CreateMcpFeatureCollectionCommand) -- fix `PrintMutationErrorsAndExit` + auth, test
2. `mcp delete` (DeleteMcpFeatureCollectionCommand) -- fix `PrintMutationErrorsAndExit` + auth, test
3. `mcp list` (ListMcpFeatureCollectionCommand) -- list pattern, test
4. `openapi create` (CreateOpenApiCollectionCommand) -- fix `PrintMutationErrorsAndExit` + auth, test
5. `openapi delete` (DeleteOpenApiCollectionCommand) -- fix `PrintMutationErrorsAndExit` + auth, test
6. `openapi list` (ListOpenApiCollectionCommand) -- list pattern, test

**Phase B2 -- Upload Commands (2)**
7. `mcp upload` (UploadMcpFeatureCollectionCommand) -- file upload pattern, test
8. `openapi upload` (UploadOpenApiCollectionCommand) -- file upload pattern, test

**Phase B3 -- Subscription Commands (4, depends on B1/B2 for helper infrastructure)**
9. `mcp validate` (ValidateMcpFeatureCollectionCommand) -- migrate subscription pattern, test
10. `mcp publish` (PublishMcpFeatureCollectionCommand) -- migrate subscription pattern, test
11. `openapi validate` (ValidateOpenApiCollectionCommand) -- migrate subscription pattern, test
12. `openapi publish` (PublishOpenApiCollectionCommand) -- migrate subscription pattern, test

**Shared Helpers**:
- `McpCommandTestHelper.cs` (collection payloads, validation error mocks)
- `OpenApiCommandTestHelper.cs` (collection payloads, validation error mocks)

**Command count**: 12 commands (6 MCP + 6 OpenAPI)

### Stream C: Client Remaining + Mock + Stage (13 commands)

**Rationale**: Client commands share `IClientsClient`. Mock and Stage commands share `IApisClient` for API lookup. Stage `edit` is the most complex interactive command in the entire CLI -- it needs its own sub-phase.

**Phase C1 -- Simple Client Commands (5)**
1. `client download` (DownloadClientCommand) -- file I/O with stream, test
2. `client list-versions` (ListClientVersionsCommand) -- list pattern, test
3. `client list-published-versions` (ListClientPublishedVersionsCommand) -- list pattern, test
4. `client unpublish` (UnpublishClientCommand) -- fix `PrintMutationErrorsAndExit`, test
5. `client upload` (UploadClientCommand) -- file upload, test

**Phase C2 -- Mock and Stage Non-Complex (5)**
6. `mock create` (CreateMockCommand) -- fix `PrintMutationErrorsAndExit` + auth, test (file I/O)
7. `mock list` (ListMockCommand) -- list pattern, test
8. `mock update` (UpdateMockCommand) -- fix `PrintMutationErrorsAndExit` + auth, test (file I/O)
9. `stage delete` (DeleteStageCommand) -- fix `PrintMutationErrorsAndExit` + auth, test
10. `stage list` (ListStagesCommand) -- list pattern, test

**Phase C3 -- Complex Interactive + Subscription (3)**
11. `stage edit` (EditStagesCommand) -- most complex interactive command, fix + test
12. `client validate` (ValidateClientCommand) -- migrate subscription, add `AssertHasAuthentication`, test
13. `client publish` (PublishClientCommand) -- migrate subscription, add `AssertHasAuthentication`, test

**Shared Helpers**:
- Existing client test helpers already exist for `IClientsClient`
- `MockCommandTestHelper.cs` (if payload complexity warrants)
- `StageCommandTestHelper.cs` (needed for `EditStagesCommand` complex state)

**Command count**: 13 commands (7 client + 3 mock + 3 stage)

### Stream D: Fusion (11 commands) -- Deferred Start

**Rationale**: Fusion is the most complex category with the multi-step publish flow (begin/start/validate/commit/cancel). It depends on `IFusionConfigurationClient` exclusively. The publish flow is a stateful 5-command sequence. This stream should start after at least one subscription command has been successfully tested in another stream, so the subscription test patterns are proven.

**Phase D1 -- Simple Fusion Commands (4)**
1. `fusion download` (FusionDownloadCommand) -- file I/O, test
2. `fusion settings set` (FusionSettingsSetCommand) -- simple mutation, test
3. `fusion upload` (FusionUploadCommand) -- fix `PrintMutationErrorsAndExit`, file upload, test
4. `fusion run` (FusionRunCommand) -- process management, special test setup

**Phase D2 -- Fusion Subscription Commands (2)**
5. `fusion validate` (FusionValidateCommand) -- complex multi-schema validation, migrate + test
6. `fusion publish` (FusionPublishCommand) -- multi-step orchestration, test

**Phase D3 -- Fusion Publish Flow (5)**
7. `fusion publish begin` (FusionConfigurationPublishBeginCommand) -- start publish, test
8. `fusion publish start` (FusionConfigurationPublishStartCommand) -- begin composition, test
9. `fusion publish validate` (FusionConfigurationPublishValidateCommand) -- migrate subscription, test
10. `fusion publish commit` (FusionConfigurationPublishCommitCommand) -- commit, test
11. `fusion publish cancel` (FusionConfigurationPublishCancelCommand) -- cancel, test

**Shared Helper**: `FusionCommandTestHelper.cs` (publish state mocks, archive mocks, configuration mocks).

**Command count**: 11 commands

---

## 4. Shared Fixture Strategy

### 4.1 Per-Client-Interface Test Helpers

Each client interface group gets a shared test helper **only when mock payloads are reused 3+ times or require 5+ nested mocks**.

| Helper File | Client Interface | Provides |
|---|---|---|
| `SchemaCommandTestHelper.cs` | `ISchemasClient` | Upload/download payloads, validation request mocks, subscription update mocks |
| `WorkspaceCommandTestHelper.cs` | `IWorkspacesClient` | Workspace detail mocks, list page mocks |
| `PatCommandTestHelper.cs` | `IPersonalAccessTokensClient` | Token creation result, list page mocks |
| `McpCommandTestHelper.cs` | `IMcpClient` | Collection detail, validation error tree mocks, subscription update mocks |
| `OpenApiCommandTestHelper.cs` | `IOpenApiCollectionsClient` | Collection detail, validation error tree mocks, subscription update mocks |
| `MockCommandTestHelper.cs` | `IMocksClient` | Mock schema payloads (if complexity warrants) |
| `StageCommandTestHelper.cs` | `IStagesClient` | Stage list, edit state mocks |
| `FusionCommandTestHelper.cs` | `IFusionConfigurationClient` | Publish state, archive, configuration, subscription mocks |

**Decision rule**: Create the helper file when the **second** command in that client group is being tested and the first command's payloads would be reused. Don't pre-create helpers speculatively.

### 4.2 Subscription Test Infrastructure

Subscription commands require mocking `IAsyncEnumerable<T>`. A shared utility for this is warranted:

```csharp
// Already in test infrastructure or should be added:
public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
```

This allows subscription tests to compose sequences of update events:
```csharp
client.Setup(x => x.SubscribeToValidationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .Returns(ToAsyncEnumerable(
        CreateInProgressUpdate(),
        CreateValidationSuccessUpdate()));
```

If this utility doesn't exist yet, it should be created once during the first subscription command test (earliest in Phase A2 or B3).

### 4.3 What NOT to Share

- **Error type switch expectations**: Each command has its own generated error interfaces. Don't try to generalize these.
- **Interactive prompt sequences**: Each command's interactive flow is unique. Keep these inline per test.
- **Help output snapshots**: Always command-specific.

---

## 5. Subscription Integration Strategy

### 5.1 When to Tackle Subscriptions

Subscription commands are **always last** within their stream. This is deliberate:

1. **Non-subscription siblings build the mock infrastructure** -- upload/download/create/list tests establish payload factories reused by validate/publish tests
2. **Implementation fixes are more complex** -- replacing `PrintMutationErrorsAndExit` in a subscription command touches both the initial mutation AND the subscription handler
3. **Test patterns are more complex** -- subscription tests need `IAsyncEnumerable` mocking, multiple state transitions, and activity lifecycle verification

### 5.2 Implementation Migration Pattern for Subscription Commands

Every subscription command migration follows the same template:

1. **Replace `PrintMutationErrorsAndExit` with typed switch** on initial mutation errors
2. **Add `activity.Fail()` before error output** in subscription failure handlers
3. **Replace `PrintMutationErrors(errors)` with per-error dispatch**: rich errors to stdout via `console.PrintMutationError(error)`, simple summary to stderr
4. **Add `activity.Success("...")` on success path**
5. **Ensure fallthrough path has `activity.Fail()`**
6. **Add `AssertHasAuthentication`** if missing

### 5.3 Subscription Test Coverage Matrix

Each subscription command needs these tests beyond the standard set:

| Test Case | Description |
|---|---|
| Initial mutation error branches | Typed switch per error in `.graphql` |
| Subscription success | Terminal success state |
| Subscription validation failure | Rich errors rendered + stderr summary |
| Subscription in-progress | `activity.Update` called |
| Subscription queue state | Queue position shown (publish commands) |
| Subscription approval state | Approval message + deployment errors (publish commands) |
| Subscription unknown state | Default handler with upgrade message |
| Subscription ends without terminal | Fallthrough to `activity.Fail()` + `ExitCodes.Error` |

---

## 6. Concrete Phase Plan

### Phase 0: Infrastructure Setup (Before Streams Start)

- Verify `IAsyncEnumerable<T>` mock utility exists in test infrastructure; add if missing
- Verify existing `ApiCommandTestHelper` patterns for `IApisClient` mock setup (reused by MCP, OpenAPI, Mock, Stage commands)

### Phase 1: Parallel Streams Launch (All 4 Streams)

**Stream A** starts with: `schema download`, `schema upload`, `workspace current`, `workspace show`
**Stream B** starts with: `mcp create`, `mcp delete`, `mcp list`, `openapi create`, `openapi delete`, `openapi list`
**Stream C** starts with: `client download`, `client list-versions`, `client list-published-versions`
**Stream D** starts with: `fusion download`, `fusion settings set` (can start in parallel but is the lowest priority)

### Phase 2: Mid-Stream Progress

**Stream A**: `workspace list`, `workspace set-default`, `workspace create` (fix + test), PAT commands, standalone commands
**Stream B**: `mcp upload`, `openapi upload`
**Stream C**: `client unpublish`, `client upload`, mock commands, stage delete, stage list

### Phase 3: Complex / Subscription Commands

**Stream A**: `schema validate` (subscription), `schema publish` (subscription)
**Stream B**: `mcp validate`, `mcp publish`, `openapi validate`, `openapi publish` (all subscription)
**Stream C**: `stage edit` (complex interactive), `client validate`, `client publish` (subscription)
**Stream D**: `fusion validate`, `fusion publish`, fusion publish flow (5 commands)

### Phase 4: Final Verification

- Run full test suite: `dotnet test` on CommandLine.Tests
- Verify all 32 commands are in `done` status in COMMAND_TEST_MIGRATION_PROGRESS.md
- Update migration progress document

---

## 7. Quality Gates

### Gate 1: Stream Entry (Before Starting Any Command)

- [ ] Command implementation reviewed against COMMAND_IMPLEMENTATION_GUIDELINES.md
- [ ] If non-compliant: implementation fix committed and verified before test writing begins
- [ ] Fix verified by building: `dotnet build` on the CommandLine project

### Gate 2: Per-Command Completion

- [ ] Help output snapshot test passes
- [ ] Auth error test passes (Theory x 3 modes)
- [ ] Client exception test passes (Theory x 3 modes)
- [ ] Authorization exception test passes (Theory x 3 modes)
- [ ] All mode-specific success paths pass
- [ ] All mutation error branches covered (per mode where output differs)
- [ ] Interactive prompting tested (if applicable)
- [ ] `client.VerifyAll()` called in every test with mocks
- [ ] Tests pass with `dotnet test --filter "FullyQualifiedName~{TestClass}"`

### Gate 3: Subscription Command Completion (Extension of Gate 2)

- [ ] Initial mutation error branches tested
- [ ] Subscription success path tested
- [ ] Subscription failure with rich errors tested
- [ ] Subscription in-progress state tested
- [ ] Subscription queue/approval states tested (publish commands)
- [ ] Subscription unknown state tested
- [ ] Subscription fallthrough (no terminal state) tested
- [ ] Activity lifecycle verified (every path ends in Fail or Success)

### Gate 4: Stream Completion

- [ ] All commands in stream pass Gate 2 (or Gate 3 for subscription commands)
- [ ] Test helper files are complete and consistent
- [ ] No `[Fact]` tests that should be `[Theory]` (mode coverage)
- [ ] Full stream test run: `dotnet test --filter "FullyQualifiedName~{Category}"`

### Gate 5: Project Completion

- [ ] `dotnet test` passes for all CommandLine.Tests
- [ ] COMMAND_TEST_MIGRATION_PROGRESS.md updated: all 32 commands marked `done`
- [ ] No remaining `PrintMutationErrorsAndExit` calls in any command (all migrated)
- [ ] All subscription commands follow the target pattern from research.md

---

## Appendix: Command-to-Stream Assignment Summary

| Stream | Phase 1 (Simple) | Phase 2 (Medium) | Phase 3 (Complex/Subscription) | Total |
|---|---|---|---|---|
| A | schema download/upload, workspace current/show | workspace list/set-default/create, PAT x3, standalone x3 | schema validate/publish | 15 |
| B | mcp create/delete/list, openapi create/delete/list | mcp upload, openapi upload | mcp validate/publish, openapi validate/publish | 12 |
| C | client download/list-versions/list-published-versions | client unpublish/upload, mock x3, stage delete/list | stage edit, client validate/publish | 13 |
| D | fusion download/settings-set | fusion upload, fusion run | fusion validate/publish, publish flow x5 | 11 |
| **Total** | **12** | **13** | **16** | **51 tasks** |

Note: 51 tasks covers 32 commands plus implementation fixes counted as separate tasks within the same unit of work. The actual command count is 32.
