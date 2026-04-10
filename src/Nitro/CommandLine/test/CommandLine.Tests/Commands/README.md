# Command Test Migration Guide

Reference for migrating Nitro CLI command tests to the shared base class pattern.

## Class Hierarchy

```
CommandTestBase                          (root - mocks, auth, file system, execution)
    |
    +-- SchemasCommandTestBase           (schema/fusion domain)
    |       +-- FusionCommandTestBase
    |       |       +-- FusionValidateCommandTests
    |       |       +-- FusionPublishCommandTests
    |       +-- UploadSchemaCommandTests
    |       +-- PublishSchemaCommandTests
    |
    +-- ClientsCommandTestBase           (client domain)
    |       +-- ShowClientCommandTests
    |       +-- UploadClientCommandTests
    |       +-- ValidateClientCommandTests
    |       ...
```

Each concrete test class inherits from a **domain base** (not `IClassFixture<NitroCommandFixture>` directly).

## What Goes Where

### `CommandTestBase` (root)

- Mock declarations for all API clients (`SchemasClientMock`, `ClientsClientMock`, `ApisClientMock`, etc.)
- File system and environment variable mocks
- Auth helpers: `SetupNoAuthentication()`, `SetupSession()`, `SetupSessionWithWorkspace()`
- `SetupInteractionMode(InteractionMode mode)`
- Command execution: `ExecuteCommandAsync(params string[] args)`, `StartInteractiveCommand(params string[] args)`
- File helpers: `SetupFile()`, `SetupDirectory()`, `SetupCreateFile()`, `SetupOpenReadStream()`
- Auto-verification of all strict mocks in `DisposeAsync()`

### Domain Base (e.g. `ClientsCommandTestBase`)

- **Constants** shared across all commands in the domain
- **Setup methods** for each GraphQL operation, organized in `#region` blocks
- **Payload factories** (private static) that create mock response objects
- **Error factories** (protected static) that create typed error mocks
- **Subscription event factories** (protected static) for subscription-based commands

### Concrete Test Class

- Test methods only
- `TheoryData` provider methods (e.g. `GetUploadClientErrors()`)
- Node/entity factory helpers specific to one command (e.g. `CreateShowClientNode()`)
- No `ClientsClientMock.Setup(...)` calls -- use base class setup methods

## Constants

Define shared test values as `protected const` in the domain base:

```csharp
// CommandTestBase
protected const string ApiId = "api-1";
protected const string Stage = "dev";
protected const string Tag = "v1";

// ClientsCommandTestBase
protected const string ClientId = "client-1";
protected const string ClientName = "web-client";
protected const string ApiName = "products";
protected const string RequestId = "request-1";
protected const string OperationsFile = "operations.json";
```

Use these instead of hardcoded strings in test arguments and setup calls.

## Setup Method Naming

Follow these conventions:

| Pattern | Purpose |
|---|---|
| `Setup<Operation>Mutation(params errors)` | Set up a mutation mock. No errors = success payload. |
| `Setup<Operation>MutationException()` | Mutation throws `InvalidOperationException`. |
| `Setup<Operation>MutationNull<Field>()` | Mutation returns null for a normally-present field. |
| `Setup<Query>Query(result)` | Set up a query mock with a specific result. |
| `Setup<Query>QueryException()` | Query throws `InvalidOperationException`. |
| `Setup<Operation>Subscription(params events)` | Set up a subscription event stream. |

Examples from `ClientsCommandTestBase`:

```csharp
// Mutation: success (no errors) or failure (with errors)
protected void SetupUploadClientMutation(
    params IUploadClient_UploadClient_Errors[] errors) { ... }

// Mutation: throws
protected void SetupUploadClientMutationException() { ... }

// Mutation: null result
protected void SetupUploadClientMutationNullClientVersion() { ... }

// Query
protected void SetupShowClientQuery(IShowClientCommandQuery_Node? result) { ... }

// Subscription
protected void SetupValidateClientSubscription(
    params IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[] events) { ... }
```

Exception setup methods always throw `InvalidOperationException("Something unexpected happened.")`.

## Test Naming

Every test class should have these standard tests:

| Test Name | Type | Purpose |
|---|---|---|
| `Help_ReturnsSuccess` | `[Fact]` | Validates `--help` output |
| `NoSession_Or_ApiKey_ReturnsError(mode)` | `[Theory]` all 3 modes | Auth required |
| `<File>DoesNotExist_ReturnsError(mode)` | `[Theory]` all 3 modes | Input file validation |
| `<Operation>Throws_ReturnsError` | `[Fact]` | Exception handling (default NonInteractive) |
| `<Operation>HasErrors_ReturnsError(error, msg)` | `[Theory]` with `TheoryData` | Typed GraphQL errors |
| `<Operation>ReturnsNull<Field>_ReturnsError` | `[Fact]` | Null result handling |
| `<SuccessScenario>_ReturnsSuccess` | `[Fact]` | Happy path |

### Key Rules

- **One `Throws` test per operation.** Don't test GraphQLException, AuthorizationException, HttpRequestException separately. One `[Fact]` with `InvalidOperationException` covers the error-handling path.
- **One `HasErrors` Theory per operation.** Don't split by interaction mode. Default NonInteractive mode, assert both `StdOut` and `StdErr`. Only include errors whose output is a simple string message. Rich errors with structured output (e.g. persisted query validation errors, breaking change trees) should be tested as individual `[Fact]` tests with their own snapshot assertions, not as TheoryData rows.
- **One test for null results.** No NonInteractive/Interactive variants.
- **`*DoesNotExist` tests are Theories** with all 3 interaction modes.
- **No interaction mode variants** for error/throw/null tests. Only test the default mode.

## Error TheoryData

Use `TheoryData<TError, string>` with base class error factory methods:

```csharp
public static TheoryData<IUploadClient_UploadClient_Errors, string>
    GetUploadClientErrors() => new()
{
    { CreateUploadClientUnauthorizedError(), "Unauthorized." },
    { CreateUploadClientClientNotFoundError(), $"Client '{ClientId}' was not found." },
    { CreateUploadClientDuplicatedTagError(), $"Tag '{Tag}' already exists." },
    { CreateUploadClientConcurrentOperationError(), "A concurrent operation is in progress." },
};
```

Error factory methods live in the domain base class, organized in `#region Error Factories -- <Operation>`.

## Subscription Tests

For commands with subscriptions (validate, publish), set up mutation and subscription separately:

```csharp
// arrange
SetupValidateClientMutation();  // success payload with RequestId
SetupValidateClientSubscription(
    CreateClientVersionValidationOperationInProgressEvent(),
    CreateClientVersionValidationInProgressEvent(),
    CreateClientVersionValidationSuccessEvent());

// act
var result = await ExecuteCommandAsync("client", "validate", ...);
```

Event factory methods are in the domain base (`CreateClientVersionValidationSuccessEvent()`, etc.).

Subscription setup uses `SetupSequence` internally for replay support.

## Interactive Tests

For commands with prompts (create, delete), use `StartInteractiveCommand()`:

```csharp
SetupSessionWithWorkspace();
SetupInteractionMode(InteractionMode.Interactive);
SetupSelectApisPrompt((ApiId, ApiName));

var command = StartInteractiveCommand("client", "create");
command.SelectOption(0);   // select API
command.Input(ClientName); // enter name
var result = await command.RunToCompletionAsync();

result.AssertSuccess();
```

## Region Organization in Domain Base

```csharp
// Setup methods grouped by operation
#region Create
#region Delete
#region Show
#region Upload
#region Validate
#region Publish
#region Unpublish
#region List
#region Download

// Subscription event factories
#region Subscription Event Factories -- Client Version Publish
#region Subscription Event Factories -- Client Version Validation

// Error factories per operation
#region Error Factories -- UploadClient
#region Error Factories -- PublishClientVersion
#region Error Factories -- ValidateClientVersion

// Payload factories (private)
#region Payload Factories
```

## Migration Checklist

When migrating a command's tests:

1. **Create or extend the domain base class** inheriting from `CommandTestBase`
2. **Change the test class** from `IClassFixture<NitroCommandFixture>` to `<DomainBase>(fixture)`
3. **Add constants** for shared values to the domain base
4. **Move all `*ClientMock.Setup(...)` calls** into base class setup methods
5. **Move payload/error/event factories** into the base class
6. **Replace `new CommandBuilder(fixture)...`** with `ExecuteCommandAsync(...)` or `StartInteractiveCommand(...)`
7. **Collapse `ClientThrows*` test pairs** into one `<Operation>Throws_ReturnsError` `[Fact]`
8. **Collapse error test pairs** (NonInteractive + Interactive) into one `[Theory]` with `TheoryData`
9. **Collapse null-result test pairs** into one `[Fact]`
10. **Remove `*.VerifyAll()` calls** -- base `DisposeAsync()` handles this
11. **Remove local helpers** that are now in the base
12. **Clean up using directives**
