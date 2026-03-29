# Nitro CLI Command Test Research

## Status Snapshot (2026-03-29)

- **Total leaf commands**: 49
- **Tests completed**: 17 (35%) — api-key (3), api (5), client (4), environment (3), fusion (2)
- **Tests not started**: 32 (65%)
- **~200+ test methods** across completed suites

---

## Test-Rule Checklist

Every command test suite must satisfy ALL items before marking `done`.

### Prerequisites (Command Implementation)

- [ ] Command follows `COMMAND_IMPLEMENTATION_GUIDELINES.md`
- [ ] Uses `SetActionWithExceptionHandling` (not raw `SetAction`)
- [ ] Calls `AddGlobalNitroOptions()` after command-specific options
- [ ] Uses `AssertHasAuthentication` at top of `ExecuteAsync` (if auth required)
- [ ] Uses typed mutation error switch (not `PrintMutationErrorsAndExit`)
- [ ] Uses `activity.Fail()` / `activity.Success(...)` correctly
- [ ] All error messages follow formatting rules (`'--option-name'`, sentences with periods)
- [ ] Uses `ExitCodes.Success` / `ExitCodes.Error` (no raw integers)

### Test Structure & Naming

- [ ] Test class: `{CommandName}CommandTests`
- [ ] Test names: `<ConditionOrInput>_Return<Outcome>[_<Mode>]`
- [ ] Mode suffixes: `_Interactive`, `_NonInteractive`, `_JsonOutput`
- [ ] Exception tests: `ClientThrowsException_ReturnsError[_<Mode>]`
- [ ] Auth exception tests: `ClientThrowsAuthorizationException_ReturnsError[_<Mode>]`
- [ ] Mutation error tests: `MutationReturns<BranchName>Error_ReturnsError_<Mode>`
- [ ] Helper file: `{CommandName}CommandTestHelper.cs` (when payloads reused 3+ times)

### Mandatory Coverage (All Command Types)

- [ ] **Help output snapshot** — always first test; uses `AssertHelpOutput()`
- [ ] **No auth (Theory × 3 modes)** — `NoSession_Or_ApiKey_ReturnsError`
- [ ] **Client exception (Theory × 3 modes)** — `ClientThrowsException_ReturnsError`
- [ ] **Authorization exception (Theory × 3 modes)** — `ClientThrowsAuthorizationException_ReturnsError`

### Additional Coverage by Command Type

#### Create / Mutation Commands

- [ ] Missing required option (NonInteractive + JsonOutput) → error
- [ ] Missing required option with prompting (Interactive) → user input → success
- [ ] Success with all options (NonInteractive, JsonOutput, Interactive)
- [ ] Each mutation error branch × 3 modes (separate Fact per mode, shared MemberData)
- [ ] Parser-level required-option test (consolidated)

#### List Commands

- [ ] Success with workspace ID (all 3 modes)
- [ ] Success with session workspace (all 3 modes)
- [ ] Cursor pagination (NonInteractive + JsonOutput)
- [ ] Empty results (all 3 modes)

#### Delete / Show Commands

- [ ] Missing resource ID (NonInteractive + JsonOutput) → error
- [ ] Success (all 3 modes)
- [ ] Resource not found (all 3 modes)

---

## Test Infrastructure

### CommandBuilder (Fluent Test Harness)

```csharp
var result = await new CommandBuilder()
    .AddService(client.Object)        // inject mock client
    .AddApiKey()                      // or .AddSession() / .AddSessionWithWorkspace()
    .AddInteractionMode(mode)         // Interactive / NonInteractive / JsonOutput
    .AddArguments("cmd", "sub", ...)  // command + options
    .ExecuteAsync();                  // or .Start() for interactive
```

### Assertion Helpers

| Method | Checks |
|--------|--------|
| `result.AssertSuccess(stdout)` | StdErr empty, StdOut matches snapshot, ExitCode == 0 |
| `result.AssertError(stderr)` | StdOut empty, StdErr matches snapshot, ExitCode == 1 |
| `result.AssertHelpOutput(stdout)` | StdErr empty, StdOut matches (exe normalized), ExitCode == 0 |

For mutation errors where both stdout and stderr have content:
```csharp
result.StdOut.MatchInlineSnapshot("""...""");
result.StdErr.MatchInlineSnapshot("""...""");
Assert.Equal(1, result.ExitCode);
```

### Interactive Testing

```csharp
var command = new CommandBuilder()
    .AddService(client.Object)
    .AddInteractionMode(InteractionMode.Interactive)
    .AddArguments(...)
    .Start();  // returns InteractiveCommand

command.Input("text");           // type + Enter
command.SelectOption(1);         // arrow down N + Enter
command.Confirm(true);           // "y" or "n"
var result = await command.RunToCompletionAsync();
```

### Mock Patterns

- **Always** `MockBehavior.Strict` + `client.VerifyAll()`
- Inline mocks for simple payloads
- Extract to `*CommandTestHelper.cs` when reused 3+ or deeply nested (5+ mocks)

---

## 7 Canonical Test Patterns

| # | Pattern | When | Method Type |
|---|---------|------|-------------|
| 1 | Auth/validation error across modes | Error output identical across modes | Theory × 3 |
| 2 | Success path per mode | Happy path with mutation | Fact per mode |
| 3 | Interactive prompting | Missing options trigger prompts | Fact (Interactive only) |
| 4 | Mutation error branches | Per error type × per mode | Fact per combo |
| 5 | Client exception | `NitroClientException` thrown | Theory × 3 |
| 6 | Help snapshot | First test in every suite | Fact |
| 7 | Cursor pagination | List commands with `--cursor` | Theory (NI + JSON) |

---

## Missing Test Suites (32 Commands)

### Client Commands (7 remaining)
| Command | Class | Complexity |
|---------|-------|------------|
| client download | `DownloadClientCommand` | File I/O + stream |
| client list-published-versions | `ListClientPublishedVersionsCommand` | List pattern |
| client list-versions | `ListClientVersionsCommand` | List pattern |
| client publish | `PublishClientCommand` | Mutation |
| client unpublish | `UnpublishClientCommand` | Mutation |
| client upload | `UploadClientCommand` | File upload |
| client validate | `ValidateClientCommand` | File validation |

### Fusion Commands (8 remaining)
| Command | Class | Complexity |
|---------|-------|------------|
| fusion download | `FusionDownloadCommand` | File I/O |
| fusion publish | `FusionPublishCommand` | Multi-step |
| fusion run | `FusionRunCommand` | Process management |
| fusion settings set | `FusionSettingsSetCommand` | Simple mutation |
| fusion upload | `FusionUploadCommand` | File upload |
| fusion validate | `FusionValidateCommand` | File validation |
| fusion publish begin/start/validate/commit/cancel | 5 classes | Complex multi-step flow |

### Schema Commands (4)
| Command | Class | Complexity |
|---------|-------|------------|
| schema download | `DownloadSchemaCommand` | File I/O |
| schema publish | `PublishSchemaCommand` | Mutation |
| schema upload | `UploadSchemaCommand` | File upload |
| schema validate | `ValidateSchemaCommand` | File validation |

### MCP Commands (6)
| Command | Class | Complexity |
|---------|-------|------------|
| mcp create | `CreateMcpFeatureCollectionCommand` | Mutation |
| mcp delete | `DeleteMcpFeatureCollectionCommand` | Mutation |
| mcp list | `ListMcpFeatureCollectionCommand` | List pattern |
| mcp publish | `PublishMcpFeatureCollectionCommand` | Mutation |
| mcp upload | `UploadMcpFeatureCollectionCommand` | File upload |
| mcp validate | `ValidateMcpFeatureCollectionCommand` | File validation |

### OpenAPI Commands (6)
| Command | Class | Complexity |
|---------|-------|------------|
| openapi create | `CreateOpenApiCollectionCommand` | Mutation |
| openapi delete | `DeleteOpenApiCollectionCommand` | Mutation |
| openapi list | `ListOpenApiCollectionCommand` | List pattern |
| openapi publish | `PublishOpenApiCollectionCommand` | Mutation |
| openapi upload | `UploadOpenApiCollectionCommand` | File upload |
| openapi validate | `ValidateOpenApiCollectionCommand` | File validation |

### Mock Commands (3)
| Command | Class | Complexity |
|---------|-------|------------|
| mock create | `CreateMockCommand` | Mutation + file I/O |
| mock list | `ListMockCommand` | List pattern |
| mock update | `UpdateMockCommand` | Mutation + file I/O |

### PAT Commands (3)
| Command | Class | Complexity |
|---------|-------|------------|
| pat create | `CreatePersonalAccessTokenCommand` | Mutation |
| pat list | `ListPersonalAccessTokenCommand` | List pattern |
| pat revoke | `RevokePersonalAccessTokenCommand` | Mutation |

### Stage Commands (3)
| Command | Class | Complexity |
|---------|-------|------------|
| stage delete | `DeleteStageCommand` | Mutation |
| stage edit | `EditStagesCommand` | Complex interactive |
| stage list | `ListStagesCommand` | List pattern |

### Workspace Commands (5)
| Command | Class | Complexity |
|---------|-------|------------|
| workspace create | `CreateWorkspaceCommand` | Mutation |
| workspace current | `CurrentWorkspaceCommand` | Query |
| workspace list | `ListWorkspaceCommand` | List pattern |
| workspace set-default | `SetDefaultWorkspaceCommand` | Mutation |
| workspace show | `ShowWorkspaceCommand` | Query |

### Standalone Commands (3)
| Command | Class | Complexity |
|---------|-------|------------|
| launch | `LaunchCommand` | Browser open (special) |
| login | `LoginCommand` | Auth flow (special) |
| logout | `LogoutCommand` | Session clear (simple) |

---

## Suggested Wave Order

| Wave | Commands | Count | Rationale |
|------|----------|-------|-----------|
| 5 | Schema (download, publish, upload, validate) | 4 | Already aligned to guidelines per migration doc |
| 6 | Workspace (create, current, list, set-default, show) | 5 | Standard CRUD, well-understood patterns |
| 7 | Stage (delete, edit, list) | 3 | Small batch, `edit` is complex interactive |
| 8 | PAT (create, list, revoke) | 3 | Standard mutation/list |
| 9 | Mock (create, list, update) | 3 | Hidden commands, file I/O |
| 10 | Client remaining (download, publish, upload, validate, list-versions, list-published-versions, unpublish) | 7 | File I/O heavy |
| 11 | MCP (create, delete, list, publish, upload, validate) | 6 | Parallel to OpenAPI |
| 12 | OpenAPI (create, delete, list, publish, upload, validate) | 6 | Parallel to MCP |
| 13 | Fusion remaining (download, publish, run, settings set, upload, validate) | 6 | Complex, multi-step |
| 14 | Fusion publish flow (begin, start, validate, commit, cancel) | 5 | Most complex — stateful multi-step |
| 15 | Standalone (launch, login, logout) | 3 | Special-case testing |

---

## Subscription-Based Commands (Outdated Pattern)

Commands that use GraphQL subscriptions (`SubscribeTo*Async`) follow a **different lifecycle** than simple mutation commands. These are currently outdated and need migration.

### Identified Subscription Commands (10 total)

| Command | Class | Subscription Method | Current Issues |
|---------|-------|-------------------|----------------|
| schema validate | `ValidateSchemaCommand` | `SubscribeToSchemaValidationAsync` | Uses `PrintMutationErrors` for validation errors |
| schema publish | `PublishSchemaCommand` | `SubscribeToSchemaPublishAsync` | Uses `PrintMutationErrors` for publish errors |
| client validate | `ValidateClientCommand` | `SubscribeToClientValidationAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |
| client publish | `PublishClientCommand` | `SubscribeToClientPublishAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |
| openapi validate | `ValidateOpenApiCollectionCommand` | `SubscribeToOpenApiCollectionValidationAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |
| openapi publish | `PublishOpenApiCollectionCommand` | `SubscribeToOpenApiCollectionPublishAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |
| mcp validate | `ValidateMcpFeatureCollectionCommand` | `SubscribeToMcpFeatureCollectionValidationAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |
| mcp publish | `PublishMcpFeatureCollectionCommand` | `SubscribeToMcpFeatureCollectionPublishAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |
| fusion validate | `FusionValidateCommand` | `SubscribeToSchemaVersionValidationUpdatedAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |
| fusion publish validate | `FusionConfigurationPublishValidateCommand` | `SubscribeToFusionConfigurationPublishingTaskChangedAsync` | Uses `PrintMutationErrorsAndExit` + `PrintMutationErrors` |

### Current (Outdated) Subscription Pattern

```csharp
// OUTDATED — shows what needs to change
await using (var activity = console.StartActivity("Validating..."))
{
    var request = await client.StartValidationAsync(...);
    console.PrintMutationErrorsAndExit(request.Errors);  // ❌ Legacy: stdout, no activity.Fail()

    await foreach (var update in client.SubscribeToValidationAsync(requestId, ct))
    {
        switch (update)
        {
            case IValidationFailed { Errors: var errors }:
                console.WriteLine("Invalid:");              // ❌ No activity.Fail()
                console.PrintMutationErrors(errors);        // ❌ Writes to stdout
                return ExitCodes.Error;

            case IValidationSuccess:
                console.Success("Succeeded");               // ❌ No activity.Success()
                return ExitCodes.Success;
        }
    }
}
```

### Target Pattern for Subscription Commands

The activity must wrap the entire subscription lifecycle. On validation failure:

1. Call `activity.Fail()` to stop the spinner and mark failed
2. For **rich validation errors** (e.g., `IMcpFeatureCollectionValidationError`, `IOpenApiCollectionValidationError`, `IInvalidGraphQLSchemaError`), call `console.PrintMutationError(error)` to render the Spectre.Console tree to **stdout**
3. Print a summary error message to **stderr** via `console.Error.WriteLineAsync(...)`
4. Return `ExitCodes.Error`

On success:
1. Call `activity.Success("message")` explicitly

```csharp
// TARGET pattern for subscription commands
await using (var activity = console.StartActivity("Validating..."))
{
    // 1. Start the operation (mutation)
    var request = await client.StartValidationAsync(...);

    if (request.Errors?.Count > 0)
    {
        activity.Fail();
        foreach (var error in request.Errors)
        {
            // typed switch for each error in the .graphql file
            var errorMessage = error switch { ... };
            await console.Error.WriteLineAsync(errorMessage);
        }
        return ExitCodes.Error;
    }

    activity.Update($"Request created (ID: {requestId.EscapeMarkup()})");

    // 2. Subscribe to updates
    await foreach (var update in client.SubscribeToValidationAsync(requestId, ct))
    {
        switch (update)
        {
            case IValidationFailed { Errors: var errors }:
                activity.Fail();

                foreach (var error in errors)
                {
                    // Rich errors → render tree to stdout via PrintMutationError
                    console.PrintMutationError(error);
                }

                // Summary error to stderr
                await console.Error.WriteLineAsync("Validation failed.");
                return ExitCodes.Error;

            case IValidationSuccess:
                activity.Success("Validation succeeded.");
                return ExitCodes.Success;

            case IOperationInProgress:
            case IValidationInProgress:
                activity.Update("Validation in progress...");
                break;

            case IProcessingTaskIsQueued v:
                activity.Update($"Queued at position {v.QueuePosition}.");
                break;

            default:
                activity.Update(
                    "Unknown server response. Ensure your CLI is on the latest version.");
                break;
        }
    }

    activity.Fail();
}
return ExitCodes.Error;
```

### Key Rules for Subscription Error Handling

1. **`PrintMutationErrorsAndExit` is banned** — replace with typed switch + `activity.Fail()` + stderr
2. **`activity.Fail()` before any error output** — stops the spinner
3. **Rich validation errors go to stdout** via `console.PrintMutationError(error)` — these render Spectre.Console trees (schema changes, entity validation, etc.)
4. **Summary error message goes to stderr** — `await console.Error.WriteLineAsync(...)` after the rich output
5. **Exit with `ExitCodes.Error`** (value 1) after validation failure
6. **`activity.Success("...")` on success** — never leave activity without terminal state
7. **Every subscription path must terminate the activity** — either `.Fail()` or `.Success()`

---

## PrintMutationError — Rich Error Rendering

`console.PrintMutationError(error)` is a **dispatch method** that pattern-matches on the error type and renders rich Spectre.Console output (trees, colored markup) to **stdout**. Use it for validation errors that have structured nested data.

### Error Types with Rich Rendering (use PrintMutationError)

| Error Type | Renders |
|-----------|---------|
| `IMcpFeatureCollectionValidationError` | Tree: Collections → Entities (Prompts/Tools) → Errors with line:column |
| `IOpenApiCollectionValidationError` | Tree: Collections → Entities (Endpoints/Models) → Errors with line:column |
| `ISchemaVersionChangeViolationError` | Tree: Schema changes |
| `ISchemaChangeViolationError` | Tree: Schema changes |
| `IPersistedQueryValidationError` | Tree: Queries → Errors with deployed tags and locations |
| `IInvalidGraphQLSchemaError` | Tree: GraphQL errors with codes |
| `IStagesHavePublishedDependenciesError` | List: Stages with published schemas/clients |

### Error Types with Simple Messages (handle inline)

| Error Type | Handling |
|-----------|---------|
| `IApiNotFoundError` | `err.Message` → stderr |
| `IStageNotFoundError` | `err.Message` → stderr |
| `ISchemaNotFoundError` | `err.Message` → stderr |
| `IUnauthorizedOperation` | `err.Message` → stderr |
| `IConcurrentOperationError` | `err.Message` → stderr |
| `IProcessingTimeoutError` | `err.Message` → stderr |
| `IValidationError` | `err.Message` → stderr |
| `ISubgraphInvalidError` | `err.Message` → stderr |

### When to Use Which

- **Rich errors (PrintMutationError to stdout)**: When the error has nested structure (collections, entities, changes, queries) that benefits from tree rendering
- **Simple errors (inline to stderr)**: When the error is a single message (not found, unauthorized, timeout, etc.)
- **Both in same command**: A subscription failure may contain a mix — iterate errors, dispatch rich ones to `PrintMutationError`, simple ones to stderr

---

## Subscription Command Test Checklist (Extension)

In addition to the base test-rule checklist, subscription commands need:

- [ ] **Initial mutation error branches** — typed switch (same as regular mutation commands)
- [ ] **Subscription success path** — `IValidationSuccess` / `IPublishSuccess`
- [ ] **Subscription validation failure** — rich errors rendered + stderr summary + exit code 1
- [ ] **Subscription in-progress states** — activity.Update called
- [ ] **Subscription queue states** (publish commands) — queue position shown
- [ ] **Subscription approval states** (publish commands) — approval message + deployment errors
- [ ] **Subscription unknown state** — default handler with upgrade message
- [ ] **Subscription ends without terminal state** — fallthrough to activity.Fail() + ExitCodes.Error
- [ ] **Activity lifecycle** — every path ends in activity.Fail() or activity.Success()

---

## Raw Research Files

Individual agent findings: `.work/research/`
- `guidelines-and-progress.md` — full guidelines + migration status
- `existing-tests.md` — test patterns with code examples
- `command-catalog.md` — all command implementations cataloged
- `subscription-commands.md` — all 10 subscription commands with full code
- `print-mutation-errors.md` — PrintMutationError/PrintMutationErrors implementations
- `validation-errors.md` — all validation error types and their structures
