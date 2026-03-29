# Validation Error Types and Handling Research

## Base Error Interfaces (from IError)

These are the core error types that inherit directly from `IError`:

| Interface | Properties | Base | Description |
|-----------|-----------|------|-------------|
| `IValidationError` | `Message`, `Errors` (IReadOnlyList) | IError | Generic validation error with field-level errors |
| `IStageValidationError` | `Message` | IError | Stage-specific validation failures |
| `IOpenApiCollectionValidationError` | `Collections` (validation collection list) | IError | OpenAPI collection validation failures with nested entity errors |
| `IMcpFeatureCollectionValidationError` | `Collections` (validation collection list) | IError | MCP Feature Collection validation failures with nested entity errors |
| `IPersistedQueryValidationError` | `Client`, `Message`, `Queries` | IError | GraphQL persisted query validation errors with error details |
| `IApiNotFoundError` | `Message` | IError | API resource not found |
| `IMockSchemaNonUniqueNameError` | `Message` | IError | Mock schema name already exists |
| `IMockSchemaNotFoundError` | `Message` | IError | Mock schema resource not found |
| `ISchemaNotFoundError` | `Message` | IError | Schema resource not found |
| `IUnauthorizedOperation` | `Message` | IError | User not authorized for operation |
| `IConcurrentOperationError` | `Message` | IError | Concurrent operation already in progress |
| `IUnexpectedProcessingError` | `Message` | IError | Unexpected error during processing |
| `IProcessingTimeoutError` | `Message` | IError | Operation timed out |
| `IOperationsAreNotAllowedError` | `Message` | IError | Operations disallowed in current state |
| `ISchemaVersionSyntaxError` | `Message`, `Line`, `Column`, `Position` | IError | GraphQL schema syntax error with location |
| `ISchemaChangeViolationError` | `Message`, `Changes` (schema changes list) | IError | Breaking schema changes detected |
| `IInvalidGraphQLSchemaError` | `Message`, `Errors` (GraphQL errors) | IError | Invalid GraphQL schema with detailed errors |
| `IStagesHavePublishedDependenciesError` | `Message`, `Stages` (stage list) | IError | Cannot delete/modify stages with published content |
| `ISubgraphInvalidError` | `Message` | IError | Invalid subgraph definition |
| `IClientNotFoundError` | `ClientId`, `Message` | IError | Client resource not found |
| `IClientVersionNotFoundError` | `ClientId`, `Tag`, `Message` | IError | Client version not found |
| `IStageNotFoundError` | `Name`, `Message` | IError | Stage not found by name |
| Other \*NotFoundError variants | `Message` | IError | Various resource-not-found errors |

## Nested Validation Error Structures

### ValidationError Errors
- **Property**: `Errors` property contains `IValidationErrorProperty` items
- **Structure**: Each property has `message` field
- **Used in**: Mutation responses like `CreateMockSchema`, `CreateWorkspace`, `CreateApiKey`
- **GraphQL Fragment** (fragments.graphql:626):
  ```graphql
  fragment ValidationError on ValidationError {
    __typename
    message
    errors {
      message
    }
    ...Error
  }
  ```

### OpenApiCollectionValidationError Nested Structure
- **Collections**: List of `IOpenApiCollectionValidationCollection`
  - **OpenApiCollection**: Reference with `id`, `name`
  - **Entities**: List of `IOpenApiCollectionValidationEntity`
    - **Endpoint Entity**: Has `httpMethod`, `route` properties
    - **Model Entity**: Has `name` property
    - **Errors**: Can be either:
      - `IOpenApiCollectionValidationDocumentError` - document parsing errors with `code`, `message`, `path`, `locations[{line, column}]`
      - `IOpenApiCollectionValidationEntityValidationError` - validation error with `message`

### McpFeatureCollectionValidationError Nested Structure
- **Collections**: List of `IMcpFeatureCollectionValidationCollection`
  - **McpFeatureCollection**: Reference with `id`, `name`
  - **Entities**: List of `IMcpFeatureCollectionValidationEntity`
    - **Prompt Entity**: Has `name` property
    - **Tool Entity**: Has `name` property
    - **Errors**: Can be either:
      - `IMcpFeatureCollectionValidationDocumentError` - document parsing errors with `code`, `message`, `path`, `locations[{line, column}]`
      - `IMcpFeatureCollectionValidationEntityValidationError` - validation error with `message`

### PersistedQueryValidationError Nested Structure
- **Client**: Reference to client with `Id`, `Name`
- **Queries**: List of persisted query validation info with:
  - `Message`, `Hash`
  - `DeployedTags`: List of deployed version tags
  - `Errors`: List of GraphQL location errors with `message`, `locations[{line, column}]`

## Mutation and Subscription Commands

### Mutation-Based Commands (sync error handling via PrintMutationErrorsAndExit)

#### Command: Create Operations
- **CreateMockCommand** → CreateMockSchema mutation
  - Errors: ApiNotFoundError, MockSchemaNonUniqueNameError, UnauthorizedOperation, ValidationError

- **CreateWorkspaceCommand** → CreateWorkspace mutation
  - Errors: ValidationError

- **CreateApiKeyCommand** → CreateApiKey mutation
  - Errors: ValidationError

#### Command: Update/Edit Operations
- **UpdateMockSchema** mutation
  - Errors: ValidationError

- **EditStagesCommand** → UpdateStages mutation
  - Errors: StageValidationError

#### Command: Delete Operations
- **DeleteApiKeyCommand**
  - No mutation errors documented (likely fail-fast on not found)

### Subscription-Based Commands (async error handling via switch expressions)

These commands use long-running operations with subscriptions:

#### Clients Publishing
- **PublishClientCommand**
  - Initial mutation: `publishClientVersion`
  - Subscription: `onClientVersionPublishUpdated(requestId)`
  - Switch cases:
    - `IClientVersionPublishFailed { Errors }` → calls `PrintMutationErrors()`
    - `IClientVersionPublishSuccess` → success
    - Other state updates: IProcessingTaskIsQueued, IProcessingTaskIsReady, IOperationInProgress, IWaitForApproval, IProcessingTaskApproved
  - Activity tracking: `console.StartActivity("Publishing...")` with `.Update()` calls

- **ValidateClientCommand**
  - Similar pattern with validation subscription

#### Schemas Publishing
- **PublishSchemaCommand**
  - Uses schema publishing subscription
  - Handles SchemaVersionPublishFailed, SchemaVersionPublishSuccess states

- **ValidateSchemaCommand**
  - Schema validation subscription

#### Collections Publishing
- **PublishOpenApiCollectionCommand**
  - Subscription: `onOpenApiCollectionVersionPublishingUpdate`
  - Errors in subscription: OpenApiCollectionValidationError, McpFeatureCollectionValidationError, PersistedQueryValidationError

- **ValidateOpenApiCollectionCommand**
  - OpenAPI collection validation subscription

- **PublishMcpFeatureCollectionCommand**
  - Subscription: `onMcpFeatureCollectionVersionPublishingUpdate`
  - Similar nested validation error structure

- **ValidateMcpFeatureCollectionCommand**
  - MCP Feature Collection validation subscription

## Error Handling in ConsoleHelpers.cs

### PrintMutationError() Overloads

The `PrintMutationError` method has specific overloads for structured error types:

1. **ISchemaVersionChangeViolationError / ISchemaChangeViolationError** - Renders tree of schema changes
2. **IPersistedQueryValidationError** - Shows client name, message, tree of queries with errors and locations
3. **IOpenApiCollectionValidationError** - Shows collections, entities (endpoints/models), and nested errors with locations
4. **IMcpFeatureCollectionValidationError** - Shows collections, entities (prompts/tools), and nested errors with locations
5. **IInvalidGraphQLSchemaError** - Shows schema syntax errors in tree format with error codes
6. **IStagesHavePublishedDependenciesError** - Lists stages and their published schema/clients

### Generic Fallback Handling

For error types without specific overloads, falls through to generic switch in `PrintMutationError(object error)`:
- IOperationsAreNotAllowedError → println(Message)
- IConcurrentOperationError → println(Message)
- IUnexpectedProcessingError → println(Message)
- IProcessingTimeoutError → println(Message)
- ISchemaVersionSyntaxError → println(Message)
- Archive errors → special message about invalid archive
- IError → generic error message
- else → "Unexpected mutation error"

## Activity Tracking Pattern

Subscription-based commands use `console.StartActivity()` with `.Update()` during operation:

```csharp
await using (var activity = console.StartActivity("Publishing..."))
{
    // ... initial setup ...

    await foreach (var update in client.SubscribeToAsync(...))
    {
        switch (update)
        {
            case IStateQueued v:
                activity.Update($"Your request is queued...");
                break;
            case IFailedState { Errors: var errors }:
                console.PrintMutationErrors(errors);  // Print structured errors
                return ExitCodes.Error;
            case ISuccessState:
                console.Success("Successfully completed!");
                return ExitCodes.Success;
            // ... other states ...
        }
    }
}
```

## Key Patterns

1. **Validation Error Hierarchy**: Generic `IValidationError` with flat `errors` list → Specialized collection errors with nested entity structures
2. **Error Rendering**: Complex error types (validation, schema changes) get tree-rendered; simple errors get one-line message
3. **Activity Updates**: Long operations use activities with `.Update()` to show progress
4. **Error Exit Pattern**:
   - Sync mutations: `PrintMutationErrorsAndExit()` throws ExitException immediately
   - Async subscriptions: `PrintMutationErrors()` logs then return ExitCodes.Error
5. **Location Tracking**: Validation/parsing errors capture line/column for source context
6. **Nested Entity Organization**: Collections errors group by collection → entity → error, allowing detailed scoped error messages

## Generated Types in ApiClient.Client.cs

All error interfaces are generated from GraphQL by StrawberryShake code generator (version 11.0.0). The generated types follow naming convention:
- Direct GraphQL type: `I{TypeName}`
- Mutation-specific: `I{MutationName}_{Field}_{ErrorType}`
- Subscription-specific: `I{SubscriptionName}_{Field}_{ErrorType}`

Example: `ICreateMockSchema_CreateMockSchema_Errors_ValidationError` is the ValidationError type in the CreateMockSchema mutation's errors field.
