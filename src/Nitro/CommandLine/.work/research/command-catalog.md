# Nitro CLI Command Catalog

**Research Date**: 2026-03-29
**Source**: `/src/CommandLine/Commands/` directory
**Total Commands Found**: 85+ command implementations across 14 categories

---

## Architecture Overview

### Base Pattern
All commands inherit from `System.CommandLine.Command` and follow a consistent pattern:

1. **Constructor**: Dependency injection via constructor, configuration in body
   - Adds options via `Options.Add()`
   - Adds subcommands via `Subcommands.Add()` for parent commands
   - Sets action handler via `SetActionWithExceptionHandling()`

2. **Exception Handling**: All commands use `CommandExtensions.SetActionWithExceptionHandling()`
   - Handles: `ExitException`, `NitroClientAuthorizationException`, `NitroClientException`, cancellations, general exceptions
   - Catches all exceptions and returns appropriate exit code

3. **Global Options**: Added via `AddGlobalNitroOptions()`
   - `OptionalCloudUrlOption`
   - `OptionalApiKeyOption`
   - `OptionalOutputFormatOption`

4. **Action Signature**:
   ```csharp
   async (parseResult, cancellationToken) => await ExecuteAsync(...)
   ```

### Command Categories

#### **Parent Commands** (Group subcommands)
- Inherit: `Command`
- Pattern: Constructor injects subcommand instances, adds to `Subcommands` collection
- No action handler (subcommands handle the work)
- Typically decorated with `#if !NET9_0_OR_GREATER` for AOT compatibility
- Examples: `ApiCommand`, `WorkspaceCommand`, `SchemaCommand`, `MockCommand`, etc.

#### **Leaf Commands** (Perform actual work)
- Inherit: `Command`
- Pattern: Constructor injects dependencies, sets action via `SetActionWithExceptionHandling()`
- Private/public static `ExecuteAsync()` method containing the implementation
- Examples: `CreateWorkspaceCommand`, `UploadSchemaCommand`, `CreateApiKeyCommand`, etc.

---

## Commands by Category

### 1. **ApiKey Commands** (4 commands)
Category: `Commands/ApiKeys/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| api-key | `ApiKeyCommand` | Parent | - | Root command for API key operations |
| create | `CreateApiKeyCommand` | Leaf | `ExecuteAsync()` | Create a new API key with optional scope (workspace/API) and stage conditions |
| delete | `DeleteApiKeyCommand` | Leaf | `ExecuteAsync()` | Delete an API key |
| list | `ListApiKeyCommand` | Leaf | `ExecuteAsync()` | List API keys with pagination |

**Dependencies**: `IApisClient`, `IApiKeysClient`, `ISessionService`, `INitroConsole`, `IResultHolder`

**Patterns**:
- Interactive prompts for missing options (API vs Workspace scope)
- Mutation error handling with specific error type matching
- Uses `IResultHolder` to return structured results

---

### 2. **Api Commands** (6 commands)
Category: `Commands/Apis/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| api | `ApiCommand` | Parent | - | Root command for API operations |
| create | `CreateApiCommand` | Leaf | `ExecuteAsync()` | Create a new API with path and kind |
| delete | `DeleteApiCommand` | Leaf | `ExecuteAsync()` | Delete an API |
| list | `ListApiCommand` | Leaf | `ExecuteAsync()` | List APIs with interactive/non-interactive modes |
| set-settings | `SetApiSettingsCommand` | Leaf | `ExecuteAsync()` | Modify API settings |
| show | `ShowApiCommand` | Leaf | `ExecuteAsync()` | Show detailed API information |

**Dependencies**: `IApisClient`, `INitroConsole`, `ISessionService`, `IResultHolder`

**Patterns**:
- Interactive vs non-interactive path handling
- Path parsing with string split
- Activity-scoped operations (`console.StartActivity()`)

---

### 3. **Client Commands** (12 commands)
Category: `Commands/Clients/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| client | `ClientCommand` | Parent | - | Root command for client operations |
| create | `CreateClientCommand` | Leaf | `ExecuteAsync()` | Create a new client |
| delete | `DeleteClientCommand` | Leaf | `ExecuteAsync()` | Delete a client |
| download | `DownloadClientCommand` | Leaf | `ExecuteAsync()` | Download generated client code |
| list | `ListClientCommand` | Leaf | `RenderInteractiveAsync()`, `RenderNonInteractiveAsync()` | List clients with pagination and table UI |
| list versions | `ListClientVersionsCommand` | Leaf | - | List versions of a client |
| list published-versions | `ListClientPublishedVersionsCommand` | Leaf | - | List published versions of a client |
| publish | `PublishClientCommand` | Leaf | `ExecuteAsync()` | Publish a client version |
| show | `ShowClientCommand` | Leaf | `ExecuteAsync()` | Show client details |
| unpublish | `UnpublishClientCommand` | Leaf | `ExecuteAsync()` | Unpublish a client version |
| upload | `UploadClientCommand` | Leaf | `ExecuteAsync()` | Upload client configuration |
| validate | `ValidateClientCommand` | Leaf | `ExecuteAsync()` | Validate client configuration |

**Dependencies**: `IClientsClient`, `IApisClient`, `INitroConsole`, `ISessionService`, `IResultHolder`, `IFileSystem`

**Patterns**:
- `ListClientCommand` has both interactive (with table UI) and non-interactive modes
- `PaginationContainer` for paginated results
- `PagedTable` for interactive table rendering with selection
- `ClientDetailPrompt` for structured result conversion

---

### 4. **Environment Commands** (4 commands)
Category: `Commands/Environments/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| environment | `EnvironmentCommand` | Parent | - | Root command for environment operations |
| create | `CreateEnvironmentCommand` | Leaf | `ExecuteAsync()` | Create a new environment |
| list | `ListEnvironmentCommand` | Leaf | `ExecuteAsync()` | List environments |
| show | `ShowEnvironmentCommand` | Leaf | `ExecuteAsync()` | Show environment details |

**Dependencies**: `IEnvironmentsClient`, `INitroConsole`, `ISessionService`, `IResultHolder`

---

### 5. **Fusion Commands** (15 commands)
Category: `Commands/Fusion/` and `Commands/Fusion/PublishCommand/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| fusion | `FusionCommand` | Parent | - | Root command for Fusion configuration |
| compose | `FusionComposeCommand` | Leaf | `ExecuteAsync()`, `ReadSourceSchemaAsync()` | Compose multiple source schemas into gateway |
| download | `FusionDownloadCommand` | Leaf | `ExecuteAsync()` | Download a composition result |
| migrate | `FusionMigrateCommand` | Leaf | `ExecuteAsync()` | Migrate Fusion configuration |
| publish | `FusionPublishCommand` | Leaf | `ExecuteAsync()` | Publish a Fusion configuration |
| run | `FusionRunCommand` | Leaf | `ExecuteAsync()` | Run a Fusion gateway locally |
| settings | `FusionSettingsCommand` | Parent | - | Root command for Fusion settings |
| settings set | `FusionSettingsSetCommand` | Leaf | `ExecuteAsync()` | Set a Fusion setting |
| upload | `FusionUploadCommand` | Leaf | `ExecuteAsync()` | Upload a source schema for composition |
| validate | `FusionValidateCommand` | Leaf | `ExecuteAsync()` | Validate a Fusion configuration |
| publish begin | `FusionConfigurationPublishBeginCommand` | Leaf | `ExecuteAsync()` | Start a multi-step publish (reserve slot) |
| publish start | `FusionConfigurationPublishStartCommand` | Leaf | `ExecuteAsync()` | Start composition of publish |
| publish validate | `FusionConfigurationPublishValidateCommand` | Leaf | - | Validate publish configuration |
| publish commit | `FusionConfigurationPublishCommitCommand` | Leaf | - | Commit the publish |
| publish cancel | `FusionConfigurationPublishCancelCommand` | Leaf | `ExecuteAsync()` | Cancel an in-progress publish |

**Dependencies**: `INitroConsole`, `IFusionConfigurationClient`, `IFileSystem`, `ISessionService`

**Patterns**:
- Complex multi-step operations (publish flow with begin/start/validate/commit/cancel)
- Archive handling for source schemas (`FusionSourceSchemaArchive`)
- File-based state management (`FusionConfigurationPublishingState`)
- Stream-based file operations for large uploads

---

### 6. **Launch Commands** (1 command)
Category: `Commands/Launch/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| launch | `LaunchCommand` | Leaf | `ExecuteAsync()` | Open Nitro in default browser |

**Dependencies**: `INitroConsole`

**Patterns**:
- Synchronous command with no async work
- Uses `SystemBrowser.Open()` utility

---

### 7. **Login Commands** (1 command)
Category: `Commands/Login/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| login | `LoginCommand` | Leaf | `ExecuteAsync()` | Interactively login via browser |

**Dependencies**: `INitroConsole`, `IWorkspacesClient`, `ISessionService`

**Patterns**:
- Browser-based authentication flow
- Delegates to `SetDefaultWorkspaceCommand.ExecuteAsync()` for workspace selection after login

---

### 8. **Logout Commands** (1 command)
Category: `Commands/Logout/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| logout | `LogoutCommand` | Leaf | `ExecuteAsync()` | Clear session and logout |

**Dependencies**: `INitroConsole`, `ISessionService`

**Patterns**:
- Simple cleanup operation
- No result holder needed

---

### 9. **MCP Commands** (7 commands)
Category: `Commands/Mcp/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| mcp | `McpCommand` | Parent | - | Root command for MCP feature collections |
| create | `CreateMcpFeatureCollectionCommand` | Leaf | `ExecuteAsync()` | Create a new MCP feature collection |
| delete | `DeleteMcpFeatureCollectionCommand` | Leaf | `ExecuteAsync()` | Delete an MCP feature collection |
| list | `ListMcpFeatureCollectionCommand` | Leaf | `ExecuteAsync()` | List MCP feature collections |
| publish | `PublishMcpFeatureCollectionCommand` | Leaf | `ExecuteAsync()` | Publish an MCP feature collection |
| upload | `UploadMcpFeatureCollectionCommand` | Leaf | `ExecuteAsync()` | Upload an MCP feature collection |
| validate | `ValidateMcpFeatureCollectionCommand` | Leaf | `ExecuteAsync()` | Validate MCP feature collection format |

**Dependencies**: `IMcpFeaturesClient`, `IFileSystem`, `INitroConsole`, `ISessionService`, `IResultHolder`

---

### 10. **Mock Commands** (4 commands)
Category: `Commands/Mocks/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| mock | `MockCommand` | Parent | - | Root command for mocks (Hidden: true) |
| create | `CreateMockCommand` | Leaf | `ExecuteAsync()`, local `CreateNewMock()` | Create mock schema with base schema and extension |
| list | `ListMockCommand` | Leaf | `ExecuteAsync()` | List mocks |
| update | `UpdateMockCommand` | Leaf | `ExecuteAsync()` | Update mock configuration |

**Dependencies**: `IMocksClient`, `IApisClient`, `INitroConsole`, `IFileSystem`, `ISessionService`, `IResultHolder`

**Patterns**:
- `CreateMockCommand` uses local function `CreateNewMock()` for nested logic
- Requires both base schema file and extension file
- Hidden command (not shown in help)

---

### 11. **OpenApi Commands** (7 commands)
Category: `Commands/OpenApi/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| openapi | `OpenApiCommand` | Parent | - | Root command for OpenAPI collections |
| create | `CreateOpenApiCollectionCommand` | Leaf | `ExecuteAsync()` | Create OpenAPI collection |
| delete | `DeleteOpenApiCollectionCommand` | Leaf | `ExecuteAsync()` | Delete OpenAPI collection |
| list | `ListOpenApiCollectionCommand` | Leaf | `ExecuteAsync()` | List OpenAPI collections |
| publish | `PublishOpenApiCollectionCommand` | Leaf | `ExecuteAsync()` | Publish OpenAPI collection |
| upload | `UploadOpenApiCollectionCommand` | Leaf | `ExecuteAsync()` | Upload OpenAPI collection |
| validate | `ValidateOpenApiCollectionCommand` | Leaf | `ExecuteAsync()` | Validate OpenAPI specification |

**Dependencies**: `IOpenApiCollectionsClient`, `IFileSystem`, `INitroConsole`, `ISessionService`, `IResultHolder`

---

### 12. **PersonalAccessToken Commands** (4 commands)
Category: `Commands/PersonalAccessTokens/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| personal-access-token | `PersonalAccessTokenCommand` | Parent | - | Root command for personal access tokens |
| create | `CreatePersonalAccessTokenCommand` | Leaf | `ExecuteAsync()` | Create a new personal access token |
| list | `ListPersonalAccessTokenCommand` | Leaf | `ExecuteAsync()` | List personal access tokens |
| revoke | `RevokePersonalAccessTokenCommand` | Leaf | `ExecuteAsync()` | Revoke a personal access token |

**Dependencies**: `IPersonalAccessTokensClient`, `INitroConsole`, `ISessionService`, `IResultHolder`

---

### 13. **Schema Commands** (5 commands)
Category: `Commands/Schemas/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| schema | `SchemaCommand` | Parent | - | Root command for schema operations |
| download | `DownloadSchemaCommand` | Leaf | `ExecuteAsync()` | Download a schema version |
| publish | `PublishSchemaCommand` | Leaf | `ExecuteAsync()` | Publish a schema version |
| upload | `UploadSchemaCommand` | Leaf | `ExecuteAsync()` | Upload a new schema version |
| validate | `ValidateSchemaCommand` | Leaf | `ExecuteAsync()` | Validate schema format |

**Dependencies**: `ISchemasClient`, `INitroConsole`, `IFileSystem`, `ISessionService`, `IResultHolder`

**Patterns**:
- `UploadSchemaCommand` handles stream-based file upload with metadata
- Error handling with specific error type matching (UnauthorizedOperation, DuplicatedTag, etc.)

---

### 14. **Stage Commands** (4 commands)
Category: `Commands/Stages/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| stage | `StageCommand` | Parent | - | Root command for stage operations |
| delete | `DeleteStageCommand` | Leaf | `ExecuteAsync()` | Delete a stage |
| edit | `EditStagesCommand` | Leaf | `ExecuteAsync()`, `EditStagesInteractivlyAsync()` | Edit stages with JSON config or interactive UI |
| list | `ListStagesCommand` | Leaf | `ExecuteAsync()` | List stages |

**Dependencies**: `IStagesClient`, `IApisClient`, `INitroConsole`, `ISessionService`, `IResultHolder`

**Patterns**:
- `EditStagesCommand` demonstrates:
  - JSON configuration parsing (`System.Text.Json`)
  - Interactive vs non-interactive modes
  - Complex UI with `SelectableTable` for multi-step operations
  - Local record types for action results
  - File-scoped static extension classes for helper methods
  - State management (add/delete/edit stages in memory before saving)

---

### 15. **Workspace Commands** (6 commands)
Category: `Commands/Workspaces/`

| Command | Class | Type | Key Methods | Purpose |
|---------|-------|------|-------------|---------|
| workspace | `WorkspaceCommand` | Parent | - | Root command for workspace operations |
| create | `CreateWorkspaceCommand` | Leaf | `ExecuteAsync()` | Create a new workspace |
| current | `CurrentWorkspaceCommand` | Leaf | `ExecuteAsync()` | Show current workspace |
| list | `ListWorkspaceCommand` | Leaf | `ExecuteAsync()` | List workspaces |
| set-default | `SetDefaultWorkspaceCommand` | Leaf | `ExecuteAsync()` | Set default workspace |
| show | `ShowWorkspaceCommand` | Leaf | `ExecuteAsync()` | Show workspace details |

**Dependencies**: `IWorkspacesClient`, `INitroConsole`, `ISessionService`, `IResultHolder`

**Patterns**:
- `CreateWorkspaceCommand` uses interactive confirmation prompts
- `SetDefaultWorkspaceCommand` has a `forceSelection` parameter for reuse in other commands (like login)
- Uses `ObjectResult` for structured output

---

## Root Command

**File**: `NitroRootCommand.cs`

```csharp
internal sealed class NitroRootCommand : RootCommand
```

- Inherits from `System.CommandLine.RootCommand`
- Injects all 15 parent category commands
- Adds each as a subcommand in constructor

---

## Infrastructure & Shared Patterns

### Extension Methods
**File**: `Extensions/CommandExtensions.cs`

```csharp
SetActionWithExceptionHandling(Command, INitroConsole, Func<ParseResult, CancellationToken, Task<int>>)
AddGlobalNitroOptions(Command)
```

### Option Pattern
Commands use singleton option instances via `Opt<T>.Instance`:
- `Opt<TagOption>.Instance`
- `Opt<SchemaFileOption>.Instance`
- `Opt<ApiIdOption>.Instance`
- etc.

All options are retrieved from `ParseResult` via `GetValue()` or `GetValueOrDefault()`

### Console & Output
**Interface**: `INitroConsole`

Key methods used:
- `WriteLineAsync()`, `WriteLine()`
- `Error.WriteLineAsync()` - for error output
- `StartActivity()` - for progress indication (returns `IAsyncDisposable` activity object)
- `PromptAsync()` - interactive prompts
- `ConfirmAsync()` - yes/no confirmation
- `PrintMutationErrorsAndExit()` - error handling helper
- `OkLine()`, `Success()` - success messages
- `IsInteractive` - check if running interactively

### Result Holding
**Interface**: `IResultHolder`

- Stores structured command results for output
- Called via `SetResult(new ObjectResult(...))` or `SetResult(new PaginatedListResult<T>())`
- Used in most create/list/show commands

### Session Management
**Interface**: `ISessionService`

- `Session` property for current user
- `LoginAsync()` - authenticate user
- `LogoutAsync()` - clear session
- `SelectWorkspaceAsync()` - set current workspace

### Client Interfaces
All following patterns present:

**Schemas**: `ISchemasClient` - upload/download/validate operations
**Apis**: `IApisClient` - CRUD + settings
**Clients**: `IClientsClient` - list/publish/validate
**Workspaces**: `IWorkspacesClient` - CRUD
**And more**: `IApiKeysClient`, `IMocksClient`, `IStagesClient`, `IFusionConfigurationClient`, etc.

All client methods are async and take `CancellationToken`

### File Operations
**Interface**: `IFileSystem`

- `OpenReadStream()` - read file as stream
- `OpenWriteStream()` - write to file
- Provides abstraction for testing and different platforms

---

## Key Patterns Identified

### 1. **Consistent Command Structure**
Every leaf command follows:
1. Constructor with DI → option setup → action setup
2. `ExecuteAsync()` method (static or instance) with same signature
3. Error handling at top level (options/auth) then delegation to business logic
4. Return `ExitCodes.Success` or `ExitCodes.Error`

### 2. **Interactive vs Non-Interactive**
Commands like `ListClientCommand`, `EditStagesCommand` check `console.IsInteractive`:
- Interactive: Use tables, prompts, confirmations
- Non-interactive: Return structured JSON, check for required options

### 3. **Activity Scoping**
Many commands use:
```csharp
await using (var activity = console.StartActivity("..."))
{
    // work
    activity.Success("message");  // or activity.Fail()
}
```

### 4. **Mutation Error Handling**
Standard pattern for GraphQL mutations:
```csharp
var result = await client.SomeAsync(...);
if (result.Errors?.Count > 0) {
    activity.Fail();
    foreach (var error in result.Errors) {
        var message = error switch {
            ISpecificError e => e.Message,
            ...
            _ => "Unexpected error"
        };
        await console.Error.WriteLineAsync(message);
    }
    return ExitCodes.Error;
}
```

### 5. **Pagination**
Used in list commands with `PaginationContainer`:
- Cursor-based pagination
- `PageSize()` configuration
- `PagedTable` for interactive rendering

### 6. **File-Scoped Static Helpers**
Complex commands (like `EditStagesCommand`) use file-scoped static classes:
```csharp
file static class ClientExtensions { ... }
file static class Extensions { ... }
file record ActionResult { ... }
```

### 7. **Prompt Helpers**
Commands have custom extension methods for common prompts:
- `GetOrPromptForApiIdAsync()`
- `PromptAsync<T>()`
- `OptionOrAskAsync()`

### 8. **Nested Local Functions**
Some commands use local functions for complex logic:
```csharp
async Task CreateNewMock() { ... }
```

---

## AOT (Ahead-of-Time) Compatibility

Most parent commands are annotated:
```csharp
#if !NET9_0_OR_GREATER
[RequiresDynamicCode(...)]
[RequiresUnreferencedCode(...)]
#endif
internal sealed class XxxCommand : Command
```

This allows the same code to work on older .NET versions (with JIT) and .NET 9+ (with AOT).

---

## Testing Observations

Based on the implementation patterns, testable units include:

1. **Option parsing & validation** - mock `ParseResult`
2. **Client interaction** - mock client interfaces (`IApisClient`, etc.)
3. **Error handling** - test specific error types and messages
4. **Interactive vs non-interactive** - test both code paths
5. **Mutation result handling** - test various error and success states
6. **Session management** - test authentication checks
7. **File I/O** - mock `IFileSystem`
8. **Console output** - capture/verify messages

No "new" pattern was discovered — all commands follow the existing System.CommandLine pattern with extensions for Nitro-specific concerns.
