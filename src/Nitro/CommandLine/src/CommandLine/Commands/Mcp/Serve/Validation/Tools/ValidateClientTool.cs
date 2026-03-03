using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;
using ModelContextProtocol.Server;
using StrawberryShake;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Tools;

[McpServerToolType]
internal sealed class ValidateClientTool
{
    private static readonly StringComparison PathComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    [McpServerTool(Name = "validate_client")]
    [Description(
        "Validates persisted GraphQL operation files against a deployed stage's "
            + "published schema. Returns whether all operations are valid and any "
            + "per-operation validation errors with source locations.")]
    public static async Task<string> ExecuteAsync(
        NitroMcpContext ctx,
        IApiClient apiClient,
        [Description(
            "List of absolute or workspace-relative paths to .graphql operation "
                + "files, or glob patterns (e.g. 'src/**/*.graphql').")]
            string[] operationPaths,
        [Description("Client ID to validate against. Overrides the value from " + ".nitro/settings.json.")]
            string? client = null,
        [Description("Stage name (e.g. 'production'). Overrides the value from " + ".nitro/settings.json.")]
            string? stage = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedClientId = client;
        var resolvedStage = stage ?? ctx.Stage;

        if (resolvedClientId is null)
        {
            return Error("ConfigurationError", "No client ID found. Pass 'client' argument.");
        }

        if (resolvedStage is null)
        {
            return Error(
                "ConfigurationError",
                "No stage specified. Pass 'stage' argument or set 'stage' in .nitro/settings.json.");
        }

        var resolvedFiles = ResolveFiles(operationPaths);
        if (resolvedFiles.Count == 0)
        {
            return Error("FileNotFound", $"No .graphql files found matching: {string.Join(", ", operationPaths)}");
        }

        await using var bundleStream = await BundleOperationsAsync(resolvedFiles, cancellationToken);

        var input = new ValidateClientInput
        {
            ClientId = resolvedClientId,
            Stage = resolvedStage,
            Operations = new Upload(bundleStream, "operations.graphql")
        };

        var mutationResult = await apiClient.ValidateClientVersion.ExecuteAsync(input, cancellationToken);

        if (mutationResult.Errors is { Count: > 0 } gqlErrors)
        {
            return Error("GraphQLError", string.Join("; ", gqlErrors.Select(e => e.Message)));
        }

        var mutationData = mutationResult.Data?.ValidateClient;
        if (mutationData is null)
        {
            return Error("InternalError", "No data returned from mutation.");
        }

        if (mutationData.Errors is { Count: > 0 } mutErrors)
        {
            var firstError = mutErrors[0];
            return firstError switch
            {
                IClientNotFoundError => Error("ClientNotFound", $"Client '{resolvedClientId}' was not found."),
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

        return await WaitForClientResultAsync(apiClient, mutationData.Id, cancellationToken);
    }

    private static async Task<string> WaitForClientResultAsync(
        IApiClient apiClient,
        string requestId,
        CancellationToken cancellationToken)
    {
        using var stopSignal = new Subject<Unit>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var subscription = apiClient
            .OnClientVersionValidationUpdated.Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        try
        {
            await foreach (var result in subscription.ToAsyncEnumerable().WithCancellation(linked.Token))
            {
                if (result.Errors is { Count: > 0 } gqlErrors)
                {
                    return Error("GraphQLError", string.Join("; ", gqlErrors.Select(e => e.Message)));
                }

                switch (result.Data?.OnClientVersionValidationUpdate)
                {
                    case IClientVersionValidationSuccess:
                        stopSignal.OnNext(Unit.Default);
                        var successResult = new ClientValidationResult(Valid: true, Errors: []);
                        return JsonSerializer.Serialize(successResult, ValidationJsonContext.Default.ClientValidationResult);

                    case IClientVersionValidationFailed failed:
                        stopSignal.OnNext(Unit.Default);
                        var errors = MapClientErrors(failed.Errors);
                        var failResult = new ClientValidationResult(Valid: false, Errors: errors);
                        return JsonSerializer.Serialize(failResult, ValidationJsonContext.Default.ClientValidationResult);

                    case IOperationInProgress:
                    case IValidationInProgress:
                        continue;
                }
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return JsonSerializer.Serialize(
                new ClientValidationResult(
                    Valid: false,
                    Errors: [new ClientValidationError("Timeout", "Client validation timed out after 120 seconds.")]),
                ValidationJsonContext.Default.ClientValidationResult);
        }

        return JsonSerializer.Serialize(
            new ClientValidationResult(
                Valid: false,
                Errors: [new ClientValidationError("InternalError", "Subscription ended without a result.")]),
            ValidationJsonContext.Default.ClientValidationResult);
    }

    private static IReadOnlyList<ClientValidationError> MapClientErrors(
        IReadOnlyList<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors> apiErrors)
    {
        var result = new List<ClientValidationError>();

        foreach (var e in apiErrors)
        {
            switch (e)
            {
                case IPersistedQueryValidationError pq:
                    foreach (var query in pq.Queries)
                    {
                        foreach (var qErr in query.Errors)
                        {
                            result.Add(
                                new ClientValidationError(
                                    "OperationValidationError",
                                    qErr.Message,
                                    Hash: query.Hash,
                                    Locations: qErr.Locations?.Select(l => new ErrorLocation(l.Line, l.Column))
                                        .ToList()));
                        }
                    }
                    break;

                case IProcessingTimeoutError:
                    result.Add(new ClientValidationError("Timeout", "Processing timed out on the server."));
                    break;

                default:
                    result.Add(new ClientValidationError("InternalError", "An unexpected error occurred."));
                    break;
            }
        }

        return result;
    }

    internal static List<string> ResolveFiles(string[] patterns)
    {
        var files = new List<string>();
        var workspaceRoot = Path.GetFullPath(Directory.GetCurrentDirectory());

        foreach (var pattern in patterns)
        {
            if (File.Exists(pattern))
            {
                var fullPath = Path.GetFullPath(pattern);
                if (fullPath.StartsWith(workspaceRoot, PathComparison))
                {
                    files.Add(fullPath);
                }

                continue;
            }

            var dirPart = Path.GetDirectoryName(pattern) ?? ".";
            var filePart = Path.GetFileName(pattern);

            if (dirPart.Contains("**"))
            {
                var basePart = dirPart[..dirPart.IndexOf("**", StringComparison.Ordinal)].TrimEnd('/', '\\');
                var baseDir = Path.IsPathRooted(basePart) ? basePart : Path.Combine(workspaceRoot, basePart);

                baseDir = Path.GetFullPath(baseDir);
                if (!baseDir.StartsWith(workspaceRoot, PathComparison))
                {
                    continue;
                }

                if (Directory.Exists(baseDir))
                {
                    files.AddRange(Directory.GetFiles(baseDir, filePart, SearchOption.AllDirectories));
                }
            }
            else
            {
                var searchDir = Path.IsPathRooted(dirPart) ? dirPart : Path.Combine(workspaceRoot, dirPart);

                searchDir = Path.GetFullPath(searchDir);
                if (!searchDir.StartsWith(workspaceRoot, PathComparison))
                {
                    continue;
                }

                if (Directory.Exists(searchDir))
                {
                    files.AddRange(Directory.GetFiles(searchDir, filePart, SearchOption.TopDirectoryOnly));
                }
            }
        }

        return files.Distinct().Where(f => f.EndsWith(".graphql", PathComparison)).ToList();
    }

    private static async Task<MemoryStream> BundleOperationsAsync(
        List<string> files,
        CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        await using (var writer = new StreamWriter(ms, leaveOpen: true))
        {
            var first = true;

            foreach (var file in files)
            {
                if (!first)
                {
                    await writer.WriteLineAsync();
                }

                first = false;
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                await writer.WriteAsync(content);
            }

            await writer.FlushAsync(cancellationToken);
        }

        ms.Position = 0;
        return ms;
    }

    private static string Error(string type, string message)
    {
        var result = new ClientValidationResult(Valid: false, Errors: [new ClientValidationError(type, message)]);
        return JsonSerializer.Serialize(result, ValidationJsonContext.Default.ClientValidationResult);
    }
}
