# Nitro CLI Test Migration — Full Breakdown

## Overview

- **Total commands to cover**: 47 (excluding login/logout/launch/fusion-run)
- **Epics**: 6 (E0 preparatory, E1-E4 parallel streams, E5 final cleanup)
- **Estimated total tests**: ~500-650 new test methods

## Dependency Graph

```
E0 (ConsoleHelpers refactor)
 |
 +---> E1 (Schema + Workspace + PAT) ----+
 |                                         |
 +---> E2 (MCP + OpenAPI) ---------------+
 |                                         |---> E5 (Final Cleanup)
 +---> E3 (Client + Mock + Stage) -------+
 |                                         |
 +---> E4 (Fusion) ----------------------+

Cross-epic gate: E1-P3-T1 (ValidateSchemaCommand + ToAsyncEnumerable)
must complete before ANY other epic's Phase 3 starts.

Within E2: Each OpenAPI task depends on its MCP counterpart.
Within E4-P2: FusionPublishHelpers migration (T1) blocks all other P2 tasks.
```

---

## Epic 0: Preparatory — ConsoleHelpers Refactor

**Must complete before Epics 1-4 start.**

### Phase 1: Rename and Deprecate

#### Task E0-P1-T1: Refactor ConsoleHelpers.cs
- **Command**: N/A (infrastructure)
- **Tier**: N/A
- **Agent**: Stream 1 agent (or dedicated prep agent)
- **Dependencies**: None
- **Files to modify**:
  - `src/CommandLine/Helpers/ConsoleHelpers.cs`
  - All callers of the renamed methods (the `PrintMutationError(object)` dispatcher + any direct callers)
- **Files to create**: None
- **What to do**:
  1. Rename typed `PrintMutationError` overloads to descriptive names:
     - `PrintMutationError(ISchemaChangeViolationError)` -> `PrintSchemaChangeViolations(...)`
     - `PrintMutationError(ISchemaVersionChangeViolationError)` -> `PrintSchemaVersionChangeViolations(...)`
     - `PrintMutationError(IMcpFeatureCollectionValidationError)` -> `PrintMcpFeatureCollectionValidationErrors(...)`
     - `PrintMutationError(IOpenApiCollectionValidationError)` -> `PrintOpenApiCollectionValidationErrors(...)`
     - `PrintMutationError(IPersistedQueryValidationError)` -> `PrintPersistedQueryValidationErrors(...)`
     - `PrintMutationError(IInvalidGraphQLSchemaError)` -> `PrintGraphQLSchemaErrors(...)`
     - `PrintMutationError(IStagesHavePublishedDependenciesError)` -> `PrintStagePublishedDependencies(...)`
  2. Update the `PrintMutationError(object)` dispatcher's switch arms to call the new names
  3. Update any direct callers of the renamed methods
  4. Mark `[Obsolete]`: `PrintMutationErrorsAndExit`, `PrintMutationErrors`, `PrintMutationError(object)`
  5. Run `dotnet build` to verify everything compiles (with obsolete warnings only)
- **Est. tests**: 0 (infrastructure only)

---

## Epic 1: Stream 1 — Schema + Workspace + PAT (12 commands)

**Agent**: 1 agent

### Phase 1: Tier A Commands (8 commands, parallel within phase)

#### Task E1-P1-T1: schema download
- **Command**: `DownloadSchemaCommand`
- **Tier**: A
- **Agent**: Stream 1
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Schemas/DownloadSchemaCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Schemas/DownloadSchemaCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md; standardize `ExecuteAsync` signature if needed
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write success path tests: file download with output path, default output, existing file overwrite
  4. Run `dotnet test --filter "FullyQualifiedName~DownloadSchemaCommandTests"`
- **Est. tests**: 12-14

#### Task E1-P1-T2: schema upload
- **Command**: `UploadSchemaCommand`
- **Tier**: A
- **Agent**: Stream 1
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Schemas/UploadSchemaCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Schemas/UploadSchemaCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md; standardize `ExecuteAsync` signature if needed
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write success path tests: file upload with schema file, missing file error
  4. Run `dotnet test --filter "FullyQualifiedName~UploadSchemaCommandTests"`
- **Est. tests**: 12-16

#### Task E1-P1-T3: workspace current
- **Command**: `CurrentWorkspaceCommand`
- **Tier**: A
- **Agent**: Stream 1
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Workspaces/CurrentWorkspaceCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Workspaces/CurrentWorkspaceCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write success path tests per mode (Interactive, NonInteractive, JsonOutput)
  4. Run `dotnet test --filter "FullyQualifiedName~CurrentWorkspaceCommandTests"`
- **Est. tests**: 8-10

#### Task E1-P1-T4: workspace show
- **Command**: `ShowWorkspaceCommand`
- **Tier**: A
- **Agent**: Stream 1
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Workspaces/ShowWorkspaceCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Workspaces/ShowWorkspaceCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write success path tests per mode, resource not found
  4. Run `dotnet test --filter "FullyQualifiedName~ShowWorkspaceCommandTests"`
- **Est. tests**: 10-12

#### Task E1-P1-T5: workspace list
- **Command**: `ListWorkspaceCommand`
- **Tier**: A
- **Agent**: Stream 1
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Workspaces/ListWorkspaceCommand.cs` (auth standardization)
- **Files to create**: `test/CommandLine.Tests/Commands/Workspaces/ListWorkspaceCommandTests.cs`
- **What to do**:
  1. Review and standardize auth pattern
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write list-specific tests: success per mode, cursor pagination, empty results
  4. Run `dotnet test --filter "FullyQualifiedName~ListWorkspaceCommandTests"`
- **Est. tests**: 10-14

#### Task E1-P1-T6: workspace set-default
- **Command**: `SetDefaultWorkspaceCommand`
- **Tier**: A
- **Agent**: Stream 1
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Workspaces/SetDefaultWorkspaceCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Workspaces/SetDefaultWorkspaceCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write success path tests per mode, missing workspace ID
  4. Run `dotnet test --filter "FullyQualifiedName~SetDefaultWorkspaceCommandTests"`
- **Est. tests**: 10-12

#### Task E1-P1-T7: pat list
- **Command**: `ListPersonalAccessTokenCommand`
- **Tier**: A
- **Agent**: Stream 1
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/PersonalAccessTokens/ListPersonalAccessTokenCommand.cs` (auth standardization)
- **Files to create**: `test/CommandLine.Tests/Commands/PersonalAccessTokens/ListPersonalAccessTokenCommandTests.cs`
- **What to do**:
  1. Review and standardize auth pattern
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write list-specific tests: success per mode, cursor pagination, empty results
  4. Run `dotnet test --filter "FullyQualifiedName~ListPersonalAccessTokenCommandTests"`
- **Est. tests**: 10-14

#### Task E1-P1-T8: (reserved — no 8th Tier A in this stream)
- **Note**: Phase 1 has 7 tasks (T1-T7). Numbering continues in Phase 2.

### Phase 2: Tier B Commands — Migrate + Test (3 commands, parallel within phase)

**Dependencies**: All of E1-P1 complete.

#### Task E1-P2-T1: workspace create
- **Command**: `CreateWorkspaceCommand`
- **Tier**: B
- **Agent**: Stream 1
- **Dependencies**: E1-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Workspaces/CreateWorkspaceCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Workspaces/CreateWorkspaceCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync` signature, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity, add `activity.Fail()`/`activity.Success()`
  4. `dotnet build` to verify
  5. Write test suite: help snapshot, no-auth, client exception, auth exception (all Theory x3)
  6. Write mutation error branches per mode, success with all options, interactive prompting
  7. Run `dotnet test --filter "FullyQualifiedName~CreateWorkspaceCommandTests"`
- **Est. tests**: 15-20

#### Task E1-P2-T2: pat create
- **Command**: `CreatePersonalAccessTokenCommand`
- **Tier**: B
- **Agent**: Stream 1
- **Dependencies**: E1-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/PersonalAccessTokens/CreatePersonalAccessTokenCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/PersonalAccessTokens/CreatePersonalAccessTokenCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync` signature, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity, add `activity.Fail()`/`activity.Success()`
  4. `dotnet build` to verify
  5. Write test suite: help snapshot, no-auth, client exception, auth exception (all Theory x3)
  6. Write mutation error branches per mode, success paths, interactive prompting
  7. Run `dotnet test --filter "FullyQualifiedName~CreatePersonalAccessTokenCommandTests"`
- **Est. tests**: 15-18

#### Task E1-P2-T3: pat revoke
- **Command**: `RevokePersonalAccessTokenCommand`
- **Tier**: B
- **Agent**: Stream 1
- **Dependencies**: E1-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/PersonalAccessTokens/RevokePersonalAccessTokenCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/PersonalAccessTokens/RevokePersonalAccessTokenCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync` signature, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity, add `activity.Fail()`/`activity.Success()`
  4. `dotnet build` to verify
  5. Write test suite: help snapshot, no-auth, client exception, auth exception (all Theory x3)
  6. Write mutation error branches per mode, success paths
  7. Run `dotnet test --filter "FullyQualifiedName~RevokePersonalAccessTokenCommandTests"`
- **Est. tests**: 12-16

### Phase 3: Tier C Commands — Subscription Migration + Test (2 commands, sequential)

**Dependencies**: All of E1-P2 complete. **CRITICAL**: E1-P3-T1 is the cross-epic gate — must complete before any other epic's Phase 3 starts.

#### Task E1-P3-T1: schema validate (CROSS-EPIC GATE)
- **Command**: `ValidateSchemaCommand`
- **Tier**: C
- **Agent**: Stream 1
- **Dependencies**: E1-P2 (all)
- **Files to modify**: `src/CommandLine/Commands/Schemas/ValidateSchemaCommand.cs` (subscription handler only — initial mutation already has typed switch)
- **Files to create**:
  - `test/CommandLine.Tests/TestHelpers.cs` (contains `ToAsyncEnumerable<T>`)
  - `test/CommandLine.Tests/Commands/Schemas/ValidateSchemaCommandTests.cs`
  - `test/CommandLine.Tests/Commands/Schemas/SchemaCommandTestHelper.cs` (subscription event factories)
- **What to do**:
  1. Create `ToAsyncEnumerable<T>` helper in shared test infrastructure
  2. Migrate subscription handler: replace `PrintMutationErrors` with inline `foreach` + `switch`
  3. Rich errors -> stdout via renamed typed methods; simple errors -> stderr
  4. Add `activity.Fail()`/`activity.Success()` to subscription paths
  5. `dotnet build` to verify
  6. Write full subscription test suite (see Subscription Test Matrix in plan):
     - Help, no-auth, client exception, auth exception
     - Each mutation error branch
     - Null request ID
     - Success path (InProgress -> Success)
     - Validation failure with errors
     - In-progress only + stream ends
     - Unknown event
  7. Run `dotnet test --filter "FullyQualifiedName~ValidateSchemaCommandTests"`
- **Est. tests**: 18-25

#### Task E1-P3-T2: schema publish
- **Command**: `PublishSchemaCommand`
- **Tier**: C
- **Agent**: Stream 1
- **Dependencies**: E1-P3-T1
- **Files to modify**: `src/CommandLine/Commands/Schemas/PublishSchemaCommand.cs` (subscription handler only)
- **Files to create**: `test/CommandLine.Tests/Commands/Schemas/PublishSchemaCommandTests.cs`
- **What to do**:
  1. Migrate subscription handler: replace `PrintMutationErrors` with inline `foreach` + `switch`
  2. Rich errors -> stdout; simple errors -> stderr
  3. Add `activity.Fail()`/`activity.Success()` to subscription paths
  4. `dotnet build` to verify
  5. Write full publish subscription test suite (validate tests + queue/approval/deployment):
     - All validate tests
     - Queue position, ready state, wait-for-approval, approved state
     - Force option behavior
  6. Run `dotnet test --filter "FullyQualifiedName~PublishSchemaCommandTests"`
- **Est. tests**: 22-30

---

## Epic 2: Stream 2 — MCP + OpenAPI (12 commands)

**Agents**: 2 agents (MCP lead + OpenAPI trail)
**Rule**: Each OpenAPI task depends on its MCP counterpart completing first.

### Phase 1: Tier A List Commands (2 commands, sequential MCP->OpenAPI)

#### Task E2-P1-T1: mcp list
- **Command**: `ListMcpFeatureCollectionCommand`
- **Tier**: A
- **Agent**: MCP agent
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Mcp/ListMcpFeatureCollectionCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Mcp/ListMcpFeatureCollectionCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help snapshot, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write list-specific tests: success per mode, cursor pagination, empty results
  4. Run `dotnet test --filter "FullyQualifiedName~ListMcpFeatureCollectionCommandTests"`
- **Est. tests**: 10-14

#### Task E2-P1-T2: openapi list
- **Command**: `ListOpenApiCollectionCommand`
- **Tier**: A
- **Agent**: OpenAPI agent
- **Dependencies**: E2-P1-T1
- **Files to modify**: `src/CommandLine/Commands/OpenApi/ListOpenApiCollectionCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/OpenApi/ListOpenApiCollectionCommandTests.cs`
- **What to do**:
  1. Copy pattern from E2-P1-T1 (mcp list), adapt types and client interfaces
  2. Write test suite mirroring mcp list tests
  3. Run `dotnet test --filter "FullyQualifiedName~ListOpenApiCollectionCommandTests"`
- **Est. tests**: 10-14

### Phase 2: Tier A + B Commands (8 commands)

**Dependencies**: E2-P1 complete.

#### Task E2-P2-T1: mcp create
- **Command**: `CreateMcpFeatureCollectionCommand`
- **Tier**: B
- **Agent**: MCP agent
- **Dependencies**: E2-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Mcp/CreateMcpFeatureCollectionCommand.cs`
- **Files to create**:
  - `test/CommandLine.Tests/Commands/Mcp/CreateMcpFeatureCollectionCommandTests.cs`
  - `test/CommandLine.Tests/Commands/Mcp/McpCommandTestHelper.cs` (if payloads reused)
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity, add `activity.Fail()`/`activity.Success()`
  4. `dotnet build` to verify
  5. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths, interactive prompting
  6. Note: uses `IApisClient` as secondary client for API selection prompt
  7. Run `dotnet test --filter "FullyQualifiedName~CreateMcpFeatureCollectionCommandTests"`
- **Est. tests**: 15-20

#### Task E2-P2-T2: mcp delete
- **Command**: `DeleteMcpFeatureCollectionCommand`
- **Tier**: B
- **Agent**: MCP agent
- **Dependencies**: E2-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Mcp/DeleteMcpFeatureCollectionCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Mcp/DeleteMcpFeatureCollectionCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity
  4. `dotnet build` to verify
  5. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths, confirmation prompt
  6. Run `dotnet test --filter "FullyQualifiedName~DeleteMcpFeatureCollectionCommandTests"`
- **Est. tests**: 12-16

#### Task E2-P2-T3: mcp upload
- **Command**: `UploadMcpFeatureCollectionCommand`
- **Tier**: A
- **Agent**: MCP agent
- **Dependencies**: E2-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Mcp/UploadMcpFeatureCollectionCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Mcp/UploadMcpFeatureCollectionCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help, no-auth, client exception, auth exception
  3. Write upload-specific tests: success with file, missing file error, archive creation
  4. Run `dotnet test --filter "FullyQualifiedName~UploadMcpFeatureCollectionCommandTests"`
- **Est. tests**: 12-16

#### Task E2-P2-T4: openapi create
- **Command**: `CreateOpenApiCollectionCommand`
- **Tier**: B
- **Agent**: OpenAPI agent
- **Dependencies**: E2-P2-T1 (mcp create)
- **Files to modify**: `src/CommandLine/Commands/OpenApi/CreateOpenApiCollectionCommand.cs`
- **Files to create**:
  - `test/CommandLine.Tests/Commands/OpenApi/CreateOpenApiCollectionCommandTests.cs`
  - `test/CommandLine.Tests/Commands/OpenApi/OpenApiCommandTestHelper.cs` (if payloads reused)
- **What to do**:
  1. Copy migration pattern from E2-P2-T1 (mcp create), adapt types
  2. Migrate: typed error switch, activity, auth
  3. `dotnet build` to verify
  4. Write test suite mirroring mcp create tests with OpenAPI types
  5. Run `dotnet test --filter "FullyQualifiedName~CreateOpenApiCollectionCommandTests"`
- **Est. tests**: 15-20

#### Task E2-P2-T5: openapi delete
- **Command**: `DeleteOpenApiCollectionCommand`
- **Tier**: B
- **Agent**: OpenAPI agent
- **Dependencies**: E2-P2-T2 (mcp delete)
- **Files to modify**: `src/CommandLine/Commands/OpenApi/DeleteOpenApiCollectionCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/OpenApi/DeleteOpenApiCollectionCommandTests.cs`
- **What to do**:
  1. Copy migration pattern from E2-P2-T2 (mcp delete), adapt types
  2. Migrate: typed error switch, activity, auth
  3. `dotnet build` to verify
  4. Write test suite mirroring mcp delete tests
  5. Run `dotnet test --filter "FullyQualifiedName~DeleteOpenApiCollectionCommandTests"`
- **Est. tests**: 12-16

#### Task E2-P2-T6: openapi upload
- **Command**: `UploadOpenApiCollectionCommand`
- **Tier**: A
- **Agent**: OpenAPI agent
- **Dependencies**: E2-P2-T3 (mcp upload)
- **Files to modify**: `src/CommandLine/Commands/OpenApi/UploadOpenApiCollectionCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/OpenApi/UploadOpenApiCollectionCommandTests.cs`
- **What to do**:
  1. Copy test pattern from E2-P2-T3 (mcp upload), adapt types
  2. Write test suite mirroring mcp upload tests
  3. Run `dotnet test --filter "FullyQualifiedName~UploadOpenApiCollectionCommandTests"`
- **Est. tests**: 12-16

### Phase 3: Tier C Subscription Commands (4 commands, sequential within pairs)

**Dependencies**: E2-P2 complete AND **E1-P3-T1** (cross-epic gate — ToAsyncEnumerable + ValidateSchemaCommandTests).

#### Task E2-P3-T1: mcp validate
- **Command**: `ValidateMcpFeatureCollectionCommand`
- **Tier**: C
- **Agent**: MCP agent
- **Dependencies**: E2-P2 (all), E1-P3-T1 (cross-epic gate)
- **Files to modify**: `src/CommandLine/Commands/Mcp/ValidateMcpFeatureCollectionCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Mcp/ValidateMcpFeatureCollectionCommandTests.cs`
- **What to do**:
  1. Migrate subscription handler: replace `PrintMutationErrorsAndExit` + `PrintMutationErrors` with typed switch
  2. Rich errors (IMcpFeatureCollectionValidationError) -> stdout via `PrintMcpFeatureCollectionValidationErrors`
  3. Simple errors -> stderr
  4. Activity lifecycle: Fail/Success on all paths
  5. `dotnet build` to verify
  6. Write full subscription test suite (validate matrix from plan)
  7. Run `dotnet test --filter "FullyQualifiedName~ValidateMcpFeatureCollectionCommandTests"`
- **Est. tests**: 18-25

#### Task E2-P3-T2: mcp publish
- **Command**: `PublishMcpFeatureCollectionCommand`
- **Tier**: C
- **Agent**: MCP agent
- **Dependencies**: E2-P3-T1
- **Files to modify**: `src/CommandLine/Commands/Mcp/PublishMcpFeatureCollectionCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Mcp/PublishMcpFeatureCollectionCommandTests.cs`
- **What to do**:
  1. Migrate subscription handler with full publish state handling
  2. Activity lifecycle on all paths
  3. `dotnet build` to verify
  4. Write full publish subscription test suite (validate + queue/approval/deployment)
  5. Run `dotnet test --filter "FullyQualifiedName~PublishMcpFeatureCollectionCommandTests"`
- **Est. tests**: 22-30

#### Task E2-P3-T3: openapi validate
- **Command**: `ValidateOpenApiCollectionCommand`
- **Tier**: C
- **Agent**: OpenAPI agent
- **Dependencies**: E2-P3-T1 (mcp validate)
- **Files to modify**: `src/CommandLine/Commands/OpenApi/ValidateOpenApiCollectionCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/OpenApi/ValidateOpenApiCollectionCommandTests.cs`
- **What to do**:
  1. Copy migration pattern from E2-P3-T1 (mcp validate), adapt to OpenAPI types
  2. Rich errors (IOpenApiCollectionValidationError) -> stdout via `PrintOpenApiCollectionValidationErrors`
  3. `dotnet build` to verify
  4. Write subscription test suite mirroring mcp validate
  5. Run `dotnet test --filter "FullyQualifiedName~ValidateOpenApiCollectionCommandTests"`
- **Est. tests**: 18-25

#### Task E2-P3-T4: openapi publish
- **Command**: `PublishOpenApiCollectionCommand`
- **Tier**: C
- **Agent**: OpenAPI agent
- **Dependencies**: E2-P3-T2 (mcp publish)
- **Files to modify**: `src/CommandLine/Commands/OpenApi/PublishOpenApiCollectionCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/OpenApi/PublishOpenApiCollectionCommandTests.cs`
- **What to do**:
  1. Copy migration pattern from E2-P3-T2 (mcp publish), adapt to OpenAPI types
  2. `dotnet build` to verify
  3. Write publish subscription test suite mirroring mcp publish
  4. Run `dotnet test --filter "FullyQualifiedName~PublishOpenApiCollectionCommandTests"`
- **Est. tests**: 22-30

---

## Epic 3: Stream 3 — Client + Mock + Stage (13 commands)

**Agent**: 1 agent

### Phase 1: Tier A Commands (6 commands, parallel within phase)

#### Task E3-P1-T1: client list-versions
- **Command**: `ListClientVersionsCommand`
- **Tier**: A
- **Agent**: Stream 3
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Clients/ListClientVersionsCommand.cs` (auth standardization)
- **Files to create**: `test/CommandLine.Tests/Commands/Clients/ListClientVersionsCommandTests.cs`
- **What to do**:
  1. Review and standardize auth pattern
  2. Write test suite: help, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write list-specific tests: success per mode, cursor pagination, empty results
  4. Run `dotnet test --filter "FullyQualifiedName~ListClientVersionsCommandTests"`
- **Est. tests**: 10-14

#### Task E3-P1-T2: client list-published-versions
- **Command**: `ListClientPublishedVersionsCommand`
- **Tier**: A
- **Agent**: Stream 3
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Clients/ListClientPublishedVersionsCommand.cs` (auth standardization)
- **Files to create**: `test/CommandLine.Tests/Commands/Clients/ListClientPublishedVersionsCommandTests.cs`
- **What to do**:
  1. Review and standardize auth pattern
  2. Write test suite: help, no-auth (Theory x3), client exception (Theory x3), auth exception (Theory x3)
  3. Write list-specific tests: success per mode, cursor pagination, empty results
  4. Run `dotnet test --filter "FullyQualifiedName~ListClientPublishedVersionsCommandTests"`
- **Est. tests**: 10-14

#### Task E3-P1-T3: client download
- **Command**: `DownloadClientCommand`
- **Tier**: A
- **Agent**: Stream 3
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Clients/DownloadClientCommand.cs` (auth standardization)
- **Files to create**: `test/CommandLine.Tests/Commands/Clients/DownloadClientCommandTests.cs`
- **What to do**:
  1. Review and standardize auth pattern
  2. Write test suite: help, no-auth, client exception, auth exception
  3. Write download-specific tests: success with output path, default output, file overwrite
  4. Run `dotnet test --filter "FullyQualifiedName~DownloadClientCommandTests"`
- **Est. tests**: 12-14

#### Task E3-P1-T4: client upload
- **Command**: `UploadClientCommand`
- **Tier**: A
- **Agent**: Stream 3
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Clients/UploadClientCommand.cs` (auth standardization)
- **Files to create**: `test/CommandLine.Tests/Commands/Clients/UploadClientCommandTests.cs`
- **What to do**:
  1. Review and standardize auth pattern
  2. Write test suite: help, no-auth, client exception, auth exception
  3. Write upload-specific tests: success with file, missing file error
  4. Run `dotnet test --filter "FullyQualifiedName~UploadClientCommandTests"`
- **Est. tests**: 12-16

#### Task E3-P1-T5: mock list
- **Command**: `ListMockCommand`
- **Tier**: A
- **Agent**: Stream 3
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Mocks/ListMockCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Mocks/ListMockCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help, no-auth, client exception, auth exception
  3. Write list-specific tests: success per mode, cursor pagination, empty results
  4. Run `dotnet test --filter "FullyQualifiedName~ListMockCommandTests"`
- **Est. tests**: 10-14

#### Task E3-P1-T6: stage list
- **Command**: `ListStagesCommand`
- **Tier**: A
- **Agent**: Stream 3
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Stages/ListStagesCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Stages/ListStagesCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help, no-auth, client exception, auth exception
  3. Write list-specific tests: success per mode, cursor pagination, empty results
  4. Run `dotnet test --filter "FullyQualifiedName~ListStagesCommandTests"`
- **Est. tests**: 10-14

### Phase 2: Tier B Commands — Migrate + Test (4 commands, parallel within phase)

**Dependencies**: E3-P1 complete.

#### Task E3-P2-T1: client unpublish
- **Command**: `UnpublishClientCommand`
- **Tier**: B
- **Agent**: Stream 3
- **Dependencies**: E3-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Clients/UnpublishClientCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Clients/UnpublishClientCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity
  4. `dotnet build` to verify
  5. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  6. Run `dotnet test --filter "FullyQualifiedName~UnpublishClientCommandTests"`
- **Est. tests**: 12-16

#### Task E3-P2-T2: mock create
- **Command**: `CreateMockCommand`
- **Tier**: B
- **Agent**: Stream 3
- **Dependencies**: E3-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Mocks/CreateMockCommand.cs`
- **Files to create**:
  - `test/CommandLine.Tests/Commands/Mocks/CreateMockCommandTests.cs`
  - `test/CommandLine.Tests/Commands/Mocks/MockCommandTestHelper.cs` (if payloads reused)
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity
  4. `dotnet build` to verify
  5. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  6. Note: uses `IApisClient` as secondary client for API selection
  7. Run `dotnet test --filter "FullyQualifiedName~CreateMockCommandTests"`
- **Est. tests**: 15-18

#### Task E3-P2-T3: mock update
- **Command**: `UpdateMockCommand`
- **Tier**: B
- **Agent**: Stream 3
- **Dependencies**: E3-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Mocks/UpdateMockCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Mocks/UpdateMockCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity
  4. `dotnet build` to verify
  5. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  6. Run `dotnet test --filter "FullyQualifiedName~UpdateMockCommandTests"`
- **Est. tests**: 12-16

#### Task E3-P2-T4: stage delete
- **Command**: `DeleteStageCommand`
- **Tier**: B
- **Agent**: Stream 3
- **Dependencies**: E3-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Stages/DeleteStageCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Stages/DeleteStageCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity
  4. `dotnet build` to verify
  5. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths, confirmation
  6. Note: uses `IApisClient` as secondary client
  7. Run `dotnet test --filter "FullyQualifiedName~DeleteStageCommandTests"`
- **Est. tests**: 12-16

### Phase 2b: Dedicated — stage edit (1 command, standalone)

**Dependencies**: E3-P2 complete (specifically E3-P2-T4 stage delete for pattern reference).

#### Task E3-P2b-T1: stage edit (HIGH RISK)
- **Command**: `EditStagesCommand`
- **Tier**: B
- **Agent**: Stream 3
- **Dependencies**: E3-P2 (all)
- **Files to modify**: `src/CommandLine/Commands/Stages/EditStagesCommand.cs`
- **Files to create**:
  - `test/CommandLine.Tests/Commands/Stages/EditStagesCommandTests.cs`
  - `test/CommandLine.Tests/Commands/Stages/StageCommandTestHelper.cs` (complex payloads)
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity
  4. `dotnet build` to verify
  5. Write test suite — break into sub-areas:
     a. Standard tests: help, no-auth, client exception, auth exception
     b. JSON config path tests: valid config, invalid config, missing file
     c. Interactive UI flow tests: SelectableTable navigation, add/remove/modify stages
     d. Mutation error branches per mode
     e. Success paths per mode
  6. Note: most complex interactive command — uses local record types, file-scoped static extensions, `IApisClient` secondary
  7. Run `dotnet test --filter "FullyQualifiedName~EditStagesCommandTests"`
- **Est. tests**: 20-28

### Phase 3: Tier C Subscription Commands (2 commands, sequential)

**Dependencies**: E3-P2b complete AND **E1-P3-T1** (cross-epic gate).

#### Task E3-P3-T1: client validate
- **Command**: `ValidateClientCommand`
- **Tier**: C
- **Agent**: Stream 3
- **Dependencies**: E3-P2b (all), E1-P3-T1 (cross-epic gate)
- **Files to modify**: `src/CommandLine/Commands/Clients/ValidateClientCommand.cs`
- **Files to create**:
  - `test/CommandLine.Tests/Commands/Clients/ValidateClientCommandTests.cs`
  - `test/CommandLine.Tests/Commands/Clients/ClientCommandTestHelper.cs` (subscription event factories)
- **What to do**:
  1. Migrate: replace `PrintMutationErrorsAndExit` (initial mutation) + `PrintMutationErrors` (subscription) with typed switches
  2. Rich errors -> stdout; simple errors -> stderr
  3. Activity lifecycle on all paths
  4. `dotnet build` to verify
  5. Write full subscription test suite (validate matrix)
  6. Run `dotnet test --filter "FullyQualifiedName~ValidateClientCommandTests"`
- **Est. tests**: 18-25

#### Task E3-P3-T2: client publish
- **Command**: `PublishClientCommand`
- **Tier**: C
- **Agent**: Stream 3
- **Dependencies**: E3-P3-T1
- **Files to modify**: `src/CommandLine/Commands/Clients/PublishClientCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Clients/PublishClientCommandTests.cs`
- **What to do**:
  1. Migrate subscription handler with full publish state handling
  2. Activity lifecycle on all paths
  3. `dotnet build` to verify
  4. Write full publish subscription test suite (validate + queue/approval/deployment)
  5. Run `dotnet test --filter "FullyQualifiedName~PublishClientCommandTests"`
- **Est. tests**: 22-30

---

## Epic 4: Stream 4 — Fusion (10 commands)

**Agent**: 1 agent

### Phase 1: Tier A Commands (2 commands, parallel within phase)

#### Task E4-P1-T1: fusion download
- **Command**: `FusionDownloadCommand`
- **Tier**: A
- **Agent**: Stream 4
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/FusionDownloadCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionDownloadCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help, no-auth, client exception, auth exception
  3. Write download-specific tests: success with output path, default output
  4. Run `dotnet test --filter "FullyQualifiedName~FusionDownloadCommandTests"`
- **Est. tests**: 10-14

#### Task E4-P1-T2: fusion settings set
- **Command**: `FusionSettingsSetCommand`
- **Tier**: A
- **Agent**: Stream 4
- **Dependencies**: E0-P1-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/FusionSettingsSetCommand.cs` (if auth standardization needed)
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionSettingsSetCommandTests.cs`
- **What to do**:
  1. Review command against COMMAND_IMPLEMENTATION_GUIDELINES.md
  2. Write test suite: help, no-auth, client exception, auth exception
  3. Write success path tests per mode
  4. Run `dotnet test --filter "FullyQualifiedName~FusionSettingsSetCommandTests"`
- **Est. tests**: 8-12

### Phase 2: Tier B Commands — FusionPublishHelpers + 6 Commands

**Dependencies**: E4-P1 complete.
**CRITICAL**: T1 (FusionPublishHelpers migration) must complete before T2-T6 start.

#### Task E4-P2-T1: FusionPublishHelpers migration (PREREQUISITE)
- **Command**: N/A (shared helper)
- **Tier**: B (infrastructure)
- **Agent**: Stream 4
- **Dependencies**: E4-P1 (all)
- **Files to modify**: `src/CommandLine/Commands/Fusion/FusionPublishHelpers.cs`
- **Files to create**: None
- **What to do**:
  1. Replace 3 `PrintMutationErrorsAndExit` calls with typed error switches
  2. Replace 1 `PrintMutationErrors` call with inline `foreach` + `switch`
  3. Ensure activity Fail/Success semantics throughout
  4. `dotnet build` to verify all 5 consuming commands still compile
- **Est. tests**: 0 (tested via consuming commands)

#### Task E4-P2-T2: fusion upload
- **Command**: `FusionUploadCommand`
- **Tier**: B
- **Agent**: Stream 4
- **Dependencies**: E4-P2-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/FusionUploadCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionUploadCommandTests.cs`
- **What to do**:
  1. Migrate: standardize `ExecuteAsync`, add `AssertHasAuthentication`
  2. Replace `PrintMutationErrorsAndExit` with typed error switch
  3. Wrap mutation in activity
  4. `dotnet build` to verify
  5. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  6. Run `dotnet test --filter "FullyQualifiedName~FusionUploadCommandTests"`
- **Est. tests**: 12-16

#### Task E4-P2-T3: fusion publish begin
- **Command**: `FusionConfigurationPublishBeginCommand`
- **Tier**: B
- **Agent**: Stream 4
- **Dependencies**: E4-P2-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishBeginCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionConfigurationPublishBeginCommandTests.cs`
- **What to do**:
  1. Review command — uses `FusionPublishHelpers` (already migrated)
  2. Standardize `ExecuteAsync` if needed, add auth
  3. `dotnet build` to verify
  4. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  5. Test `FusionConfigurationPublishingState` file-based state (mock `IFileSystem`)
  6. Run `dotnet test --filter "FullyQualifiedName~FusionConfigurationPublishBeginCommandTests"`
- **Est. tests**: 12-16

#### Task E4-P2-T4: fusion publish start
- **Command**: `FusionConfigurationPublishStartCommand`
- **Tier**: B
- **Agent**: Stream 4
- **Dependencies**: E4-P2-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishStartCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionConfigurationPublishStartCommandTests.cs`
- **What to do**:
  1. Review command — uses `FusionPublishHelpers` (already migrated)
  2. Standardize `ExecuteAsync` if needed, add auth
  3. `dotnet build` to verify
  4. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  5. Test state management (mock `IFileSystem`)
  6. Run `dotnet test --filter "FullyQualifiedName~FusionConfigurationPublishStartCommandTests"`
- **Est. tests**: 12-16

#### Task E4-P2-T5: fusion publish commit
- **Command**: `FusionConfigurationPublishCommitCommand`
- **Tier**: B
- **Agent**: Stream 4
- **Dependencies**: E4-P2-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishCommitCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionConfigurationPublishCommitCommandTests.cs`
- **What to do**:
  1. Review command — uses `FusionPublishHelpers` (already migrated)
  2. Standardize `ExecuteAsync` if needed, add auth
  3. `dotnet build` to verify
  4. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  5. Test state management (mock `IFileSystem`)
  6. Run `dotnet test --filter "FullyQualifiedName~FusionConfigurationPublishCommitCommandTests"`
- **Est. tests**: 12-16

#### Task E4-P2-T6: fusion publish cancel
- **Command**: `FusionConfigurationPublishCancelCommand`
- **Tier**: B
- **Agent**: Stream 4
- **Dependencies**: E4-P2-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishCancelCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionConfigurationPublishCancelCommandTests.cs`
- **What to do**:
  1. Review command — uses `FusionPublishHelpers` (already migrated)
  2. Standardize `ExecuteAsync` if needed, add auth
  3. `dotnet build` to verify
  4. Write test suite: help, no-auth, client exception, auth exception, mutation errors, success paths
  5. Test state management (mock `IFileSystem`)
  6. Run `dotnet test --filter "FullyQualifiedName~FusionConfigurationPublishCancelCommandTests"`
- **Est. tests**: 10-14

### Phase 3: Tier C Subscription Commands (3 commands, sequential)

**Dependencies**: E4-P2 complete AND **E1-P3-T1** (cross-epic gate).

#### Task E4-P3-T1: fusion validate (HIGH RISK)
- **Command**: `FusionValidateCommand`
- **Tier**: C
- **Agent**: Stream 4
- **Dependencies**: E4-P2 (all), E1-P3-T1 (cross-epic gate)
- **Files to modify**: `src/CommandLine/Commands/Fusion/FusionValidateCommand.cs`
- **Files to create**:
  - `test/CommandLine.Tests/Commands/Fusion/FusionValidateCommandTests.cs`
  - `test/CommandLine.Tests/Commands/Fusion/FusionCommandTestHelper.cs` (subscription event factories)
- **What to do**:
  1. Migrate: replace `PrintMutationErrorsAndExit` + `PrintMutationErrors` with typed switches
  2. Handle two-level activity tracking (compose + validate)
  3. Rich errors -> stdout; simple errors -> stderr
  4. Activity lifecycle on all paths
  5. `dotnet build` to verify
  6. Write test suite:
     a. Standard: help, no-auth, client exception, auth exception
     b. Compose failure paths (reference `FusionComposeCommandTests`)
     c. Full subscription validate matrix
     d. Compose + subscribe combined flow
  7. Run `dotnet test --filter "FullyQualifiedName~FusionValidateCommandTests"`
- **Est. tests**: 22-30

#### Task E4-P3-T2: fusion publish
- **Command**: `FusionPublishCommand`
- **Tier**: B+C
- **Agent**: Stream 4
- **Dependencies**: E4-P3-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/FusionPublishCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionPublishCommandTests.cs`
- **What to do**:
  1. Migrate: compose + helpers + subscription — combine patterns
  2. Activity lifecycle on all paths
  3. `dotnet build` to verify
  4. Write test suite:
     a. Standard: help, no-auth, client exception, auth exception
     b. Compose failure paths
     c. Full publish subscription matrix (validate + queue/approval/deployment)
     d. Combined compose + publish flow
  5. Run `dotnet test --filter "FullyQualifiedName~FusionPublishCommandTests"`
- **Est. tests**: 25-35

#### Task E4-P3-T3: fusion publish validate (HIGH RISK)
- **Command**: `FusionConfigurationPublishValidateCommand`
- **Tier**: C
- **Agent**: Stream 4
- **Dependencies**: E4-P3-T1
- **Files to modify**: `src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishValidateCommand.cs`
- **Files to create**: `test/CommandLine.Tests/Commands/Fusion/FusionConfigurationPublishValidateCommandTests.cs`
- **What to do**:
  1. Migrate: only `PrintMutationErrorsAndExit` (initial mutation) + `PrintMutationErrors` (subscription failure)
  2. **Keep `throw Exit(...)` calls** for pipeline state errors (queued, already failed, already published) — these are guard clauses
  3. Activity lifecycle on all paths
  4. `dotnet build` to verify
  5. Write test suite:
     a. Standard: help, no-auth, client exception, auth exception
     b. Each ExitException path (queued, already failed, already published)
     c. Full subscription validate/publish matrix
     d. State management (mock `IFileSystem`)
  6. Run `dotnet test --filter "FullyQualifiedName~FusionConfigurationPublishValidateCommandTests"`
- **Est. tests**: 22-30

---

## Epic 5: Final Cleanup

**Dependencies**: ALL of Epics 1-4 complete.

### Phase 1: Delete Obsolete Methods

#### Task E5-P1-T1: Remove obsolete ConsoleHelpers methods
- **Command**: N/A (infrastructure)
- **Tier**: N/A
- **Agent**: Any agent
- **Dependencies**: E1 (all), E2 (all), E3 (all), E4 (all)
- **Files to modify**: `src/CommandLine/Helpers/ConsoleHelpers.cs`
- **Files to create**: None
- **What to do**:
  1. Delete `PrintMutationErrorsAndExit<T>` method
  2. Delete `PrintMutationErrors<T>` method
  3. Delete `PrintMutationError(object)` dispatcher method
  4. Verify no remaining callers: `grep -r "PrintMutationErrorsAndExit\|PrintMutationErrors\b" src/`
  5. `dotnet build` to verify clean compile
  6. Run full test suite: `dotnet test` on all CommandLine.Tests
- **Est. tests**: 0 (verification only)

#### Task E5-P1-T2: Update progress tracker
- **Command**: N/A (documentation)
- **Tier**: N/A
- **Agent**: Any agent
- **Dependencies**: E5-P1-T1
- **Files to modify**: `test/CommandLine.Tests/COMMAND_TEST_MIGRATION_PROGRESS.md`
- **Files to create**: None
- **What to do**:
  1. Mark all 47 commands as `done` in the progress table
  2. Verify final test count
  3. Final `dotnet test` pass to confirm everything green
- **Est. tests**: 0

---

## Summary Statistics

| Epic | Phase | Tasks | Commands | Est. Tests |
|------|-------|-------|----------|-----------|
| E0 | P1 | 1 | 0 (infra) | 0 |
| E1 | P1 | 7 | 7 | 72-96 |
| E1 | P2 | 3 | 3 | 42-54 |
| E1 | P3 | 2 | 2 | 40-55 |
| E2 | P1 | 2 | 2 | 20-28 |
| E2 | P2 | 6 | 6 | 78-104 |
| E2 | P3 | 4 | 4 | 80-110 |
| E3 | P1 | 6 | 6 | 64-86 |
| E3 | P2 | 4 | 4 | 51-66 |
| E3 | P2b | 1 | 1 | 20-28 |
| E3 | P3 | 2 | 2 | 40-55 |
| E4 | P1 | 2 | 2 | 18-26 |
| E4 | P2 | 6 | 5+infra | 58-78 |
| E4 | P3 | 3 | 3 | 69-95 |
| E5 | P1 | 2 | 0 (cleanup) | 0 |
| **Total** | | **51** | **47 commands + 4 infra** | **~652-881** |

## Critical Path

The longest dependency chain determines minimum time:

```
E0-P1-T1 (ConsoleHelpers)
  -> E1-P1 (any Tier A, parallel)
    -> E1-P2 (Tier B, parallel)
      -> E1-P3-T1 (ValidateSchema — CROSS-EPIC GATE)
        -> E1-P3-T2 (PublishSchema)
          -> E5-P1-T1 (Final cleanup)
```

All other epics can run in parallel with E1, but their Phase 3 tasks must wait for E1-P3-T1.

## Agent Assignment Summary

| Agent | Epic | Scope | Total Commands |
|-------|------|-------|---------------|
| Stream 1 | E0 + E1 | ConsoleHelpers + Schema + Workspace + PAT | 12 + prep |
| MCP agent | E2 (lead) | MCP commands | 6 |
| OpenAPI agent | E2 (trail) | OpenAPI commands (copies MCP patterns) | 6 |
| Stream 3 | E3 | Client + Mock + Stage | 13 |
| Stream 4 | E4 | Fusion | 10 |
| Any | E5 | Final cleanup | 0 (verification) |
