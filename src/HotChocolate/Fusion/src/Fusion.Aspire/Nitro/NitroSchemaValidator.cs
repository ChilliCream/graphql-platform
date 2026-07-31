using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed class NitroSchemaValidator(
    GraphQLHttpClient client,
    ILogger<NitroSchemaValidator> logger)
    : INitroSchemaValidator
{
    private const int MaxParsedFindings = 10_000;
    private static readonly TimeSpan s_overallTimeout = TimeSpan.FromMinutes(2);

    public async Task<NitroSchemaValidationReport> ValidateAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        byte[] schema,
        string schemaHash,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaHash);

        if (connection.Credential.Kind is NitroCredentialKind.None)
        {
            return Unavailable(connection.Credential.UnavailableMessage!, schemaHash);
        }

        using var overall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        overall.CancelAfter(s_overallTimeout);

        string? requestId = null;

        try
        {
            var start = await SendStartAsync(
                connection,
                apiId,
                stage,
                schema,
                overall.Token);

            if (start.Failure is not null)
            {
                return Unavailable(start.Failure, schemaHash);
            }

            requestId = start.RequestId;
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return Unavailable(
                    "Nitro returned no validation request id.",
                    schemaHash);
            }

            var watch = await NitroGraphQL.SubscribeAsync(
                client,
                CreateWatchRequest(connection, requestId),
                result => ReadValidationResult(result, schemaHash, requestId),
                overall.Token);

            return watch.Value ?? Unavailable(watch.Failure!, schemaHash, requestId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Unavailable(
                "Nitro schema validation timed out.",
                schemaHash,
                requestId);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Nitro schema validation failed.");
            return Unavailable(
                $"Nitro schema validation was unavailable: {exception.Message}",
                schemaHash,
                requestId);
        }
    }

    private async Task<(string? RequestId, string? Failure)> SendStartAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        byte[] schema,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["apiId"] = apiId,
                ["stage"] = stage,
                ["schema"] = new FileReference(
                    () => new MemoryStream(schema, writable: false),
                    "schema.graphql",
                    "application/graphql")
            }
        };

        var result = await NitroGraphQL.SendWithRetryAsync(
            client,
            CreateRequest(
                connection,
                NitroOperationDocuments.ValidateSchemaOperationName,
#if NITRO_PERSISTED_OPERATIONS
                NitroOperationDocuments.GetValidateSchemaOperationId(),
#else
                NitroOperationDocuments.GetValidateSchemaDocument(),
#endif
                variables,
                enableFileUploads: true),
            retryTransientFailures: false,
            NitroGraphQL.DefaultRequestTimeout,
            cancellationToken);

        if (result.Failure is not null)
        {
            return (null, result.Failure);
        }

        if (result.Data.ValueKind is not JsonValueKind.Object
            || !result.Data.TryGetProperty("validateSchema", out var payload)
            || payload.ValueKind is not JsonValueKind.Object)
        {
            return (null, "Nitro returned a malformed validation response.");
        }

        var error = ReadFirstError(payload);
        return error is null
            ? (GetString(payload, "id"), null)
            : (null, error);
    }

    private static GraphQLHttpRequest CreateWatchRequest(
        NitroConnection connection,
        string requestId)
        => CreateRequest(
            connection,
            NitroOperationDocuments.WatchSchemaValidationOperationName,
#if NITRO_PERSISTED_OPERATIONS
            NitroOperationDocuments.GetWatchSchemaValidationOperationId(),
#else
            NitroOperationDocuments.GetWatchSchemaValidationDocument(),
#endif
            new Dictionary<string, object?> { ["requestId"] = requestId },
            enableFileUploads: false);

    private static NitroSchemaValidationReport? ReadValidationResult(
        OperationResult result,
        string schemaHash,
        string requestId)
    {
        if (result.Data.ValueKind is not JsonValueKind.Object
            || !result.Data.TryGetProperty("onSchemaVersionValidationUpdate", out var payload)
            || payload.ValueKind is not JsonValueKind.Object)
        {
            return Unavailable(
                "Nitro returned no schema validation result.",
                schemaHash,
                requestId);
        }

        var typeName = GetString(payload, "__typename");
        switch (typeName)
        {
            case "OperationInProgress":
            case "ValidationInProgress":
                return null;

            case "SchemaVersionValidationSuccess":
                return NitroSchemaValidationReport.Passed(
                    schemaHash,
                    requestId,
                    DateTimeOffset.UtcNow);

            case "SchemaVersionValidationFailed":
                return ParseViolations(payload, schemaHash, requestId);

            default:
                return Unavailable(
                    $"Nitro returned the unknown validation result '{typeName ?? "null"}'.",
                    schemaHash,
                    requestId);
        }
    }

    private static GraphQLHttpRequest CreateRequest(
        NitroConnection connection,
        string operationName,
        string documentOrId,
        IReadOnlyDictionary<string, object?> variables,
        bool enableFileUploads)
    {
#if NITRO_PERSISTED_OPERATIONS
        var body = new OperationRequest(
            id: documentOrId,
            operationName: operationName,
            variables: variables);
#else
        var body = new OperationRequest(
            documentOrId,
            operationName: operationName,
            variables: variables);
#endif
        return new GraphQLHttpRequest(body, connection.GraphQLEndpoint)
        {
            EnableFileUploads = enableFileUploads,
            OnMessageCreated = (_, requestMessage, _) =>
                NitroRequestHeaders.Apply(requestMessage, connection.Credential)
        };
    }

    private static NitroSchemaValidationReport ParseViolations(
        JsonElement result,
        string schemaHash,
        string requestId)
    {
        var clients = new List<NitroClientContractViolation>();
        var findings = new List<NitroSchemaValidationFinding>();
        var findingCount = 0;
        var hasMoreClientErrors = false;
        string? unknownFailure = null;

        if (!result.TryGetProperty("errors", out var errors)
            || errors.ValueKind is not JsonValueKind.Array)
        {
            return NitroSchemaValidationReport.Unavailable(
                schemaHash,
                "Nitro returned a malformed validation failure.",
                DateTimeOffset.UtcNow,
                requestId);
        }

        foreach (var error in errors.EnumerateArray())
        {
            if (++findingCount > MaxParsedFindings)
            {
                return NitroSchemaValidationReport.Unavailable(
                    schemaHash,
                    "Nitro returned too many schema validation findings.",
                    DateTimeOffset.UtcNow,
                    requestId);
            }

            var typeName = GetString(error, "__typename") ?? "UnknownValidationError";
            switch (typeName)
            {
                case "PersistedQueryValidationError":
                    ParseClientViolation(
                        error,
                        clients,
                        ref findingCount,
                        ref hasMoreClientErrors);
                    break;

                case "SchemaVersionChangeViolationError":
                    ParseSchemaChanges(error, findings, ref findingCount);
                    break;

                case "InvalidGraphQLSchemaError":
                    ParseNestedErrors(error, "Invalid GraphQL schema", findings, ref findingCount);
                    break;

                case "OpenApiCollectionValidationError":
                    ParseCollectionErrors(error, "OpenAPI", "collections", findings, ref findingCount);
                    break;

                case "McpFeatureCollectionValidationError":
                    ParseCollectionErrors(error, "MCP", "collections", findings, ref findingCount);
                    break;

                case "SchemaVersionSyntaxError":
                case "OperationsAreNotAllowedError":
                case "ProcessingTimeoutError":
                case "ReadyTimeoutError":
                case "UnexpectedProcessingError":
                    var finding = CreateFinding(
                        GroupFor(typeName),
                        typeName,
                        error,
                        GetString(error, "message") ?? typeName);
                    findings.Add(
                        finding with
                        {
                            Identity = GetString(error, "operationName")
                        });
                    break;

                default:
                    unknownFailure =
                        $"Nitro returned the unknown validation error '{typeName}'.";
                    break;
            }

            if (findingCount > MaxParsedFindings)
            {
                return NitroSchemaValidationReport.Unavailable(
                    schemaHash,
                    "Nitro returned too many schema validation findings.",
                    DateTimeOffset.UtcNow,
                    requestId);
            }
        }

        clients = NormalizeClients(clients);
        if (unknownFailure is not null
            && clients.Count == 0
            && findings.Count == 0)
        {
            return NitroSchemaValidationReport.Unavailable(
                schemaHash,
                unknownFailure,
                DateTimeOffset.UtcNow,
                requestId);
        }

        if (clients.Count == 0 && findings.Count == 0)
        {
            return NitroSchemaValidationReport.Unavailable(
                schemaHash,
                "Nitro returned a validation failure without findings.",
                DateTimeOffset.UtcNow,
                requestId);
        }

        return new NitroSchemaValidationReport(
            NitroSchemaValidationStatus.Violations,
            schemaHash,
            requestId,
            clients,
            findings,
            null,
            DateTimeOffset.UtcNow)
        {
            HasMoreClientErrors = hasMoreClientErrors
        };
    }

    private static List<NitroClientContractViolation> NormalizeClients(
        List<NitroClientContractViolation> clients)
        =>
        [
            .. clients
                .GroupBy(client => client.ClientId, StringComparer.Ordinal)
                .Select(clientGroup =>
                    new NitroClientContractViolation(
                        clientGroup.Key,
                        clientGroup.Select(client => client.ClientName).FirstOrDefault(
                            name => name != "unknown") ?? "unknown",
                        [
                            .. clientGroup
                                .SelectMany(client => client.Operations)
                                .GroupBy(operation => operation.Hash, StringComparer.Ordinal)
                                .Select(operationGroup =>
                                    new NitroOperationContractViolation(
                                        operationGroup.Key,
                                        [
                                            .. operationGroup
                                                .SelectMany(operation => operation.DeployedTags)
                                                .Distinct(StringComparer.Ordinal)
                                                .Order(StringComparer.Ordinal)
                                        ],
                                        [
                                            .. operationGroup.SelectMany(operation => operation.Errors)
                                        ]))
                        ]))
        ];

    private static void ParseClientViolation(
        JsonElement error,
        List<NitroClientContractViolation> clients,
        ref int findingCount,
        ref bool hasMoreClientErrors)
    {
        hasMoreClientErrors |= GetBoolean(error, "hasMoreErrors") is true;
        var clientId = GetString(error, "clientId") ?? "unknown";
        var clientName = "unknown";

        if (error.TryGetProperty("client", out var client)
            && client.ValueKind is JsonValueKind.Object)
        {
            clientId = GetString(client, "id") ?? clientId;
            clientName = GetString(client, "name") ?? clientName;
        }

        var operations = new List<NitroOperationContractViolation>();
        if (error.TryGetProperty("queries", out var queries)
            && queries.ValueKind is JsonValueKind.Array)
        {
            foreach (var query in queries.EnumerateArray())
            {
                var operationErrors = new List<NitroSchemaValidationFinding>();
                if (query.TryGetProperty("errors", out var queryErrors)
                    && queryErrors.ValueKind is JsonValueKind.Array)
                {
                    foreach (var queryError in queryErrors.EnumerateArray())
                    {
                        operationErrors.Add(CreateFinding(
                            "Client contract violations",
                            "PersistedQueryValidationError",
                            queryError,
                            GetString(queryError, "message") ?? "Client operation is invalid."));
                        findingCount++;
                    }
                }

                if (operationErrors.Count == 0)
                {
                    operationErrors.Add(
                        new NitroSchemaValidationFinding(
                            "Client contract violations",
                            "PersistedQueryValidationError",
                            GetString(query, "message")
                                ?? "The client operation is invalid."));
                    findingCount++;
                }

                operations.Add(
                    new NitroOperationContractViolation(
                        GetString(query, "hash") ?? "unknown",
                        ReadStrings(query, "deployedTags"),
                        operationErrors));
            }
        }

        clients.Add(new NitroClientContractViolation(clientId, clientName, operations));
    }

    private static void ParseSchemaChanges(
        JsonElement error,
        List<NitroSchemaValidationFinding> findings,
        ref int findingCount)
    {
        if (!error.TryGetProperty("changes", out var changes)
            || changes.ValueKind is not JsonValueKind.Array)
        {
            findings.Add(
                CreateFinding(
                    "Schema change violations",
                    "SchemaVersionChangeViolationError",
                    error,
                    GetString(error, "message") ?? "The schema has breaking changes."));
            return;
        }

        foreach (var change in changes.EnumerateArray())
        {
            var typeName = GetString(change, "__typename") ?? "SchemaChange";
            var severity = GetString(change, "severity");
            findings.Add(
                new NitroSchemaValidationFinding(
                    "Schema change violations",
                    typeName,
                    severity is null ? typeName : $"{typeName} ({severity})",
                    Coordinate: GetString(change, "coordinate"),
                    Identity: severity));
            findingCount++;
        }
    }

    private static void ParseNestedErrors(
        JsonElement error,
        string group,
        List<NitroSchemaValidationFinding> findings,
        ref int findingCount)
    {
        if (error.TryGetProperty("errors", out var nested)
            && nested.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in nested.EnumerateArray())
            {
                findings.Add(
                    CreateFinding(
                        group,
                        GetString(error, "__typename") ?? group,
                        item,
                        GetString(item, "message") ?? group));
                findingCount++;
            }
        }
        else
        {
            findings.Add(
                CreateFinding(
                    group,
                    GetString(error, "__typename") ?? group,
                    error,
                    GetString(error, "message") ?? group));
        }
    }

    private static void ParseCollectionErrors(
        JsonElement error,
        string group,
        string collectionsProperty,
        List<NitroSchemaValidationFinding> findings,
        ref int findingCount)
    {
        if (!error.TryGetProperty(collectionsProperty, out var collections)
            || collections.ValueKind is not JsonValueKind.Array)
        {
            findings.Add(
                CreateFinding(
                    group,
                    GetString(error, "__typename") ?? group,
                    error,
                    GetString(error, "message") ?? group));
            return;
        }

        foreach (var collection in collections.EnumerateArray())
        {
            var collectionProperty = group == "OpenAPI"
                ? "openApiCollection"
                : "mcpFeatureCollection";
            var collectionName = "unknown collection";
            var collectionIdentity = collectionName;
            if (collection.TryGetProperty(collectionProperty, out var collectionInfo)
                && collectionInfo.ValueKind is JsonValueKind.Object)
            {
                collectionName = GetString(collectionInfo, "name")
                    ?? GetString(collectionInfo, "id")
                    ?? collectionName;
                collectionIdentity = GetString(collectionInfo, "id")
                    ?? GetString(collectionInfo, "name")
                    ?? collectionIdentity;
            }

            if (!collection.TryGetProperty("entities", out var entities)
                || entities.ValueKind is not JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entity in entities.EnumerateArray())
            {
                var entityName = GetString(entity, "name")
                    ?? (GetString(entity, "httpMethod") is { } method
                        ? $"{method} {GetString(entity, "route")}"
                        : GetString(entity, "__typename"))
                    ?? "unknown entity";

                if (!entity.TryGetProperty("errors", out var entityErrors)
                    || entityErrors.ValueKind is not JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var entityError in entityErrors.EnumerateArray())
                {
                    var finding = CreateFinding(
                        group,
                        GetString(entityError, "__typename") ?? group,
                        entityError,
                        GetString(entityError, "message") ?? group);
                    findings.Add(
                        finding with
                        {
                            Message = $"{collectionName}, {entityName}: {finding.Message}",
                            Identity = $"{collectionIdentity}|{entityName}"
                        });
                    findingCount++;
                }
            }
        }
    }

    private static NitroSchemaValidationFinding CreateFinding(
        string group,
        string kind,
        JsonElement value,
        string message)
    {
        int? line = null;
        int? column = null;

        if (value.TryGetProperty("locations", out var locations)
            && locations.ValueKind is JsonValueKind.Array
            && locations.GetArrayLength() > 0)
        {
            var location = locations[0];
            line = GetInt32(location, "line");
            column = GetInt32(location, "column");
        }

        return new NitroSchemaValidationFinding(
            group,
            kind,
            message,
            GetString(value, "code"),
            GetString(value, "coordinate"),
            GetString(value, "path"),
            line ?? GetInt32(value, "line"),
            column ?? GetInt32(value, "column"));
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement value, string propertyName)
    {
        if (!value.TryGetProperty(propertyName, out var items)
            || items.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. items.EnumerateArray()
                .Where(item => item.ValueKind is JsonValueKind.String)
                .Select(item => item.GetString()!)
        ];
    }

    private static string? ReadFirstError(JsonElement payload)
    {
        if (!payload.TryGetProperty("errors", out var errors)
            || errors.ValueKind is not JsonValueKind.Array
            || errors.GetArrayLength() == 0)
        {
            return null;
        }

        var error = errors[0];
        var typeName = GetString(error, "__typename") ?? "NitroError";
        var message = GetString(error, "message") ?? "Nitro rejected the schema validation request.";
        return $"{typeName}: {message}";
    }

    private static string GroupFor(string typeName)
        => typeName switch
        {
            "SchemaVersionSyntaxError" => "Schema syntax errors",
            "OperationsAreNotAllowedError" => "Operation policy errors",
            "ProcessingTimeoutError"
                or "ReadyTimeoutError"
                or "UnexpectedProcessingError" => "Processing errors",
            _ => "Other validation errors"
        };

    private static string? GetString(JsonElement value, string propertyName)
        => value.ValueKind is JsonValueKind.Object
            && value.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.String
                ? property.GetString()
                : null;

    private static int? GetInt32(JsonElement value, string propertyName)
        => value.ValueKind is JsonValueKind.Object
            && value.TryGetProperty(propertyName, out var property)
            && property.TryGetInt32(out var result)
                ? result
                : null;

    private static bool? GetBoolean(JsonElement value, string propertyName)
        => value.ValueKind is JsonValueKind.Object
            && value.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? property.GetBoolean()
                : null;

    private static NitroSchemaValidationReport Unavailable(
        string reason,
        string schemaHash,
        string? requestId = null)
        => NitroSchemaValidationReport.Unavailable(
            schemaHash,
            reason,
            DateTimeOffset.UtcNow,
            requestId);
}
