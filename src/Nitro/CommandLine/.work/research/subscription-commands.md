# Subscription-Based Commands Research

## Overview
This document catalogs all Nitro CLI commands that use GraphQL subscriptions. Subscriptions are invoked via `await foreach` patterns on methods returning `IAsyncEnumerable<T>` with names containing `SubscribeTo*Async`.

---

## Commands Using Subscriptions

### Schema Commands

#### 1. ValidateSchemaCommand
**File:** `/src/CommandLine/Commands/Schemas/ValidateSchemaCommand.cs`

**Subscription Method:** `client.SubscribeToSchemaValidationAsync(requestId, ct)`

**ExecuteAsync Code (lines 37-127):**
```csharp
private static async Task<int> ExecuteAsync(
    ParseResult parseResult,
    INitroConsole console,
    ISchemasClient client,
    IFileSystem fileSystem,
    ISessionService sessionService,
    CancellationToken ct)
{
    parseResult.AssertHasAuthentication(sessionService);

    var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
    var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
    var schemaFilePath = parseResult.GetValue(Opt<SchemaFileOption>.Instance)!;
    var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

    var source = SourceMetadataParser.Parse(sourceMetadataJson);

    await using (var activity = console.StartActivity("Validating schema..."))
    {
        await using var stream = fileSystem.OpenReadStream(schemaFilePath);

        var validationRequest = await client.StartSchemaValidationAsync(
            apiId,
            stage,
            stream,
            source,
            ct);

        if (validationRequest.Errors?.Count > 0)
        {
            activity.Fail();

            foreach (var error in validationRequest.Errors)
            {
                var errorMessage = error switch
                {
                    IValidateSchemaVersion_ValidateSchema_Errors_UnauthorizedOperation err => err.Message,
                    IValidateSchemaVersion_ValidateSchema_Errors_ApiNotFoundError err => err.Message,
                    IValidateSchemaVersion_ValidateSchema_Errors_StageNotFoundError err => err.Message,
                    IValidateSchemaVersion_ValidateSchema_Errors_SchemaNotFoundError err => err.Message,
                    IError err => "Unexpected mutation error: " + err.Message,
                    _ => "Unexpected mutation error."
                };

                await console.Error.WriteLineAsync(errorMessage);
            }

            return ExitCodes.Error;
        }

        if (validationRequest.Id is not { } requestId)
        {
            throw new ExitException("Could not create schema validation request.");
        }

        activity.Update($"Validation request created (ID: {requestId.EscapeMarkup()})");

        await foreach (var update in client.SubscribeToSchemaValidationAsync(requestId, ct))
        {
            switch (update)
            {
                case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                    activity.Fail();
                    // TODO: This should be more explicit
                    console.PrintMutationErrors(schemaErrors);

                    // TODO: Also output as result.
                    return ExitCodes.Error;

                case ISchemaVersionValidationSuccess:
                    activity.Success("Schema validation succeeded.");
                    return ExitCodes.Success;

                case IOperationInProgress:
                case IValidationInProgress:
                    activity.Update("The schema validation is in progress.");
                    break;

                default:
                    // TODO: Pull this out into a error messages class so other commands can use it.
                    activity.Update(
                        "Warning: Received a unknown server response. Ensure your CLI is on the latest version.");
                    break;
            }
        }

        activity.Fail();
    }

    return ExitCodes.Error;
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` on `IOperationInProgress` / `IValidationInProgress`
- `activity.Success()` on success, `activity.Fail()` on failure

**Error Handling:**
- Initial mutation errors: checked with `validationRequest.Errors?.Count > 0` and printed individually
- Subscription errors: uses `console.PrintMutationErrors(schemaErrors)` for `ISchemaVersionValidationFailed`
- **Note:** Has TODO comments about error reporting being not explicit enough

**Exit Behavior:**
- Returns `ExitCodes.Error` for mutation errors
- Returns `ExitCodes.Success` for success
- Falls through to `ExitCodes.Error` if subscription ends without clear success


#### 2. PublishSchemaCommand
**File:** `/src/CommandLine/Commands/Schemas/PublishSchemaCommand.cs`

**Subscription Method:** `client.SubscribeToSchemaPublishAsync(requestId, ct)`

**ExecuteAsync Code (lines 37-159):**
```csharp
private static async Task<int> ExecuteAsync(
    ParseResult parseResult,
    INitroConsole console,
    ISchemasClient client,
    ISessionService sessionService,
    CancellationToken ct)
{
    parseResult.AssertHasAuthentication(sessionService);

    var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
    var stage = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
    var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
    var force = parseResult.GetValue(Opt<ForceOption>.Instance);
    var waitForApproval = parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
    var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

    var source = SourceMetadataParser.Parse(sourceMetadataJson);

    await using (var activity = console.StartActivity("Publishing..."))
    {
        console.Log("Initialized");

        if (force)
        {
            console.Log("[yellow]Force push is enabled[/]");
        }

        console.Log("Create publish request");

        var publishRequest = await client.StartSchemaPublishAsync(
            apiId,
            stage,
            tag,
            force,
            waitForApproval,
            source,
            ct);

        if (publishRequest.Errors?.Count > 0)
        {
            activity.Fail();

            foreach (var error in publishRequest.Errors)
            {
                var errorMessage = error switch
                {
                    IPublishSchemaVersion_PublishSchema_Errors_UnauthorizedOperation err => err.Message,
                    IPublishSchemaVersion_PublishSchema_Errors_ApiNotFoundError err => err.Message,
                    IPublishSchemaVersion_PublishSchema_Errors_StageNotFoundError err => err.Message,
                    IPublishSchemaVersion_PublishSchema_Errors_SchemaNotFoundError err => err.Message,
                    IError err => "Unexpected mutation error: " + err.Message,
                    _ => "Unexpected mutation error."
                };

                await console.Error.WriteLineAsync(errorMessage);
                return ExitCodes.Error;
            }
        }

        if (publishRequest.Id is not { } requestId)
        {
            activity.Fail();
            await console.Error.WriteLineAsync("Could not create publish request.");
            return ExitCodes.Error;
        }

        console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

        await foreach (var update in client.SubscribeToSchemaPublishAsync(requestId, ct))
        {
            switch (update)
            {
                case IProcessingTaskIsQueued v:
                    activity.Update(
                        $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                    break;

                case ISchemaVersionPublishFailed { Errors: var schemaErrors }:
                    activity.Fail();
                    console.WriteLine("Schema publish failed");
                    console.PrintMutationErrors(schemaErrors);
                    return ExitCodes.Error;

                case ISchemaVersionPublishSuccess:
                    activity.Success("Successfully published schema!");
                    return ExitCodes.Success;

                case IProcessingTaskIsReady:
                    console.Success("Your request is ready for the committing.");
                    break;

                case IOperationInProgress:
                    activity.Update("The committing of your request is in progress.");
                    break;

                case IWaitForApproval waitForApprovalEvent:
                    if (waitForApprovalEvent.Deployment is
                        IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Deployment_SchemaDeployment deployment)
                    {
                        console.PrintMutationErrors(deployment.Errors);
                    }

                    activity.Update(
                        "The committing of your request is waiting for approval. Check Nitro to approve the request.");
                    break;

                case IProcessingTaskApproved:
                    activity.Update("The committing of your request is approved.");
                    break;

                default:
                    activity.Update(
                        "This is an unknown response, upgrade Nitro CLI to the latest version.");
                    break;
            }
        }

        activity.Fail();
    }

    return ExitCodes.Error;
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` for queue position, in-progress, and approval states
- `activity.Success()` on success, `activity.Fail()` on failure or timeout
- `console.Log()` for informational messages during initialization
- `console.Success()` for state transitions (ready, approval states)

**Error Handling:**
- Initial mutation errors: checked and printed individually with pattern matching
- Subscription errors: uses `console.PrintMutationErrors(schemaErrors)` for `ISchemaVersionPublishFailed`
- Handles deployment errors in approval state

**Exit Behavior:**
- Returns `ExitCodes.Error` for any error in initialization or subscription
- Returns `ExitCodes.Success` on success
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


---

### Client Commands

#### 3. ValidateClientCommand
**File:** `/src/CommandLine/Commands/Clients/ValidateClientCommand.cs`

**Subscription Method:** `client.SubscribeToClientValidationAsync(requestId, ct)`

**ExecuteAsync Code (lines 37-101):**
```csharp
private static async Task<int> ExecuteAsync(
    INitroConsole console,
    IClientsClient client,
    IFileSystem fileSystem,
    string stage,
    string clientId,
    string operationsFilePath,
    string? sourceMetadataJson,
    CancellationToken ct)
{
    var source = SourceMetadataParser.Parse(sourceMetadataJson);

    await using (var activity = console.StartActivity("Validating..."))
    {
        console.Log("Initialized");
        console.Log($"Reading file [blue]{operationsFilePath.EscapeMarkup()}[/]");

        await using var stream = fileSystem.OpenReadStream(operationsFilePath);

        console.Log("Create validation request");

        var validationRequest = await client.StartClientValidationAsync(
            clientId,
            stage,
            stream,
            source,
            ct);

        console.PrintMutationErrorsAndExit(validationRequest.Errors);
        if (validationRequest.Id is not { } requestId)
        {
            throw new ExitException("Could not create validation request!");
        }

        console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

        await foreach (var update in client.SubscribeToClientValidationAsync(requestId, ct))
        {
            switch (update)
            {
                case IClientVersionValidationFailed { Errors: var errors }:
                    console.WriteLine("The client is invalid:");
                    console.PrintMutationErrors(errors);
                    return ExitCodes.Error;

                case IClientVersionValidationSuccess:
                    console.Success("Client validation succeeded");
                    return ExitCodes.Success;

                case IOperationInProgress:
                case IValidationInProgress:
                    activity.Update("The validation is in progress.");
                    break;

                default:
                    activity.Update(
                        "This is an unknown response, upgrade Nitro CLI to the latest version.");
                    break;
            }
        }
    }

    return ExitCodes.Error;
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` for in-progress states
- `console.Success()` on success
- `console.Log()` for informational messages during initialization

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit()`** — exits immediately on mutation errors
- Subscription errors: uses `console.PrintMutationErrors(errors)` for `IClientVersionValidationFailed`

**Exit Behavior:**
- Exits immediately (via `PrintMutationErrorsAndExit`) if mutation fails
- Returns `ExitCodes.Success` on validation success
- Returns `ExitCodes.Error` for validation failures
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


#### 4. PublishClientCommand
**File:** `/src/CommandLine/Commands/Clients/PublishClientCommand.cs`

**Subscription Method:** `client.SubscribeToClientPublishAsync(requestId, ct)`

**ExecuteAsync Code (lines 38-130):**
```csharp
private static async Task<int> ExecuteAsync(
    INitroConsole console,
    IClientsClient client,
    string tag,
    string stage,
    string clientId,
    bool force,
    bool waitForApproval,
    string? sourceMetadataJson,
    CancellationToken ct)
{
    var source = SourceMetadataParser.Parse(sourceMetadataJson);

    await using (var activity = console.StartActivity("Publishing..."))
    {
        console.Log("Initialized");

        if (force)
        {
            console.Log("[yellow]Force push is enabled[/]");
        }

        console.Log("Create publish request");

        var publishRequest = await client.StartClientPublishAsync(
            clientId,
            stage,
            tag,
            force,
            waitForApproval,
            source,
            ct);

        console.PrintMutationErrorsAndExit(publishRequest.Errors);
        if (publishRequest.Id is not { } requestId)
        {
            throw new ExitException("Could not create publish request!");
        }

        console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

        await foreach (var update in client.SubscribeToClientPublishAsync(requestId, ct))
        {
            switch (update)
            {
                case IProcessingTaskIsQueued v:
                    activity.Update(
                        $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
                    break;

                case IClientVersionPublishFailed { Errors: var errors }:
                    console.WriteLine("Client publish failed");
                    console.PrintMutationErrors(errors);
                    return ExitCodes.Error;

                case IClientVersionPublishSuccess:
                    console.Success("Successfully published client!");
                    return ExitCodes.Success;

                case IProcessingTaskIsReady:
                    console.Success("Your request is ready for the committing.");
                    break;

                case IOperationInProgress:
                    activity.Update("The committing of your request is in progress.");
                    break;

                case IWaitForApproval waitForApprovalEvent:
                    if (waitForApprovalEvent.Deployment is
                        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_ClientDeployment deployment)
                    {
                        console.PrintMutationErrors(deployment.Errors);
                    }

                    activity.Update(
                        "The committing of your request is waiting for approval. Check Nitro to approve the request.");
                    break;

                case IProcessingTaskApproved:
                    activity.Update("The committing of your request is approved.");
                    break;

                default:
                    activity.Update(
                        "This is an unknown response, upgrade Nitro CLI to the latest version.");
                    break;
            }
        }
    }

    return ExitCodes.Error;
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` for queue position, in-progress, and approval states
- `console.Success()` for success and state transitions
- `console.Log()` for informational messages during initialization

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit()`**
- Subscription errors: uses `console.PrintMutationErrors(errors)` for `IClientVersionPublishFailed`
- Handles deployment errors in approval state

**Exit Behavior:**
- Exits immediately on mutation errors
- Returns `ExitCodes.Success` on successful publish
- Returns `ExitCodes.Error` on publish failure
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


---

### OpenAPI Commands

#### 5. ValidateOpenApiCollectionCommand
**File:** `/src/CommandLine/Commands/OpenApi/ValidateOpenApiCollectionCommand.cs`

**Subscription Method:** `client.SubscribeToOpenApiCollectionValidationAsync(requestId, ct)`

**Relevant Code (lines 74-117):**
```csharp
var validationRequest = await client.StartOpenApiCollectionValidationAsync(
    openApiCollectionId,
    stage,
    archiveStream,
    source,
    ct);

console.PrintMutationErrorsAndExit(validationRequest.Errors);
if (validationRequest.Id is not { } requestId)
{
    throw new ExitException("Could not create validation request!");
}

console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

await foreach (var update in client.SubscribeToOpenApiCollectionValidationAsync(requestId, ct))
{
    switch (update)
    {
        case IOpenApiCollectionVersionValidationFailed { Errors: var errors }:
            console.WriteLine("The OpenAPI collection is invalid:");
            console.PrintMutationErrors(errors);
            return ExitCodes.Error;

        case IOpenApiCollectionVersionValidationSuccess:
            console.Success("OpenAPI collection validation succeeded");
            return ExitCodes.Success;

        case IOperationInProgress:
        case IValidationInProgress:
            activity.Update("The validation is in progress.");
            break;

        default:
            activity.Update(
                "This is an unknown response, upgrade Nitro CLI to the latest version.");
            break;
    }
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` for in-progress states
- `console.Success()` on success
- `console.Log()` for validation request creation

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit()`**
- Subscription errors: uses `console.PrintMutationErrors(errors)` for failed validation

**Exit Behavior:**
- Exits immediately on mutation errors
- Returns `ExitCodes.Success` on validation success
- Returns `ExitCodes.Error` for validation failures
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


#### 6. PublishOpenApiCollectionCommand
**File:** `/src/CommandLine/Commands/OpenApi/PublishOpenApiCollectionCommand.cs`

**Subscription Method:** `client.SubscribeToOpenApiCollectionPublishAsync(requestId, ct)`

**Relevant Code (lines 61-129):**
```csharp
var publishRequest = await client.StartOpenApiCollectionPublishAsync(
    openApiCollectionId,
    stage,
    tag,
    force,
    waitForApproval,
    source,
    ct);

console.PrintMutationErrorsAndExit(publishRequest.Errors);
if (publishRequest.Id is not { } requestId)
{
    throw new ExitException("Could not create publish request!");
}

console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

await foreach (var update in client.SubscribeToOpenApiCollectionPublishAsync(requestId, ct))
{
    switch (update)
    {
        case IProcessingTaskIsQueued v:
            activity.Update(
                $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
            break;

        case IOpenApiCollectionVersionPublishFailed { Errors: var errors }:
            console.WriteLine("OpenAPI collection publish failed");
            console.PrintMutationErrors(errors);
            return ExitCodes.Error;

        case IOpenApiCollectionVersionPublishSuccess:
            console.Success("Successfully published OpenAPI collection!");
            return ExitCodes.Success;

        case IProcessingTaskIsReady:
            console.Success("Your request is ready for processing.");
            break;

        case IOperationInProgress:
            activity.Update("Your request is in progress.");
            break;

        case IWaitForApproval waitForApprovalEvent:
            if (waitForApprovalEvent.Deployment is
                IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Deployment_OpenApiCollectionDeployment deployment)
            {
                console.PrintMutationErrors(deployment.Errors);
            }

            activity.Update(
                "The processing of your request is waiting for approval. Check Nitro to approve the request.");
            break;

        case IProcessingTaskApproved:
            activity.Update("The processing of your request is approved.");
            break;

        default:
            activity.Update(
                "This is an unknown response, upgrade Nitro CLI to the latest version.");
            break;
    }
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` for queue position, in-progress, and approval states
- `console.Success()` for success and state transitions
- `console.Log()` for publish request creation

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit()`**
- Subscription errors: uses `console.PrintMutationErrors(errors)` for publish failures
- Handles deployment errors in approval state

**Exit Behavior:**
- Exits immediately on mutation errors
- Returns `ExitCodes.Success` on successful publish
- Returns `ExitCodes.Error` on publish failure
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


---

### MCP Commands

#### 7. ValidateMcpFeatureCollectionCommand
**File:** `/src/CommandLine/Commands/Mcp/ValidateMcpFeatureCollectionCommand.cs`

**Subscription Method:** `client.SubscribeToMcpFeatureCollectionValidationAsync(requestId, ct)`

**Relevant Code (lines 78-120):**
```csharp
var validationRequest = await client.StartMcpFeatureCollectionValidationAsync(
    mcpFeatureCollectionId,
    stage,
    archiveStream,
    source,
    ct);

console.PrintMutationErrorsAndExit(validationRequest.Errors);
if (validationRequest.Id is not { } requestId)
{
    throw new ExitException("Could not create validation request!");
}

console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

await foreach (var update in client.SubscribeToMcpFeatureCollectionValidationAsync(requestId, ct))
{
    switch (update)
    {
        case IMcpFeatureCollectionVersionValidationFailed { Errors: var errors }:
            console.ErrorLine("The MCP Feature Collection is invalid:");
            console.PrintMutationErrors(errors);
            return ExitCodes.Error;

        case IMcpFeatureCollectionVersionValidationSuccess:
            console.Success("MCP Feature Collection validation succeeded");
            return ExitCodes.Success;

        case IOperationInProgress:
        case IValidationInProgress:
            activity.Update("The validation is in progress.");
            break;

        default:
            activity.Update(
                "This is an unknown response, upgrade Nitro CLI to the latest version.");
            break;
    }
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` for in-progress states
- `console.Success()` on success
- `console.Log()` for validation request creation
- `console.ErrorLine()` for validation failure (note: different from other commands)

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit()`**
- Subscription errors: uses `console.PrintMutationErrors(errors)` for failed validation

**Exit Behavior:**
- Exits immediately on mutation errors
- Returns `ExitCodes.Success` on validation success
- Returns `ExitCodes.Error` for validation failures
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


#### 8. PublishMcpFeatureCollectionCommand
**File:** `/src/CommandLine/Commands/Mcp/PublishMcpFeatureCollectionCommand.cs`

**Subscription Method:** `client.SubscribeToMcpFeatureCollectionPublishAsync(requestId, ct)`

**Relevant Code (lines 56-123):**
```csharp
var publishRequest = await client.StartMcpFeatureCollectionPublishAsync(
    mcpFeatureCollectionId,
    stage,
    tag,
    force,
    waitForApproval,
    source,
    ct);

console.PrintMutationErrorsAndExit(publishRequest.Errors);
if (publishRequest.Id is not { } requestId)
{
    throw new ExitException("Could not create publish request!");
}

console.Log($"Publish request created [grey](ID: {requestId.EscapeMarkup()})[/]");

await foreach (var update in client.SubscribeToMcpFeatureCollectionPublishAsync(requestId, ct))
{
    switch (update)
    {
        case IProcessingTaskIsQueued v:
            activity.Update(
                $"Your request is queued. The current position in the queue is {v.QueuePosition}.");
            break;

        case IMcpFeatureCollectionVersionPublishFailed { Errors: var errors }:
            console.ErrorLine("MCP Feature Collection publish failed");
            console.PrintMutationErrors(errors);
            return ExitCodes.Error;

        case IMcpFeatureCollectionVersionPublishSuccess:
            console.Success("Successfully published MCP Feature Collection!");
            return ExitCodes.Success;

        case IProcessingTaskIsReady:
            console.Success("Your request is ready for processing.");
            break;

        case IOperationInProgress:
            activity.Update("Your request is in progress.");
            break;

        case IWaitForApproval waitForApprovalEvent:
            if (waitForApprovalEvent.Deployment is IMcpFeatureCollectionDeployment deployment)
            {
                console.PrintMutationErrors(deployment.Errors);
            }

            activity.Update(
                "The processing of your request is waiting for approval. Check Nitro to approve the request.");
            break;

        case IProcessingTaskApproved:
            activity.Update("The processing of your request is approved.");
            break;

        default:
            activity.Update(
                "This is an unknown response, upgrade Nitro CLI to the latest version.");
            break;
    }
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress
- `activity.Update()` for queue position, in-progress, and approval states
- `console.Success()` for success and state transitions
- `console.Log()` for publish request creation
- `console.ErrorLine()` for publish failure (note: different from other commands)

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit()`**
- Subscription errors: uses `console.PrintMutationErrors(errors)` for publish failures
- Handles deployment errors in approval state (note: simplified type check vs. OpenAPI/Client commands)

**Exit Behavior:**
- Exits immediately on mutation errors
- Returns `ExitCodes.Success` on successful publish
- Returns `ExitCodes.Error` on publish failure
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


---

### Fusion Commands

#### 9. FusionValidateCommand
**File:** `/src/CommandLine/Commands/Fusion/FusionValidateCommand.cs`

**Subscription Method:** `fusionConfigurationClient.SubscribeToSchemaVersionValidationUpdatedAsync(requestId, ct)`

**Relevant Code (lines 184-226):**
```csharp
async Task ValidateSchemaAsync(INitroConsoleActivity activity, Stream schemaStream)
{
    console.Log("Create validation request");

    var requestId = await ValidateAsync(
        console,
        fusionConfigurationClient,
        apiId,
        stageName,
        schemaStream,
        ct);

    console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

    await foreach (var @event in fusionConfigurationClient
        .SubscribeToSchemaVersionValidationUpdatedAsync(requestId, ct))
    {
        switch (@event)
        {
            case ISchemaVersionValidationFailed v:
                console.WriteLine("The schema is invalid:");
                console.PrintMutationErrors(v.Errors);

                isValid = false;
                return;

            case ISchemaVersionValidationSuccess:
                isValid = true;
                console.Success("Schema validation succeeded.");
                return;

            case IOperationInProgress:
            case IValidationInProgress:
                activity.Update("The validation is in progress.");
                break;

            default:
                activity.Update(
                    "This is an unknown response, upgrade Nitro CLI to the latest version.");
                break;
        }
    }
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress (outer method)
- `activity.Update()` for in-progress states
- `console.Success()` on success
- `console.Log()` for validation request creation

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit(validationRequest.Errors)`** in `ValidateAsync()` method
- Subscription errors: uses `console.PrintMutationErrors(v.Errors)` for `ISchemaVersionValidationFailed`

**Exit Behavior:**
- Exits immediately on mutation errors (via `PrintMutationErrorsAndExit`)
- Sets `isValid = true` on validation success
- Sets `isValid = false` on validation failure
- Returns `ExitCodes.Success` if `isValid == true`, otherwise `ExitCodes.Error`


#### 10. FusionConfigurationPublishValidateCommand
**File:** `/src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishValidateCommand.cs`

**Subscription Method:** `fusionConfigurationClient.SubscribeToFusionConfigurationPublishingTaskChangedAsync(requestId, cancellationToken)`

**Relevant Code (lines 49-102):**
```csharp
async Task<int> ValidateAsync(INitroConsoleActivity activity)
{
    console.Log("Initialized");

    await using var stream = fileSystem.OpenReadStream(archiveFile);

    var result = await fusionConfigurationClient.ValidateFusionConfigurationPublishAsync(
        requestId,
        stream,
        cancellationToken);
    console.PrintMutationErrorsAndExit(result.Errors);

    await foreach (var @event in fusionConfigurationClient
        .SubscribeToFusionConfigurationPublishingTaskChangedAsync(requestId, cancellationToken))
    {
        switch (@event)
        {
            case IProcessingTaskIsQueued:
                throw Exit(
                    "Your request is in the queued state. Try to run `fusion-configuration publish start` once the request is ready ");

            case IFusionConfigurationPublishingFailed:
                throw Exit("Your request has already failed");

            case IFusionConfigurationPublishingSuccess:
                throw Exit("You request is already published");

            case IProcessingTaskIsReady:
                throw Exit(
                    "Your request is ready for the composition. Run `fusion-configuration publish start`");

            case IFusionConfigurationValidationFailed failed:
                console.WriteLine("The validation failed:");
                console.PrintMutationErrors(failed.Errors);
                return ExitCodes.Error;

            case IFusionConfigurationValidationSuccess:
                console.Success("The validation was successful");
                return ExitCodes.Success;

            case IOperationInProgress:
            case IValidationInProgress:
            case IWaitForApproval:
            case IProcessingTaskApproved:
                activity.Update("The validation is in progress");
                break;

            default:
                throw Exit("Unknown response");
        }
    }

    return ExitCodes.Error;
}
```

**Activity/Progress Reporting:**
- Uses `console.StartActivity()` for top-level progress (outer method)
- `activity.Update()` for in-progress and approval states
- `console.Success()` on success

**Error Handling:**
- Initial mutation errors: uses **`console.PrintMutationErrorsAndExit(result.Errors)`**
- Subscription errors: uses `console.PrintMutationErrors(failed.Errors)` for `IFusionConfigurationValidationFailed`
- **Note:** Throws `ExitException` via `Exit()` for unexpected states (queued, failed, success, ready)

**Exit Behavior:**
- Exits immediately on mutation errors
- Throws exception for unexpected state transitions
- Returns `ExitCodes.Success` on validation success
- Returns `ExitCodes.Error` on validation failure
- Falls through to `ExitCodes.Error` if subscription ends without explicit exit


---

## Commands NOT Using Subscriptions

### Schema Commands
- **DownloadSchemaCommand:** Simple async operation, no subscription
- **UploadSchemaCommand:** Simple mutation, no subscription

### Client Commands
- **DownloadClientCommand:** Uses `IAsyncEnumerable` but for JSON deserialization stream, not GraphQL subscription

### Fusion Commands
- **FusionComposeCommand:** Uses channels and file watchers for watch mode, not GraphQL subscriptions
- **FusionPublishCommand:** Not analyzed (appears to be a container command)
- **FusionDownloadCommand:** Not analyzed
- **FusionMigrateCommand:** Not analyzed
- **FusionRunCommand:** Not analyzed
- **FusionUploadCommand:** Not analyzed

---

## Patterns and Observations

### Error Handling Patterns

1. **PrintMutationErrorsAndExit vs PrintMutationErrors**
   - **`PrintMutationErrorsAndExit()`**: Used during initial mutation (request creation phase)
     - Commands: ValidateClientCommand, PublishClientCommand, ValidateOpenApiCollectionCommand, PublishOpenApiCollectionCommand, ValidateMcpFeatureCollectionCommand, PublishMcpFeatureCollectionCommand, FusionValidateCommand, FusionConfigurationPublishValidateCommand
   - **`PrintMutationErrors()`**: Used within subscription switch statement for error states
     - All commands with subscriptions

2. **Activity/Progress Reporting**
   - All commands use `console.StartActivity()` wrapper
   - Update activity with `.Update()`, `.Success()`, or `.Fail()`
   - Most provide queue position or operation state updates
   - Some use `console.Log()` for informational messages during setup

3. **Exit Code Pattern**
   - `ExitCodes.Success` returned explicitly on success
   - `ExitCodes.Error` returned on failure
   - Default fallthrough to `ExitCodes.Error` if subscription ends without terminal state

### State Machine Pattern

All publish/validation commands follow a similar state machine:
1. Create request (mutation with error check)
2. Subscribe to updates
3. Handle queue states (if applicable)
4. Handle in-progress states
5. Handle approval states (if applicable)
6. Handle terminal states (success/failure)
7. Handle unknown states with default message

### Activity Lifecycle Pattern

```csharp
await using (var activity = console.StartActivity("Operation..."))
{
    // initialization
    // request creation
    // subscription loop
    activity.Fail(); // default if no explicit terminal state
}
return ExitCodes.Error; // default exit code
```

### TODO/Known Issues

- **ValidateSchemaCommand (line 100-101):** "TODO: This should be more explicit" — error reporting in subscription
- **ValidateSchemaCommand (line 103):** "TODO: Also output as result" — missing result output for validation errors
