using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;

internal sealed class ManagementService(NitroApiService apiService)
{
    public async Task<CreateApiResult> CreateApiAsync(
        string workspaceId,
        string name,
        string path,
        string? kind,
        CancellationToken cancellationToken)
    {
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var request = ManagementQueries.BuildCreateApi(workspaceId, name, pathSegments, kind);
        using var result = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var data = result.Data;

        // Check for top-level errors
        if (TryGetErrors(data, "pushWorkspaceChanges", out var errorMessage))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("create_api", errorMessage);
        }

        // Navigate: pushWorkspaceChanges.changes[0]
        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("pushWorkspaceChanges", out var push))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("create_api", "No response data.");
        }

        if (push.TryGetProperty("changes", out var changes)
            && changes.ValueKind == JsonValueKind.Array)
        {
            foreach (var change in changes.EnumerateArray())
            {
                // Check per-change error
                if (change.TryGetProperty("error", out var changeError)
                    && changeError.ValueKind != JsonValueKind.Null)
                {
                    var msg = changeError.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
                    throw ManagementThrowHelper.GraphQLOperationFailed("create_api", msg ?? "Unknown error");
                }

                // Extract result
                if (change.TryGetProperty("result", out var apiResult)
                    && apiResult.ValueKind == JsonValueKind.Object)
                {
                    return new CreateApiResult
                    {
                        Success = true,
                        Api = new ApiEntry
                        {
                            Id = GetStringOrEmpty(apiResult, "id"),
                            Name = GetStringOrEmpty(apiResult, "name"),
                            Path = GetStringOrEmpty(apiResult, "path"),
                            Kind = GetStringOrNull(apiResult, "kind")
                        }
                    };
                }
            }
        }

        throw ManagementThrowHelper.GraphQLOperationFailed("create_api", "Unexpected response structure.");
    }

    public async Task<ListApisResult> ListApisAsync(
        string workspaceId,
        int first,
        string? after,
        CancellationToken cancellationToken)
    {
        var request = ManagementQueries.BuildListApis(workspaceId, first, after);
        using var result = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var data = result.Data;
        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("workspaceById", out var workspace)
            || !workspace.TryGetProperty("apis", out var apis))
        {
            return new ListApisResult
            {
                Apis = [],
                PageInfo = new PageInfoEntry()
            };
        }

        var entries = ParseEdges(apis, ParseApiEntry);
        var pageInfo = ParsePageInfo(apis);

        return new ListApisResult { Apis = entries, PageInfo = pageInfo };
    }

    public async Task<UpdateApiSettingsResult> UpdateApiSettingsAsync(
        string apiId,
        bool? treatDangerousAsBreaking,
        bool? allowBreakingSchemaChanges,
        CancellationToken cancellationToken)
    {
        var request = ManagementQueries.BuildUpdateApiSettings(apiId, treatDangerousAsBreaking, allowBreakingSchemaChanges);
        using var result = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var data = result.Data;

        if (TryGetErrors(data, "updateApiSettings", out var errorMessage))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("update_api_settings", errorMessage);
        }

        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("updateApiSettings", out var update))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("update_api_settings", "No response data.");
        }

        string? apiName = null;
        if (update.TryGetProperty("api", out var api) && api.ValueKind == JsonValueKind.Object)
        {
            apiName = GetStringOrNull(api, "name");
        }

        return new UpdateApiSettingsResult { Success = true, ApiName = apiName };
    }

    public async Task<CreateApiKeyResult> CreateApiKeyAsync(
        string workspaceId,
        string name,
        string? apiId,
        string? stageName,
        CancellationToken cancellationToken)
    {
        var request = ManagementQueries.BuildCreateApiKey(workspaceId, name, apiId, stageName);
        using var result = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var data = result.Data;

        if (TryGetErrors(data, "createApiKey", out var errorMessage))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("create_api_key", errorMessage);
        }

        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("createApiKey", out var createApiKey))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("create_api_key", "No response data.");
        }

        if (createApiKey.TryGetProperty("result", out var keyResult)
            && keyResult.ValueKind == JsonValueKind.Object)
        {
            ApiKeyEntry? keyEntry = null;
            if (keyResult.TryGetProperty("key", out var key) && key.ValueKind == JsonValueKind.Object)
            {
                keyEntry = new ApiKeyEntry
                {
                    Id = GetStringOrEmpty(key, "id"),
                    Name = GetStringOrEmpty(key, "name")
                };
            }

            var secret = GetStringOrNull(keyResult, "secret");

            return new CreateApiKeyResult
            {
                Success = true,
                ApiKey = keyEntry,
                Secret = secret
            };
        }

        throw ManagementThrowHelper.GraphQLOperationFailed("create_api_key", "Unexpected response structure.");
    }

    public async Task<ListApiKeysResult> ListApiKeysAsync(
        string workspaceId,
        int first,
        string? after,
        CancellationToken cancellationToken)
    {
        var request = ManagementQueries.BuildListApiKeys(workspaceId, first, after);
        using var result = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var data = result.Data;
        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("workspaceById", out var workspace)
            || !workspace.TryGetProperty("apiKeys", out var apiKeys))
        {
            return new ListApiKeysResult
            {
                ApiKeys = [],
                PageInfo = new PageInfoEntry()
            };
        }

        var entries = ParseEdges(apiKeys, ParseApiKeyEntry);
        var pageInfo = ParsePageInfo(apiKeys);

        return new ListApiKeysResult { ApiKeys = entries, PageInfo = pageInfo };
    }

    public async Task<CreateClientResult> CreateClientAsync(
        string apiId,
        string name,
        CancellationToken cancellationToken)
    {
        var request = ManagementQueries.BuildCreateClient(apiId, name);
        using var result = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var data = result.Data;

        if (TryGetErrors(data, "createClient", out var errorMessage))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("create_client", errorMessage);
        }

        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("createClient", out var createClient))
        {
            throw ManagementThrowHelper.GraphQLOperationFailed("create_client", "No response data.");
        }

        if (createClient.TryGetProperty("client", out var client)
            && client.ValueKind == JsonValueKind.Object)
        {
            return new CreateClientResult
            {
                Success = true,
                Client = new ClientEntry
                {
                    Id = GetStringOrEmpty(client, "id"),
                    Name = GetStringOrEmpty(client, "name")
                }
            };
        }

        throw ManagementThrowHelper.GraphQLOperationFailed("create_client", "Unexpected response structure.");
    }

    public async Task<ListClientsResult> ListClientsAsync(
        string apiId,
        int first,
        string? after,
        CancellationToken cancellationToken)
    {
        var request = ManagementQueries.BuildListClients(apiId, first, after);
        using var result = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var data = result.Data;
        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("node", out var node)
            || !node.TryGetProperty("clients", out var clients))
        {
            return new ListClientsResult
            {
                Clients = [],
                PageInfo = new PageInfoEntry()
            };
        }

        var entries = ParseEdges(clients, ParseClientEntry);
        var pageInfo = ParsePageInfo(clients);

        return new ListClientsResult { Clients = entries, PageInfo = pageInfo };
    }

    private static List<T> ParseEdges<T>(JsonElement connection, Func<JsonElement, T> parseNode)
    {
        var list = new List<T>();

        if (!connection.TryGetProperty("edges", out var edges)
            || edges.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var edge in edges.EnumerateArray())
        {
            if (edge.TryGetProperty("node", out var node) && node.ValueKind == JsonValueKind.Object)
            {
                list.Add(parseNode(node));
            }
        }

        return list;
    }

    private static PageInfoEntry ParsePageInfo(JsonElement connection)
    {
        if (!connection.TryGetProperty("pageInfo", out var pageInfo))
        {
            return new PageInfoEntry();
        }

        return new PageInfoEntry
        {
            HasNextPage = pageInfo.TryGetProperty("hasNextPage", out var hn)
                && hn.ValueKind == JsonValueKind.True,
            EndCursor = GetStringOrNull(pageInfo, "endCursor")
        };
    }

    private static ApiEntry ParseApiEntry(JsonElement node)
    {
        return new ApiEntry
        {
            Id = GetStringOrEmpty(node, "id"),
            Name = GetStringOrEmpty(node, "name"),
            Path = GetStringOrEmpty(node, "path"),
            Kind = GetStringOrNull(node, "kind")
        };
    }

    private static ApiKeyEntry ParseApiKeyEntry(JsonElement node)
    {
        return new ApiKeyEntry
        {
            Id = GetStringOrEmpty(node, "id"),
            Name = GetStringOrEmpty(node, "name")
        };
    }

    private static ClientEntry ParseClientEntry(JsonElement node)
    {
        return new ClientEntry
        {
            Id = GetStringOrEmpty(node, "id"),
            Name = GetStringOrEmpty(node, "name")
        };
    }

    private static bool TryGetErrors(JsonElement data, string operationField, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }

        if (!data.TryGetProperty(operationField, out var operation))
        {
            return false;
        }

        if (operation.TryGetProperty("errors", out var errors)
            && errors.ValueKind == JsonValueKind.Array)
        {
            var messages = new List<string>();
            foreach (var error in errors.EnumerateArray())
            {
                if (error.TryGetProperty("message", out var msg) && msg.GetString() is { } m)
                {
                    messages.Add(m);
                }
            }

            if (messages.Count > 0)
            {
                errorMessage = string.Join("; ", messages);
                return true;
            }
        }

        return false;
    }

    private static string GetStringOrEmpty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? GetStringOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }
}
