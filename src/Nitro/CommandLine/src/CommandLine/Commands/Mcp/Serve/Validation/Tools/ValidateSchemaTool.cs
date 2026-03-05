using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Services;
using ModelContextProtocol.Server;
using StrawberryShake;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Tools;

[McpServerToolType]
internal sealed class ValidateSchemaTool
{
    private static readonly StringComparison PathComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? PathComparison
            : StringComparison.Ordinal;

    [McpServerTool(Name = "validate_schema")]
    [Description(
        "Validates a GraphQL SDL schema file or inline SDL string against a deployed "
            + "stage. Returns whether the schema is valid, a list of BREAKING/DANGEROUS/SAFE "
            + "changes versus the deployed schema, and any syntax or rule errors.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext ctx,
        IApiClient apiClient,
        [Description("Absolute or workspace-relative path to a .graphql SDL file. " + "Mutually exclusive with 'sdl'.")]
            string? schemaPath = null,
        [Description(
            "Inline GraphQL SDL string. Use when the schema is not yet "
                + "written to disk. Mutually exclusive with 'schemaPath'.")]
            string? sdl = null,
        [Description("API ID to validate against. Overrides the value from " + ".nitro/settings.json.")]
            string? api = null,
        [Description(
            "Stage name to validate against (e.g. 'production', 'staging'). "
                + "Overrides the value from .nitro/settings.json.")]
            string? stage = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedApiId = api ?? ctx.ApiId;
        var resolvedStage = stage ?? ctx.Stage;

        if (resolvedApiId is null)
        {
            return Error(
                "ConfigurationError",
                "No API ID found. Pass 'api' argument or set 'apiId' in .nitro/settings.json.");
        }

        if (resolvedStage is null)
        {
            return Error(
                "ConfigurationError",
                "No stage specified. Pass 'stage' argument or set 'stage' in .nitro/settings.json.");
        }

        Stream schemaStream;
        if (schemaPath is not null)
        {
            var fullPath = Path.GetFullPath(schemaPath);
            var workspaceRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
            if (!fullPath.StartsWith(workspaceRoot, PathComparison))
            {
                return Error("SecurityError", "Schema path must be within the workspace directory.");
            }

            var file = new FileInfo(fullPath);
            if (!file.Exists)
            {
                return Error("FileNotFound", $"Schema file not found: {schemaPath}");
            }

            schemaStream = file.OpenRead();
        }
        else if (sdl is not null)
        {
            schemaStream = new MemoryStream(Encoding.UTF8.GetBytes(sdl));
        }
        else
        {
            return Error("ConfigurationError", "Either 'schemaPath' or 'sdl' must be provided.");
        }

        try
        {
            var input = new ValidateSchemaInput
            {
                ApiId = resolvedApiId,
                Stage = resolvedStage,
                Schema = new Upload(schemaStream, "schema.graphql")
            };

            var mutationResult = await apiClient.ValidateSchemaVersion.ExecuteAsync(input, cancellationToken);

            if (mutationResult.Errors is { Count: > 0 } gqlErrors)
            {
                return Error("GraphQLError", string.Join("; ", gqlErrors.Select(e => e.Message)));
            }

            var mutationData = mutationResult.Data?.ValidateSchema;
            if (mutationData is null)
            {
                return Error("InternalError", "No data returned from mutation.");
            }

            if (mutationData.Errors is { Count: > 0 } mutErrors)
            {
                var firstError = mutErrors[0];
                return firstError switch
                {
                    IApiNotFoundError => Error("ApiNotFound", $"API '{resolvedApiId}' was not found."),
                    IStageNotFoundError => Error("StageNotFound", $"Stage '{resolvedStage}' was not found."),
                    IUnauthorizedOperation => Error("Unauthorized", "Unauthorized. Check your login session."),
                    IError e => Error("ValidationError", e.Message),
                    _ => Error("ValidationError", "Unknown mutation error.")
                };
            }

            if (mutationData.Id is null)
            {
                return Error("InternalError", "No request ID returned from validation mutation.");
            }

            return await WaitForSchemaResultAsync(apiClient, mutationData.Id, cancellationToken);
        }
        finally
        {
            await schemaStream.DisposeAsync();
        }
    }

    private static async Task<string> WaitForSchemaResultAsync(
        IApiClient apiClient,
        string requestId,
        CancellationToken cancellationToken)
    {
        using var stopSignal = new Subject<Unit>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var subscription = apiClient
            .OnSchemaVersionValidationUpdated.Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        try
        {
            await foreach (var result in subscription.ToAsyncEnumerable().WithCancellation(linked.Token))
            {
                if (result.Errors is { Count: > 0 } gqlErrors)
                {
                    return Error("GraphQLError", string.Join("; ", gqlErrors.Select(e => e.Message)));
                }

                switch (result.Data?.OnSchemaVersionValidationUpdate)
                {
                    case ISchemaVersionValidationSuccess success:
                        stopSignal.OnNext(Unit.Default);
                        var changes = SchemaChangeMapper.MapAll(success.Changes);
                        var successResult = new SchemaValidationResult(Valid: true, Changes: changes, Errors: []);
                        return JsonSerializer.Serialize(successResult, ValidationJsonContext.Default.SchemaValidationResult);

                    case ISchemaVersionValidationFailed failed:
                        stopSignal.OnNext(Unit.Default);
                        var errors = MapSchemaErrors(failed.Errors);
                        var failResult = new SchemaValidationResult(Valid: false, Changes: [], Errors: errors);
                        return JsonSerializer.Serialize(failResult, ValidationJsonContext.Default.SchemaValidationResult);

                    case IOperationInProgress:
                    case IValidationInProgress:
                        continue;
                }
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return JsonSerializer.Serialize(
                new SchemaValidationResult(
                    Valid: false,
                    Changes: [],
                    Errors: [new ValidationError("Timeout", "Validation timed out after 120 seconds.")]),
                ValidationJsonContext.Default.SchemaValidationResult);
        }

        return JsonSerializer.Serialize(
            new SchemaValidationResult(
                Valid: false,
                Changes: [],
                Errors: [new ValidationError("InternalError", "Subscription ended without a result.")]),
            ValidationJsonContext.Default.SchemaValidationResult);
    }

    private static IReadOnlyList<ValidationError> MapSchemaErrors(
        IReadOnlyList<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors> apiErrors)
    {
        var result = new List<ValidationError>();

        foreach (var e in apiErrors)
        {
            switch (e)
            {
                case ISchemaVersionSyntaxError s:
                    result.Add(
                        new ValidationError(
                            "SyntaxError",
                            s.Message,
                            Line: s.Line,
                            Column: s.Column,
                            Position: s.Position));
                    break;

                case IInvalidGraphQLSchemaError inv:
                    var details = inv.Errors.Select(x => $"{x.Code}: {x.Message}").ToList();
                    result.Add(new ValidationError("InvalidSchema", inv.Message, Details: details));
                    break;

                case ISchemaVersionChangeViolationError cv:
                    var changes = SchemaChangeMapper.MapAll(cv.Changes);
                    result.Add(
                        new ValidationError(
                            "BreakingChanges",
                            "Stage policy violation: breaking changes detected.",
                            Details: changes.Select(c => $"[{c.Severity}] {c.Description}").ToList()));
                    break;

                case IOperationsAreNotAllowedError ops:
                    result.Add(new ValidationError("OperationsNotAllowed", ops.Message));
                    break;

                case IPersistedQueryValidationError pq:
                    result.Add(new ValidationError("PersistedQueryError", pq.Message));
                    break;

                case IProcessingTimeoutError:
                    result.Add(new ValidationError("Timeout", "Processing timed out on the server."));
                    break;

                default:
                    result.Add(new ValidationError("InternalError", "An unexpected error occurred."));
                    break;
            }
        }

        return result;
    }

    private static string Error(string type, string message)
    {
        var result = new SchemaValidationResult(
            Valid: false,
            Changes: [],
            Errors: [new ValidationError(type, message)]);
        return JsonSerializer.Serialize(result, ValidationJsonContext.Default.SchemaValidationResult);
    }
}
