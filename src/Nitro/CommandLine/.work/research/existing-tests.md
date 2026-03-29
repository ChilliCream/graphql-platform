# Existing Test Pattern Analysis

## Executive Summary

The CommandLine.Tests project demonstrates a **mature, well-established testing framework** based on the "guideline" pattern. All examined test suites (API, Client, Environment) follow consistent conventions using:

- **CommandBuilder pattern** for test setup and configuration
- **Three-mode interaction coverage**: Interactive, NonInteractive, JsonOutput
- **Inline snapshot testing** for output verification
- **Strict mocks** with error scenario branches
- **Comprehensive error coverage**: auth, mutation errors, exceptions, missing options

This document catalogs the patterns, infrastructure, and exemplary test structures that should guide all future migrations.

---

## Test Infrastructure Overview

### Core Components

#### 1. **CommandBuilder** (`CommandBuilder.cs`)
The main test harness for building commands with fluent configuration.

**Key Features:**
- Fluent builder pattern for composing test configurations
- `ExecuteAsync()` for one-shot command execution
- `Start()` + `RunToCompletionAsync()` for interactive testing
- Service injection via `AddService<T>()`
- Session management (`AddSession()`, `AddSessionWithWorkspace()`)
- Argument composition with `AddArguments()`

**Key Methods:**
```csharp
public CommandBuilder AddInteractionMode(InteractionMode mode)
public CommandBuilder AddApiKey()
public CommandBuilder AddSession()
public CommandBuilder AddSessionWithWorkspace(string workspaceId = "workspace-from-session")
public CommandBuilder AddArguments(params string[] arguments)
public CommandBuilder AddService<T>(T service)
public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken = default)
public InteractiveCommand Start()
```

**Internal Implementation Details:**
- Uses `ServiceCollection` to build DI container
- Mocks **all** Nitro client interfaces by default (via `AddMockedNitroClients`)
- Wraps `System.CommandLine` invocation with custom `InvocationConfiguration`
- Captures stdout/stderr to `StringWriter` instances
- Uses `TestConsole` from Spectre.Console for interactive input simulation

#### 2. **InteractionMode** (`InteractionMode.cs`)
Enum specifying command execution context:
```csharp
public enum InteractionMode
{
    Interactive,      // User can be prompted
    NonInteractive,   // No prompts, fail on missing options
    JsonOutput        // --output json; implies non-interactive + structured output
}
```

#### 3. **CommandExecutionResultExtensions** (`CommandExecutionResultExtensions.cs`)
Helper assertions for result validation:

```csharp
result.AssertError(string stderr)
  // Asserts: StdOut is empty, StdErr matches snapshot, ExitCode == 1

result.AssertSuccess(string stdout)
  // Asserts: StdErr is empty, StdOut matches snapshot, ExitCode == 0

result.AssertHelpOutput(string stdout)
  // Asserts: StdErr is empty, StdOut matches (with executable name normalized), ExitCode == 0
```

#### 4. **InteractiveCommand** (nested in CommandBuilder.cs)
Helper for interactive mode testing:
```csharp
command.Input(string input)              // Push text + Enter
command.SelectOption(int index)          // Arrow down N times, then Enter
command.Confirm(bool value)              // Input "y" or "n"
await command.RunToCompletionAsync()     // Execute and get result
```

#### 5. **Test Helper Classes** (per-command)
Shared factories for creating mock payloads:

**ApiCommandTestHelper.cs example:**
```csharp
public static IShowApiCommandQuery_Node CreateShowApiNode(string id, string name, ...)
public static IDeleteApiCommandMutation_DeleteApiById CreateDeleteApiPayload(...)
public static ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node> CreateListApisPage(...)
```

These reduce boilerplate by centralizing mock construction.

---

## Exemplary Test Patterns

### Pattern 1: Basic Non-Interactive + Interactive + JSON Mode Coverage

**Test Structure:**
```csharp
[Theory]
[InlineData(InteractionMode.Interactive)]
[InlineData(InteractionMode.NonInteractive)]
[InlineData(InteractionMode.JsonOutput)]
public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
{
    // arrange & act
    var result = await new CommandBuilder()
        .AddInteractionMode(mode)
        .AddArguments(...)
        .ExecuteAsync();

    // assert
    result.AssertError("""...""");
}
```

**When to Use:** Validation failures that should produce identical error output across all three modes.

**Real Example:** `CreateApiCommandTests.cs:46-67`

---

### Pattern 2: Single Mode (Success Path) with Mutations

**Test Structure:**
```csharp
[Fact]
public async Task WithOptions_ReturnsSuccess_NonInteractive()
{
    // arrange
    var client = new Mock<IApisClient>(MockBehavior.Strict);
    client.Setup(x => x.CreateApiAsync(...))
        .ReturnsAsync(CreateApiSuccessPayload());

    // act
    var result = await new CommandBuilder()
        .AddService(client.Object)
        .AddApiKey()
        .AddInteractionMode(InteractionMode.NonInteractive)
        .AddArguments(...)
        .ExecuteAsync();

    // assert
    result.AssertSuccess("""...""");
    client.VerifyAll();
}
```

**When to Use:** Happy path with mutation; one test per mode (often NonInteractive + JsonOutput share logic).

**Real Example:** `CreateApiCommandTests.cs:119-172`

---

### Pattern 3: Interactive Mode with Prompting

**Test Structure:**
```csharp
[Fact]
public async Task MissingRequiredOptions_PromptsUser_ReturnsSuccess()
{
    // arrange
    var client = new Mock<IApisClient>(MockBehavior.Strict);
    client.Setup(x => x.CreateApiAsync(...))
        .ReturnsAsync(CreateApiSuccessPayload());

    var command = new CommandBuilder()
        .AddService(client.Object)
        .AddSessionWithWorkspace()
        .AddInteractionMode(InteractionMode.Interactive)
        .AddArguments(...)
        .Start();  // <- Returns InteractiveCommand, not CommandResult

    // act
    command.Input("my-api");
    command.Input("/products");
    var result = await command.RunToCompletionAsync();

    // assert
    result.AssertSuccess("""...""");
    client.VerifyAll();
}
```

**Key Points:**
- Use `.Start()` instead of `.ExecuteAsync()` for interactive
- Simulate user input with `.Input()`, `.SelectOption()`, `.Confirm()`
- Still uses inline snapshots for output verification

**Real Example:** `CreateApiCommandTests.cs:228-280`

---

### Pattern 4: Mutation Error Branches (Typed Errors)

**Test Structure:**
```csharp
[Fact]
public async Task MutationReturnsChangeError_ReturnsError_NonInteractive()
{
    // arrange
    var changeError = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error>(MockBehavior.Strict);
    changeError
        .As<IError>()
        .SetupGet(x => x.Message)
        .Returns("Create denied");

    var client = new Mock<IApisClient>(MockBehavior.Strict);
    client.Setup(x => x.CreateApiAsync(...))
        .ReturnsAsync(CreateApiPayloadWithChangeError(changeError.Object));

    // act
    var result = await new CommandBuilder()
        .AddService(client.Object)
        .AddApiKey()
        .AddInteractionMode(InteractionMode.NonInteractive)
        .AddArguments(...)
        .ExecuteAsync();

    // assert
    result.StdOut.MatchInlineSnapshot("""...""");
    result.StdErr.MatchInlineSnapshot("""...""");
    Assert.Equal(1, result.ExitCode);
    client.VerifyAll();
}
```

**Key Points:**
- Separate tests per error type per mode (3x3 matrix typical)
- Use named error interfaces (e.g., `ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error`)
- Verify both stdout and stderr independently (not via `AssertError()`)

**Real Examples:**
- `CreateApiCommandTests.cs:327-374` (Change error, NonInteractive)
- `CreateApiCommandTests.cs:377-423` (Change error, Interactive)
- `CreateApiCommandTests.cs:426-468` (Change error, JsonOutput)

---

### Pattern 5: Client Exception Handling (Parametric via Theory)

**Test Structure:**
```csharp
[Theory]
[InlineData(InteractionMode.Interactive)]
[InlineData(InteractionMode.NonInteractive)]
[InlineData(InteractionMode.JsonOutput)]
public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
{
    // arrange
    var client = new Mock<IApisClient>(MockBehavior.Strict);
    client.Setup(x => x.ListApisAsync(...))
        .ThrowsAsync(ex);

    // act
    var result = await new CommandBuilder()
        .AddService(client.Object)
        .AddSessionWithWorkspace()
        .AddInteractionMode(mode)
        .AddArguments(...)
        .ExecuteAsync();

    // assert
    result.AssertError("""...""");
    client.VerifyAll();
}
```

**When to Use:** Exceptions that map to identical error output across all modes.

**Real Example:** `ListApiCommandTests.cs:527-547`

---

### Pattern 6: Help Output Snapshot

**Test Structure:**
```csharp
[Fact]
public async Task Help_ReturnsSuccess()
{
    // arrange & act
    var result = await new CommandBuilder()
        .AddArguments("api", "create", "--help")
        .ExecuteAsync();

    // assert
    result.AssertHelpOutput("""
        Description:
          Creates a new API

        Usage:
          nitro api create [options]
        ...
        """);
}
```

**Key Points:**
- Always first test in a suite (foundation reference)
- Uses `AssertHelpOutput()` which normalizes executable name
- Multiline snapshot with full option descriptions

**Real Example:** `CreateApiCommandTests.cs:10-40`

---

### Pattern 7: List Command with Cursor Pagination

**Test Structure (Non-Interactive):**
```csharp
[Theory]
[InlineData(InteractionMode.NonInteractive)]
[InlineData(InteractionMode.JsonOutput)]
public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
{
    // arrange
    var client = new Mock<IApisClient>(MockBehavior.Strict);
    client.Setup(x => x.ListApisAsync(
            "workspace-from-session",
            "cursor-1",  // <- Cursor passed through
            10,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(ApiCommandTestHelper.CreateListApisPage(...));

    // act
    var result = await new CommandBuilder()
        .AddService(client.Object)
        .AddApiKey()
        .AddSessionWithWorkspace()
        .AddInteractionMode(mode)
        .AddArguments("api", "list", "--cursor", "cursor-1")
        .ExecuteAsync();

    // assert
    result.AssertSuccess("""...""");
    client.VerifyAll();
}
```

**Key Points:**
- Cursor commands have dedicated test cases
- Typically tested in NonInteractive + JsonOutput only
- Interactive mode has its own pagination test (user selects next page)

**Real Example:** `ListApiCommandTests.cs:391-464`

---

## Test File Organization

### Directory Structure
```
test/CommandLine.Tests/
├── Commands/
│   ├── ApiKeys/
│   │   ├── CreateApiKeyCommandTests.cs
│   │   ├── DeleteApiKeyCommandTests.cs
│   │   ├── ListApiKeyCommandTests.cs
│   │   └── ApiKeyCommandTestHelper.cs
│   ├── Apis/
│   │   ├── CreateApiCommandTests.cs
│   │   ├── ListApiCommandTests.cs
│   │   ├── DeleteApiCommandTests.cs
│   │   ├── ShowApiCommandTests.cs
│   │   ├── SetApiSettingsCommandTests.cs
│   │   └── ApiCommandTestHelper.cs
│   ├── Clients/
│   │   ├── CreateClientCommandTests.cs
│   │   ├── DeleteClientCommandTests.cs
│   │   ├── ListClientCommandTests.cs
│   │   ├── ShowClientCommandTests.cs
│   │   └── (no helper; simple payloads inline)
│   ├── Environments/
│   │   ├── CreateEnvironmentCommandTests.cs
│   │   ├── ListEnvironmentCommandTests.cs
│   │   ├── ShowEnvironmentCommandTests.cs
│   │   └── (no helper; simple payloads inline)
│   └── ...
├── CommandBuilder.cs
├── InteractionMode.cs
├── CommandExecutionResultExtensions.cs
└── COMMAND_TEST_MIGRATION_PROGRESS.md
```

---

## Test Coverage Patterns by Command Type

### Create Commands

**Mandatory Test Cases:**
1. **Help output** → snapshot
2. **No auth (all 3 modes)** → `AssertError()`
3. **No workspace when required (all 3 modes)** → `AssertError()`
4. **Missing required option (NonInteractive + JsonOutput)** → `AssertError()`
5. **Missing required option with prompting (Interactive)** → user input flow
6. **Success with all options (NonInteractive, JsonOutput, + maybe Interactive)** → `AssertSuccess()`
7. **Mutation error branches (per error type, all 3 modes)** → varies by command
8. **Client exception (all 3 modes or shared theory)** → `AssertError()`
9. **Client authorization exception (all 3 modes)** → `AssertError()`

**Example Count:**
- `CreateApiCommandTests`: 27 test methods (help + 3 auth cases + required options + success paths + 7 mutation branches × 3 modes + 2 exceptions × 3 modes)
- `CreateClientCommandTests`: 18 test methods (help + auth + options + success + 4 mutation branches + 2 exceptions)

### List Commands

**Mandatory Test Cases:**
1. **Help output** → snapshot
2. **No auth (all 3 modes)** → `AssertError()`
3. **No workspace when required (all 3 modes)** → `AssertError()`
4. **Success with workspace ID (Interactive, NonInteractive, JsonOutput)** → mixed
5. **Success with session workspace (Interactive, NonInteractive, JsonOutput)**
6. **Success with cursor (NonInteractive, JsonOutput)**
7. **Empty results (Interactive, NonInteractive, JsonOutput)**
8. **Client exception (all 3 modes or shared theory)** → `AssertError()`
9. **Client authorization exception (all 3 modes)** → `AssertError()`

**Example Count:**
- `ListApiCommandTests`: 11 test methods (help + auth + workspace + 3 × workspace variants + 2 exceptions with theory)

### Delete/Show Commands

**Mandatory Test Cases:**
1. **Help output** → snapshot
2. **No auth (all 3 modes)** → `AssertError()`
3. **Missing resource ID** (NonInteractive + JsonOutput) → `AssertError()`
4. **Success** (NonInteractive, JsonOutput, Interactive variant)
5. **Resource not found** (all 3 modes)
6. **Client exception** (all 3 modes)
7. **Client authorization exception** (all 3 modes)

---

## Mock Payload Construction Pattern

### Inline (Simple Cases)
```csharp
var payload = new Mock<ICreateClientCommandMutation_CreateClient>(MockBehavior.Strict);
payload.SetupGet(x => x.Client).Returns(client);
payload.SetupGet(x => x.Errors).Returns(errors);
return payload.Object;
```

### Factory Methods (Reusable Cases)
Placed in `*CommandTestHelper.cs`:
```csharp
public static ICreateApiCommandMutation_PushWorkspaceChanges CreateApiSuccessPayload()
{
    var change = new Mock<...>(MockBehavior.Strict);
    // ... build complex nested mocks
    return payload.Object;
}
```

**When to Extract:**
- Payload used in 3+ tests
- Payload requires 5+ mock objects nested
- Multiple variants (WithError, WithNoChanges, etc.)

---

## Snapshot Testing Conventions

### Inline Snapshot Format
```csharp
result.AssertSuccess("""
    Creating API...
    └── Successfully created API!

    {
      "id": "api-1",
      "name": "my-api",
      "path": "products/catalog"
    }
    """);
```

**Key Points:**
- Triple quotes for multiline strings
- Indentation matters (4 spaces for JSON)
- Output is trimmed (trailing whitespace removed)
- Executable name in help output is replaced with "nitro" for stability

### ANSI/Spectre Output
Interactive snapshots include Spectre markup:
```
[    ] Creating API...
```
These are captured as-is from TestConsole.

---

## Test Naming Conventions

### Pattern: `<ConditionOrInput>_Return<Outcome>[_<Mode>]`

**Examples:**
- `Help_ReturnsSuccess`
- `NoSession_Or_ApiKey_ReturnsError` (theory with 3 modes)
- `WithOptions_ReturnsSuccess_NonInteractive`
- `WithOptions_ReturnsSuccess_OutputJson` (alternative mode suffix)
- `MissingRequiredOptions_PromptsUser_ReturnsSuccess`
- `MutationReturnsChangeError_ReturnsError_NonInteractive`
- `ClientThrowsException_ReturnsError` (theory or per-mode)
- `ClientThrowsAuthorizationException_ReturnsError_Interactive`

**Mode Suffixes:**
- `_Interactive`
- `_NonInteractive`
- `_JsonOutput` or `_OutputJson` (both seen)

---

## Error Handling Patterns

### Authentication/Session Errors
```csharp
result.AssertError("""
    This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
    """);
```

### Missing Workspace Context
```csharp
result.AssertError("""
    You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
    """);
```

### Mutation Errors (Change-Level)
```csharp
result.StdOut.MatchInlineSnapshot("""
    Creating API...
    └── Failed!
    """);
result.StdErr.MatchInlineSnapshot("""
    Create denied
    """);
Assert.Equal(1, result.ExitCode);
```

### Mutation Errors (Payload-Level)
```csharp
result.StdOut.MatchInlineSnapshot("""
    Creating API...
    └── Failed!
    """);
result.StdErr.MatchInlineSnapshot("""
    Unexpected mutation error: Mutation payload denied
    """);
Assert.Equal(1, result.ExitCode);
```

### Client Exceptions
```csharp
result.AssertError("""
    There was an unexpected error executing your request: create failed
    """);
```

### Authorization Exceptions
```csharp
result.AssertError("""
    The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
    """);
```

---

## Key Takeaways for Implementation

1. **CommandBuilder is the pivot point** — all tests use it, in consistent order:
   - Add service mocks (client)
   - Add auth (api-key or session)
   - Add interaction mode
   - Add command arguments
   - Execute or start

2. **Three-mode coverage is non-negotiable** — at minimum for error paths, shared via Theory/InlineData

3. **Snapshot testing scales** — inline snapshots work well up to ~30 test methods per file

4. **Helper factories reduce duplication** — extracted into `*CommandTestHelper.cs` when reused

5. **Strict mocks catch gaps** — `MockBehavior.Strict` + `VerifyAll()` ensures setup matches actual calls

6. **Error paths are the majority** — happy path ~20%, auth/validation/exception paths ~80% of tests

7. **Interactive mode needs special handling** — `.Start()` + input simulation, separate test methods (not Theory)

---

## Completed Test Suites (Reference Implementation)

| Suite | File | Test Count | Status |
|-------|------|-----------|--------|
| api create | CreateApiCommandTests.cs | 27 | done |
| api delete | DeleteApiCommandTests.cs | 23 | done |
| api list | ListApiCommandTests.cs | 11 | done |
| api show | ShowApiCommandTests.cs | 17 | done |
| api set-settings | SetApiSettingsCommandTests.cs | 19 | done |
| client create | CreateClientCommandTests.cs | 18 | done |
| client delete | DeleteClientCommandTests.cs | 17 | done |
| client list | ListClientCommandTests.cs | 8 | done |
| client show | ShowClientCommandTests.cs | 12 | done |
| environment create | CreateEnvironmentCommandTests.cs | 12+ | done |
| environment list | ListEnvironmentCommandTests.cs | 8+ | done |
| environment show | ShowEnvironmentCommandTests.cs | 11+ | done |
| fusion compose | FusionComposeCommandTests.cs | - | done |
| fusion migrate | FusionMigrateCommandTests.cs | - | done |

**Total: ~200+ test methods across completed suites**

---

## Appendix: Quick Reference

### Build a Basic Test
```csharp
[Fact]
public async Task Condition_ReturnsOutcome()
{
    var client = new Mock<ISomeClient>(MockBehavior.Strict);
    client.Setup(x => x.MethodAsync(...))
        .ReturnsAsync(CreatePayload(...));

    var result = await new CommandBuilder()
        .AddService(client.Object)
        .AddApiKey()
        .AddSessionWithWorkspace()
        .AddInteractionMode(InteractionMode.NonInteractive)
        .AddArguments("command", "subcommand", "--option", "value")
        .ExecuteAsync();

    result.AssertSuccess("""expected output""");
    client.VerifyAll();
}
```

### Build an Interactive Test
```csharp
[Fact]
public async Task WithPrompts_ReturnsSuccess()
{
    var client = new Mock<ISomeClient>(MockBehavior.Strict);
    client.Setup(x => x.MethodAsync(...))
        .ReturnsAsync(CreatePayload(...));

    var command = new CommandBuilder()
        .AddService(client.Object)
        .AddApiKey()
        .AddInteractionMode(InteractionMode.Interactive)
        .AddArguments("command", "subcommand")
        .Start();

    command.Input("user input");
    command.SelectOption(1);
    var result = await command.RunToCompletionAsync();

    result.AssertSuccess("""expected output with prompts""");
    client.VerifyAll();
}
```

### Extract a Helper Factory
```csharp
// In MyCommandTestHelper.cs
public static IMyMutation_Result CreateSuccessPayload(string id, string name)
{
    var result = new Mock<IMyMutation_Result>(MockBehavior.Strict);
    result.SetupGet(x => x.Id).Returns(id);
    result.SetupGet(x => x.Name).Returns(name);
    return result.Object;
}

// In test:
.ReturnsAsync(MyCommandTestHelper.CreateSuccessPayload("id-1", "name"))
```
