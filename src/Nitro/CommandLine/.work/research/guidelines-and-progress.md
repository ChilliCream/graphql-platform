# Nitro CommandLine: Guidelines and Migration Progress

## Overview

This document synthesizes two key guidance files:

- **COMMAND_IMPLEMENTATION_GUIDELINES.md** — how commands should be implemented in code
- **COMMAND_TEST_MIGRATION_PROGRESS.md** — testing strategy and current status of all 60+ commands

---

## Part 1: Command Implementation Guidelines

### 1.1 Command Structure

Every command is a **sealed class inheriting from `Command`**.

**Template structure:**

```csharp
internal sealed class MyCommand : Command
{
    public MyCommand(
        INitroConsole console,
        IMyClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("my-command")
    {
        Description = "Does the thing.";

        Options.Add(Opt<MyNameOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(
            console,
            async (parseResult, cancellationToken)
                => await ExecuteAsync(
                    parseResult,
                    console,
                    client,
                    sessionService,
                    resultHolder,
                    cancellationToken));
    }

    private async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IMyClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

**Key principles:**

- Constructor wires up options, description, and action handler
- All logic lives in private `ExecuteAsync` method
- **Always use `sealed`** modifier on command classes

### 1.2 SetActionWithExceptionHandling

**Always use `this.SetActionWithExceptionHandling(console, ...)` instead of `SetAction`.**

This provides uniform exception handling for:

- `ExitException` — writes message to stderr, returns exit code 1
- `NitroClientAuthorizationException` — writes permission error to stderr, returns exit code 1
- `NitroClientException` — writes server error to stderr, returns exit code 1
- Cancellation (CancellationToken) — returns exit code 1 silently

### 1.3 AddGlobalNitroOptions

**Always call `this.AddGlobalNitroOptions()` after registering command-specific options.**

This adds the three options present on every command:

- `--cloud-url`
- `--api-key`
- `--output`

### 1.4 ExecuteAsync Structure (Top-to-Bottom Order)

1. **Assert authentication** — if the command requires an authenticated user
2. **Resolve input** — options and/or interactive prompts
3. **Execute async work** — wrapped in an activity (mutations only)
4. **Handle errors** — with typed error handling
5. **Mark activity success** — emit output, set result, return 0

### 1.5 Exit Codes

Always use the `ExitCodes` constants:

```csharp
return ExitCodes.Success;  // 0
return ExitCodes.Error;    // 1
```

Never return raw integers.

### 1.6 Authentication

Only commands requiring an authenticated user need to assert authentication. Skip this if the command can run without a session or API key.

When authentication is required, call at the **very top of ExecuteAsync**, before any options or prompts:

```csharp
parseResult.AssertHasAuthentication(sessionService);
```

This throws an `ExitException` if neither a session nor `--api-key` is present. `SetActionWithExceptionHandling` renders it automatically.

### 1.7 Error Messages

**Formatting rules:**

- **Option references**: `'--option-name'` (single-quoted, kebab-case)
- **Value references**: Quote value with single quotes: `'value'`
- **Sentence format**: End with period, proper English
- **Throw Exit(...)**: Use for guard-style errors inside ExecuteAsync

```csharp
// Correct
throw Exit("The '--workspace-id' or '--api-id' option is required in non-interactive mode.");
throw Exit($"The API with ID '{apiId}' was not found.");

// Incorrect
throw new Exception($"api {apiId} not found");
```

Import via: `using static ChilliCream.Nitro.CommandLine.ThrowHelper`

### 1.8 Input Resolution

**For required options:**

- Use `parseResult.GetValue(Opt<TOption>.Instance)`
- Throw `ExitException` before entering activity block if missing in non-interactive mode
- Never let missing required values surface as null inside activity

**For interactive fallbacks:**

- Check `console.IsInteractive` first
- Use `console.PromptAsync(...)` or `console.PromptForApiIdAsync(...)`

```csharp
if (workspaceId is null && apiId is null)
{
    if (!console.IsInteractive)
    {
        throw Exit(
            $"The '--workspace-id' or '--api-id' option is required in non-interactive mode.");
    }

    // interactive prompts here
}
```

### 1.9 Activities (Mutation Wrapper)

**Wrap async operations around GraphQL mutations** that do visible, state-changing work.

Use `await using` to ensure activity is disposed even on exceptions:

```csharp
await using (var activity = console.StartActivity("Creating API key..."))
{
    // ... async work ...

    activity.Success("Successfully created the API key!");
    // ...
    return ExitCodes.Success;
}
```

**Activity rules:**

- **Mutations only.** Do NOT wrap queries or subscriptions in activities
- **Call `activity.Fail()` before stderr writes** — stops spinner and marks failed
- **Always call `activity.Success("...")` explicitly** — activities are NOT implicitly successful on block exit
- **Never leave activity without terminal state** — every path must end in `activity.Fail()` OR `activity.Success(...)`

**Error path example:**

```csharp
if (data.Errors?.Count > 0)
{
    activity.Fail();

    foreach (var error in data.Errors)
    {
        await console.Error.WriteLineAsync(error.Message);
    }

    return ExitCodes.Error;
}
```

### 1.10 GraphQL Mutations — Typed Error Handling

**Strategy:**

1. Open the command's `.graphql` file (located under `src/CommandLine.Client/`)
2. Identify all error fragment spreads on the mutation's `errors` field
3. Handle every error type explicitly in the command

The generated client returns `data.Errors` as a list of union members.

**Pattern:**

```csharp
if (data.Errors?.Count > 0)
{
    activity.Fail();

    foreach (var error in data.Errors)
    {
        var errorMessage = error switch
        {
            IApiNotFoundError err                 => err.Message,
            IWorkspaceNotFound err                => err.Message,
            IPersonalWorkspaceNotSupportedError err => err.Message,
            IRoleNotFoundError err                => err.Message,
            IValidationError err                  => err.Message,
            IError err                            => "Unexpected mutation error: " + err.Message,
            _                                     => "Unexpected mutation error."
        };

        await console.Error.WriteLineAsync(errorMessage);
        return ExitCodes.Error;
    }
}
```

**Rules:**

- One `switch` arm per explicitly named fragment in `.graphql` file
- Include catch-all `IError` arm for `...Error` fragments or unrecognized errors
- Include `_` default arm as safety net
- Always call `activity.Fail()` before loop
- Return `ExitCodes.Error` after first error message

**Legacy pattern — DEPRECATED:**

- `console.PrintMutationErrorsAndExit(data.Errors)` is outdated
- Writes to stdout (not stderr)
- Does not call `activity.Fail()`
- Throws generic `ExitException` without message
- **Do not use in new commands** — replace with inline switch pattern

### 1.11 Result Output

After successful operation:

1. Call `activity.Success("...")`
2. Write blank line with `console.WriteLine()`
3. Set result on `resultHolder`
4. Return `ExitCodes.Success`

```csharp
activity.Success("Successfully created API key!");

console.WriteLine();

resultHolder.SetResult(new ObjectResult(new MyResult
{
    Id = result.Id,
}));

return ExitCodes.Success;
```

**Note:** `IResultHolder` writes the result in the format requested by `--output` option (plain text or JSON). Always populate for commands producing programmatically-consumable data.

### 1.12 Implementation Checklist

- [ ] `SetActionWithExceptionHandling` used (not raw `SetAction`)
- [ ] `AddGlobalNitroOptions` called after command-specific options
- [ ] `AssertHasAuthentication` is first call in `ExecuteAsync` (only if required)
- [ ] All option references in error messages use `'--option-name'` format
- [ ] All error messages are complete sentences ending with period
- [ ] `throw Exit(...)` used for guard errors (via `using static ThrowHelper`)
- [ ] All async work wrapped in `await using (var activity = ...)`
- [ ] `activity.Fail()` called before every stderr write inside activity
- [ ] `activity.Success("...")` called explicitly before returning `ExitCodes.Success`
- [ ] Every mutation error fragment from `.graphql` has corresponding switch arm
- [ ] Blank line written before setting result on `IResultHolder`
- [ ] All return paths use `ExitCodes.Success` or `ExitCodes.Error`

---

## Part 2: Command Test Migration Progress

### 2.1 Migration Status Overview

**Current focus:**

- Scope baseline established
- Existing baseline suites (reference): `api-key create`, `api-key delete`, `api-key list`
- API command migration wave completed: `api create|delete|list|show|set-settings`
- Schema commands aligned to COMMAND_IMPLEMENTATION_GUIDELINES.md (typed mutation errors, auth assertions, activity fail/success semantics) — ready for test-suite migration

### 2.2 Command Inventory & Status

**Legend:**

- `done` — command test suite added and reviewed against checklist
- `in-progress` — currently being implemented or reviewed
- `not-started` — not migrated yet
- `n/a` — group/root command with no direct behavior to test

#### Api Keys (3 commands)

| Command         | File                   | Status | Notes          |
| --------------- | ---------------------- | ------ | -------------- |
| api-key (group) | ApiKeyCommand.cs       | n/a    | Group command  |
| api-key create  | CreateApiKeyCommand.cs | done   | Baseline suite |
| api-key delete  | DeleteApiKeyCommand.cs | done   | Baseline suite |
| api-key list    | ListApiKeyCommand.cs   | done   | Baseline suite |

#### Apis (5 commands)

| Command          | File                     | Status | Notes         |
| ---------------- | ------------------------ | ------ | ------------- |
| api (group)      | ApiCommand.cs            | n/a    | Group command |
| api create       | CreateApiCommand.cs      | done   | Wave 1        |
| api delete       | DeleteApiCommand.cs      | done   | Wave 1        |
| api list         | ListApiCommand.cs        | done   | Wave 1        |
| api show         | ShowApiCommand.cs        | done   | Wave 1        |
| api set-settings | SetApiSettingsCommand.cs | done   | Wave 1        |

#### Clients (9 commands)

| Command                        | File                                  | Status      | Notes         |
| ------------------------------ | ------------------------------------- | ----------- | ------------- |
| client (group)                 | ClientCommand.cs                      | n/a         | Group command |
| client create                  | CreateClientCommand.cs                | done        | Wave 4        |
| client delete                  | DeleteClientCommand.cs                | done        | Wave 4        |
| client download                | DownloadClientCommand.cs              | not-started |               |
| client list                    | ListClientCommand.cs                  | done        | Wave 2        |
| client list-published-versions | ListClientPublishedVersionsCommand.cs | not-started |               |
| client list-versions           | ListClientVersionsCommand.cs          | not-started |               |
| client publish                 | PublishClientCommand.cs               | not-started |               |
| client show                    | ShowClientCommand.cs                  | done        | Wave 2        |
| client unpublish               | UnpublishClientCommand.cs             | not-started |               |
| client upload                  | UploadClientCommand.cs                | not-started |               |
| client validate                | ValidateClientCommand.cs              | not-started |               |

#### Environments (3 commands)

| Command             | File                        | Status | Notes         |
| ------------------- | --------------------------- | ------ | ------------- |
| environment (group) | EnvironmentCommand.cs       | n/a    | Group command |
| environment create  | CreateEnvironmentCommand.cs | done   | Wave 3        |
| environment list    | ListEnvironmentCommand.cs   | done   | Wave 3        |
| environment show    | ShowEnvironmentCommand.cs   | done   | Wave 3        |

#### Fusion (10 commands)

| Command                 | File                        | Status      | Notes         |
| ----------------------- | --------------------------- | ----------- | ------------- |
| fusion (group)          | FusionCommand.cs            | n/a         | Group command |
| fusion compose          | FusionComposeCommand.cs     | done        |               |
| fusion download         | FusionDownloadCommand.cs    | not-started |               |
| fusion migrate          | FusionMigrateCommand.cs     | done        |               |
| fusion publish          | FusionPublishCommand.cs     | not-started |               |
| fusion run              | FusionRunCommand.cs         | not-started |               |
| fusion settings (group) | FusionSettingsCommand.cs    | n/a         | Group command |
| fusion settings set     | FusionSettingsSetCommand.cs | not-started |               |
| fusion upload           | FusionUploadCommand.cs      | not-started |               |
| fusion validate         | FusionValidateCommand.cs    | not-started |               |

#### Fusion Publish (5 subcommands)

| Command                 | File                                         | Status      | Notes |
| ----------------------- | -------------------------------------------- | ----------- | ----- |
| fusion publish begin    | FusionConfigurationPublishBeginCommand.cs    | not-started |       |
| fusion publish cancel   | FusionConfigurationPublishCancelCommand.cs   | not-started |       |
| fusion publish commit   | FusionConfigurationPublishCommitCommand.cs   | not-started |       |
| fusion publish start    | FusionConfigurationPublishStartCommand.cs    | not-started |       |
| fusion publish validate | FusionConfigurationPublishValidateCommand.cs | not-started |       |

#### Launch, Login, Logout (3 commands)

| Command | File             | Status      | Notes |
| ------- | ---------------- | ----------- | ----- |
| launch  | LaunchCommand.cs | not-started |       |
| login   | LoginCommand.cs  | not-started |       |
| logout  | LogoutCommand.cs | not-started |       |

#### MCP (6 commands)

| Command      | File                                   | Status      | Notes         |
| ------------ | -------------------------------------- | ----------- | ------------- |
| mcp (group)  | McpCommand.cs                          | n/a         | Group command |
| mcp create   | CreateMcpFeatureCollectionCommand.cs   | not-started |               |
| mcp delete   | DeleteMcpFeatureCollectionCommand.cs   | not-started |               |
| mcp list     | ListMcpFeatureCollectionCommand.cs     | not-started |               |
| mcp publish  | PublishMcpFeatureCollectionCommand.cs  | not-started |               |
| mcp upload   | UploadMcpFeatureCollectionCommand.cs   | not-started |               |
| mcp validate | ValidateMcpFeatureCollectionCommand.cs | not-started |               |

#### Mocks (3 commands)

| Command      | File                 | Status      | Notes         |
| ------------ | -------------------- | ----------- | ------------- |
| mock (group) | MockCommand.cs       | n/a         | Group command |
| mock create  | CreateMockCommand.cs | not-started |               |
| mock list    | ListMockCommand.cs   | not-started |               |
| mock update  | UpdateMockCommand.cs | not-started |               |

#### OpenAPI (6 commands)

| Command          | File                                | Status      | Notes         |
| ---------------- | ----------------------------------- | ----------- | ------------- |
| openapi (group)  | OpenApiCommand.cs                   | n/a         | Group command |
| openapi create   | CreateOpenApiCollectionCommand.cs   | not-started |               |
| openapi delete   | DeleteOpenApiCollectionCommand.cs   | not-started |               |
| openapi list     | ListOpenApiCollectionCommand.cs     | not-started |               |
| openapi publish  | PublishOpenApiCollectionCommand.cs  | not-started |               |
| openapi upload   | UploadOpenApiCollectionCommand.cs   | not-started |               |
| openapi validate | ValidateOpenApiCollectionCommand.cs | not-started |               |

#### PAT (Personal Access Tokens) (3 commands)

| Command     | File                                | Status      | Notes         |
| ----------- | ----------------------------------- | ----------- | ------------- |
| pat (group) | PersonalAccessTokenCommand.cs       | n/a         | Group command |
| pat create  | CreatePersonalAccessTokenCommand.cs | not-started |               |
| pat list    | ListPersonalAccessTokenCommand.cs   | not-started |               |
| pat revoke  | RevokePersonalAccessTokenCommand.cs | not-started |               |

#### Schemas (4 commands)

| Command         | File                     | Status      | Notes         |
| --------------- | ------------------------ | ----------- | ------------- |
| schema (group)  | SchemaCommand.cs         | n/a         | Group command |
| schema download | DownloadSchemaCommand.cs | not-started |               |
| schema publish  | PublishSchemaCommand.cs  | not-started |               |
| schema upload   | UploadSchemaCommand.cs   | not-started |               |
| schema validate | ValidateSchemaCommand.cs | not-started |               |

#### Stages (3 commands)

| Command       | File                  | Status      | Notes         |
| ------------- | --------------------- | ----------- | ------------- |
| stage (group) | StageCommand.cs       | n/a         | Group command |
| stage delete  | DeleteStageCommand.cs | not-started |               |
| stage edit    | EditStagesCommand.cs  | not-started |               |
| stage list    | ListStagesCommand.cs  | not-started |               |

#### Workspaces (5 commands)

| Command               | File                          | Status      | Notes         |
| --------------------- | ----------------------------- | ----------- | ------------- |
| workspace (group)     | WorkspaceCommand.cs           | n/a         | Group command |
| workspace create      | CreateWorkspaceCommand.cs     | not-started |               |
| workspace current     | CurrentWorkspaceCommand.cs    | not-started |               |
| workspace list        | ListWorkspaceCommand.cs       | not-started |               |
| workspace set-default | SetDefaultWorkspaceCommand.cs | not-started |               |
| workspace show        | ShowWorkspaceCommand.cs       | not-started |               |

### 2.3 Per-Command Completion Checklist

When marking a command as `done`, validate ALL items from `COMMAND_TESTING_GUIDELINES.md`:

**Implementation prerequisites:**

- [ ] Command implementation follows `COMMAND_IMPLEMENTATION_GUIDELINES.md`
- [ ] Uses typed mutation errors (switch on concrete error interfaces)
- [ ] Uses auth assertions (`AssertHasAuthentication`)
- [ ] Uses activity fail/success semantics correctly

**Test structure & naming:**

- [ ] Test names follow convention: `<ConditionOrInput>_Returns<Outcome>[_<Mode>]`
- [ ] Mode suffixes: `_Interactive`, `_NonInteractive`, `_JsonOutput`
- [ ] Exception tests: `ClientThrowsException_ReturnsError_<Mode>` and `ClientThrowsAuthorizationException_ReturnsError_<Mode>`
- [ ] Mutation branch errors: `MutationReturns<BranchName>Error_ReturnsError_<Mode>`

**Coverage requirements:**

- [ ] Help output snapshot
- [ ] Interactive mode coverage
- [ ] Non-interactive mode coverage
- [ ] JSON mode coverage
- [ ] Auth/session prerequisite coverage
- [ ] Parser-level required-option test (consolidated case where applicable)
- [ ] ExecuteAsync custom validation branches
- [ ] Input combination branches (options/session/prompt permutations)
- [ ] Cursor coverage in all modes for list commands exposing `--cursor`
- [ ] `NitroClientException` in all three modes
- [ ] `NitroClientAuthorizationException` in all three modes
- [ ] Mutation typed error coverage in all three modes (3 separate theories + shared public MemberData)
- [ ] Branch/end-state mapping complete

### 2.4 Completion Progress Summary

**Completed (11 commands):**

- Baseline suites (3): api-key create, api-key delete, api-key list
- Wave 1 (5): api create, api delete, api list, api show, api set-settings
- Wave 2 (2): client list, client show
- Wave 3 (3): environment create, environment list, environment show
- Wave 4 (2): client create, client delete
- Additional (2): fusion compose, fusion migrate

**In Progress (0 commands):**

- None actively in progress

**Not Started (48+ commands):**

- All remaining commands in client, fusion, launch/login/logout, mcp, mocks, openapi, pat, schemas, stages, workspaces categories

---

## Summary Statistics

- **Total commands:** 60 (49 actual commands + 11 group/container commands)
- **Completed:** 11 commands (22%)
- **In Progress:** 0 commands
- **Not Started:** 49 commands (78%)

**Wave structure established:**

- Waves 1-4 have been executed with specific command groups
- Remaining waves would cover the remaining 49 commands
- Baseline patterns from api-key and api commands serve as reference implementations

---

## Key Takeaways for Implementation

1. **Before adding/migrating tests:** Update command implementation to follow COMMAND_IMPLEMENTATION_GUIDELINES.md first
2. **Implementation-first philosophy:** Schema commands show the readiness pattern — aligned to guidelines, then test suite migration follows
3. **Wave approach:** Batch similar command groups for efficient migration (api, clients, environments, etc.)
4. **Baseline reference:** Use api-key create|delete|list and api command tests as pattern templates
5. **Test prerequisites:** Every command test suite must validate all checklist items before marking `done`
