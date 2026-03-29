# PrintMutationErrors Pattern Analysis

## Overview

The `PrintMutationErrors` and `PrintMutationErrorsAndExit` are helper methods in the Nitro CLI that handle rendering mutation response errors to the console. They use a dispatch pattern to route different error types to specialized rendering methods.

## Implementation Details

### PrintMutationErrorsAndExit (plural with exit)

**Location:** `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Helpers/ConsoleHelpers.cs:8-23`

```csharp
public static void PrintMutationErrorsAndExit<T>(this INitroConsole console, IReadOnlyList<T>? errors)
    where T : class
{
    if (errors?.Count > 0)
    {
        // TODO: This needs to write to stderr
        console.WriteLine();

        foreach (var error in errors)
        {
            console.PrintMutationError(error);
        }

        throw new ExitException();
    }
}
```

**Key characteristics:**
- Generic method accepting any error type list
- Outputs blank line, then iterates through errors
- Throws `ExitException` when errors exist
- **TODO:** Should write to stderr instead of stdout
- **Status:** Marked as legacy in COMMAND_IMPLEMENTATION_GUIDELINES.md — should not be used in new commands

### PrintMutationErrors (plural without exit)

**Location:** `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Helpers/ConsoleHelpers.cs:25-38`

```csharp
public static void PrintMutationErrors<T>(this INitroConsole console, IReadOnlyList<T>? errors)
    where T : class
{
    if (errors?.Count > 0)
    {
        // TODO: Write to stderr
        console.WriteLine();

        foreach (var error in errors)
        {
            console.PrintMutationError(error);
        }
    }
}
```

**Key characteristics:**
- Generic wrapper that iterates and calls singular `PrintMutationError` dispatch
- Does not exit on error
- Used in streaming/subscription scenarios (publish with validation phases)
- **TODO:** Should write to stderr instead of stdout

### PrintMutationError (singular dispatch method)

**Location:** `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Helpers/ConsoleHelpers.cs:260-365`

This is a dispatcher that pattern-matches on error type and routes to specific rendering implementations. It handles 20+ error types:

```csharp
private static void PrintMutationError(this INitroConsole console, object error)
{
    switch (error)
    {
        case IOperationsAreNotAllowedError err:
            console.WriteLine(err.Message);
            break;
        case IConcurrentOperationError err:
            console.WriteLine(err.Message);
            break;
        case IUnexpectedProcessingError err:
            console.WriteLine(err.Message);
            break;
        case IProcessingTimeoutError err:
            console.WriteLine(err.Message);
            break;
        case ISchemaVersionChangeViolationError err:
            console.PrintMutationError(err);  // Delegates to typed overload
            break;
        case ISchemaVersionSyntaxError err:
            console.WriteLine(err.Message);
            break;
        case IPersistedQueryValidationError err:
            console.PrintMutationError(err);  // Delegates to typed overload
            break;
        case IStagesHavePublishedDependenciesError err:
            console.PrintMutationError(err);  // Delegates to typed overload
            break;
        case IApiNotFoundError err:
            console.WriteLine(err.Message);
            break;
        case IMockSchemaNonUniqueNameError err:
            console.WriteLine(err.Message);
            break;
        case IMockSchemaNotFoundError err:
            console.WriteLine(err.Message);
            break;
        case IStageNotFoundError err:
            console.WriteLine(err.Message);
            break;
        case ISubgraphInvalidError err:
            console.WriteLine(err.Message);
            break;
        case IInvalidGraphQLSchemaError err:
            console.PrintMutationError(err);  // Delegates to typed overload
            break;
        case ISchemaChangeViolationError err:
            console.PrintMutationError(err);  // Delegates to typed overload
            break;
        case IInvalidFusionSourceSchemaArchiveError err:
            // Special message + err.Message
            break;
        case IOpenApiCollectionValidationError err:
            console.PrintMutationError(err);  // Delegates to typed overload
            break;
        case IInvalidOpenApiCollectionArchiveError err:
            console.PrintInvalidOpenApiCollectionArchiveError(err.Message);
            break;
        case IOpenApiCollectionValidationArchiveError err:
            console.PrintInvalidOpenApiCollectionArchiveError(err.Message);
            break;
        case IMcpFeatureCollectionValidationError err:
            console.PrintMutationError(err);  // Delegates to typed overload
            break;
        case IInvalidMcpFeatureCollectionArchiveError err:
            console.PrintInvalidMcpFeatureCollectionArchiveError(err.Message);
            break;
        case IMcpFeatureCollectionValidationArchiveError err:
            console.PrintInvalidMcpFeatureCollectionArchiveError(err.Message);
            break;
        case IError err:
            console.WriteLine("Unexpected mutation error: " + err.Message);
            break;
        default:
            console.WriteLine("Unexpected mutation error");
            break;
    }
}
```

## Specialized Error Rendering Overloads

### Schema Change Violations

**For ISchemaVersionChangeViolationError and ISchemaChangeViolationError:**

```csharp
private static void PrintMutationError(
    this INitroConsole console,
    ISchemaVersionChangeViolationError error)
{
    var tree = new Tree("");
    tree.AddSchemaChanges(error.Changes.OfType<ISchemaChange>());
    console.Write(tree);
}
```

Uses Spectre.Console's Tree structure to render schema changes. Calls `AddSchemaChanges` extension method (defined elsewhere).

### Persisted Query Validation

**For IPersistedQueryValidationError:**

```csharp
private static void PrintMutationError(this INitroConsole console, IPersistedQueryValidationError error)
{
    console.WarningLine(
        $"There were errors on client {error.Client?.Name.AsHighlight()} [dim](ID: {error.Client?.Id})[/]");

    console.WriteLine(error.Message);

    var node = new Tree("");
    foreach (var query in error.Queries)
    {
        var publishingInfo = query.DeployedTags.Count > 0
            ? $" [dim](Deployed tags: {string.Join(",", query.DeployedTags)})[/]"
            : "";

        var queryNode = node.AddNode(
            $"[red]{query.Message.EscapeMarkup().Replace(query.Hash, $"[bold]{query.Hash}[/]{publishingInfo}")}[/]");

        foreach (var err in query.Errors)
        {
            var errorLocation = string.Empty;
            if (err.Locations is { Count: > 0 } locations)
            {
                errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
            }

            queryNode.AddNode($"{err.Message.EscapeMarkup()} {errorLocation}");
        }
    }

    console.Write(node);
}
```

**Output structure:**
- Warning header with client name and ID
- Main error message
- Tree of queries with:
  - Query message (with hash bolded and deployed tags if applicable)
  - Sub-nodes for each error with location info (line:column)

### Stages with Published Dependencies

**For IStagesHavePublishedDependenciesError:**

```csharp
private static void PrintMutationError(
    this INitroConsole console,
    IStagesHavePublishedDependenciesError error)
{
    console.WriteLine(error.Message);
    console.WriteLine();

    foreach (var stage in error.Stages)
    {
        if (stage.PublishedSchema?.Version is { Tag: var tag })
        {
            console.WriteLine(
                $"The schema {tag.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
        }

        foreach (var publishedClient in stage.PublishedClients)
        {
            var tags = string.Join(
                ',',
                publishedClient.PublishedVersions.Select(x => x.Version?.Tag));
            console.WriteLine(
                $"The client {publishedClient.Client.Name.AsHighlight()} in version {tags.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
        }
    }
}
```

**Output structure:**
- Main error message
- For each stage:
  - Published schema version (if any)
  - Published clients with their version tags

### OpenAPI Collection Validation

**For IOpenApiCollectionValidationError:**

```csharp
private static void PrintMutationError(this INitroConsole console, IOpenApiCollectionValidationError error)
{
    foreach (var collectionError in error.Collections)
    {
        var openApiCollection = collectionError.OpenApiCollection;

        console.WarningLine(
            $"There were errors in the OpenAPI collection '{openApiCollection?.Name.AsHighlight()}' [dim](ID: {openApiCollection?.Id})[/]");

        var node = new Tree("");
        foreach (var entity in collectionError.Entities)
        {
            var entityNode = node.AddNode(GetEntityNodeHeading(entity));

            foreach (var entityError in entity.Errors)
            {
                if (entityError is IOpenApiCollectionValidationDocumentError documentError)
                {
                    var errorLocation = string.Empty;
                    if (documentError.Locations is { Count: > 0 } locations)
                    {
                        errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
                    }

                    entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                }
                else if (entityError is IOpenApiCollectionValidationEntityValidationError entityValidationError)
                {
                    entityNode.AddNode(entityValidationError.Message.EscapeMarkup());
                }
                else
                {
                    entityNode.AddNode("Unknown error type");
                }
            }
        }

        console.Write(node);
    }

    static string GetEntityNodeHeading(IOpenApiCollectionValidationEntity entity)
    {
        var heading = entity switch
        {
            IOpenApiCollectionValidationEndpoint endpoint => $"Endpoint '{endpoint.HttpMethod} {endpoint.Route}'",
            IOpenApiCollectionValidationModel model => $"Model '{model.Name}'",
            _ => "Unknown entity type"
        };

        return $"[red]{heading}[/]";
    }
}
```

**Output structure:**
- For each collection:
  - Warning header with collection name and ID
  - Tree of entities (Endpoints or Models)
  - Sub-nodes for each entity error:
    - Document errors with line:column locations
    - Validation errors with message

### MCP Feature Collection Validation

**For IMcpFeatureCollectionValidationError:**

```csharp
private static void PrintMutationError(this INitroConsole console, IMcpFeatureCollectionValidationError error)
{
    foreach (var collectionError in error.Collections)
    {
        var mcpFeatureCollection = collectionError.McpFeatureCollection;

        console.WarningLine(
            $"There were errors in the MCP Feature Collection '{mcpFeatureCollection?.Name.AsHighlight()}' [dim](ID: {mcpFeatureCollection?.Id})[/]");

        var node = new Tree("");
        foreach (var entity in collectionError.Entities)
        {
            var entityNode = node.AddNode(GetEntityNodeHeading(entity));

            foreach (var entityError in entity.Errors)
            {
                if (entityError is IMcpFeatureCollectionValidationDocumentError documentError)
                {
                    var errorLocation = string.Empty;
                    if (documentError.Locations is { Count: > 0 } locations)
                    {
                        errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
                    }

                    entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                }
                else if (entityError is IMcpFeatureCollectionValidationEntityValidationError entityValidationError)
                {
                    entityNode.AddNode(entityValidationError.Message.EscapeMarkup());
                }
                else
                {
                    entityNode.AddNode("Unknown error type");
                }
            }
        }

        console.Write(node);
    }

    static string GetEntityNodeHeading(IMcpFeatureCollectionValidationEntity entity)
    {
        var heading = entity switch
        {
            IMcpFeatureCollectionValidationPrompt prompt => $"Prompt '{prompt.Name}'",
            IMcpFeatureCollectionValidationTool tool => $"Tool '{tool.Name}'",
            _ => "Unknown entity type"
        };

        return $"[red]{heading}[/]";
    }
}
```

**Output structure:**
- For each collection:
  - Warning header with collection name and ID
  - Tree of entities (Prompts or Tools)
  - Sub-nodes for each entity error:
    - Document errors with line:column locations
    - Validation errors with message

### Invalid GraphQL Schema

**For IInvalidGraphQLSchemaError:**

```csharp
private static void PrintMutationError(
    this INitroConsole console,
    IInvalidGraphQLSchemaError error)
{
    console.WriteLine(
        "The schema you are trying to publish is invalid. Please fix the following errors:");

    console.WriteLine(error.Message);

    var node = new Tree("");
    foreach (var query in error.Errors)
    {
        node.AddNode($"[red]{query.Message.EscapeMarkup()}[/] [grey]{query.Code}[/]");
    }

    console.Write(node);
}
```

**Output structure:**
- Header message
- Main error message
- Tree of individual GraphQL errors with message and code

## Validation Error Interface Properties

Based on the generated client code, here are the key validation error interfaces:

### IMcpFeatureCollectionValidationError
```
- Collections: IReadOnlyList<IMcpFeatureCollectionValidationCollection>
```

### IMcpFeatureCollectionValidationCollection
```
- McpFeatureCollection: IMcpFeatureCollectionValidationCollection?
- Entities: IReadOnlyList<IMcpFeatureCollectionValidationEntity>
```

### IMcpFeatureCollectionValidationEntity
- Variants:
  - `IMcpFeatureCollectionValidationPrompt` with `Name` property
  - `IMcpFeatureCollectionValidationTool` with `Name` property

### IMcpFeatureCollectionValidationEntity Errors
- `IMcpFeatureCollectionValidationDocumentError` (with Message, Locations)
- `IMcpFeatureCollectionValidationEntityValidationError` (with Message)

### IOpenApiCollectionValidationError
```
- Collections: IReadOnlyList<IOpenApiCollectionValidationCollection>
```

### IOpenApiCollectionValidationCollection
```
- OpenApiCollection: IOpenApiCollectionValidationCollection?
- Entities: IReadOnlyList<IOpenApiCollectionValidationEntity>
```

### IOpenApiCollectionValidationEntity
- Variants:
  - `IOpenApiCollectionValidationEndpoint` with HttpMethod, Route
  - `IOpenApiCollectionValidationModel` with Name

### IOpenApiCollectionValidationEntity Errors
- `IOpenApiCollectionValidationDocumentError` (with Message, Locations)
- `IOpenApiCollectionValidationEntityValidationError` (with Message)

## Usage Patterns

### PrintMutationErrorsAndExit Usage (25 calls in source)

Used in these commands for immediate error exit:
- `CreateWorkspaceCommand`
- `CreateMockCommand`
- `CreateOpenApiCollectionCommand`
- `CreateMcpFeatureCollectionCommand`
- `DeleteMcpFeatureCollectionCommand`
- `DeleteOpenApiCollectionCommand`
- `DeleteStageCommand`
- `EditStagesCommand`
- `FusionUploadCommand`
- `FusionPublishHelpers` (multiple calls)
- `PublishOpenApiCollectionCommand`
- `PublishMcpFeatureCollectionCommand`
- `ValidateClientCommand`
- `ValidateMcpFeatureCollectionCommand`
- `ValidateOpenApiCollectionCommand`
- `PublishClientCommand`
- `UnpublishClientCommand`
- `CreatePersonalAccessTokenCommand`
- `RevokePersonalAccessTokenCommand`
- `FusionConfigurationPublishValidateCommand`
- `UpdateMockCommand`

### PrintMutationErrors Usage (15+ calls in source)

Used in streaming/subscription contexts where validation happens in phases:
- `FusionValidateCommand` — multi-phase validation
- `FusionPublishHelpers` — deployment validation
- `PublishSchemaCommand` — schema deployment validation
- `PublishOpenApiCollectionCommand` — collection deployment validation
- `PublishMcpFeatureCollectionCommand` — collection deployment validation
- `PublishClientCommand` — client version deployment validation
- `ValidateSchemaCommand` — standalone schema validation
- `FusionConfigurationPublishValidateCommand` — configuration validation

## Rendering Technology

**Spectre.Console Tree Structure:**
- Uses `Tree("")` to create a root tree node
- Calls `AddNode()` to append child nodes
- Supports Spectre markup for colors/styles:
  - `[red]...[/]` for red text
  - `[bold]...[/]` for bold text
  - `[dim]...[/]` for dimmed text
  - `[grey]...[/]` for grey text
  - `[green]...[/]` for green text

**Helper Extensions:**
- `EscapeMarkup()` — escapes markup special characters in error messages
- `AsHighlight()` — applies Spectre highlighting style (appears to apply a color)
- `WarningLine()` — renders line with warning glyph/style
- `AsQuestion()` — formats text as a question

## Key Observations

1. **TODO Items:** Both plural methods need stderr redirection instead of stdout
2. **Pattern Similarity:** OpenAPI and MCP error rendering follow identical patterns with entity-specific heading logic
3. **Spectre.Console Usage:** Rich console output using trees and markup for visual hierarchy
4. **Dispatch Pattern:** Generic object dispatch in PrintMutationError(object) with type-specific overloads
5. **Legacy Status:** PrintMutationErrorsAndExit is marked as legacy and should not be used in new commands
6. **Location Information:** Validation errors include source locations (line:column) for document errors
7. **Deployment Context:** Many errors include deployment/published version information for context

## Error Handling Guidelines

Per `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Commands/COMMAND_IMPLEMENTATION_GUIDELINES.md`:

- Do not use `PrintMutationErrorsAndExit` in new or updated commands
- Use explicit inline switch pattern for error handling
- Call `activity.Fail()` on activity before exiting
- Do not rely on generic `ExitException` without message
