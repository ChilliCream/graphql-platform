using HotChocolate.Transport;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;

internal static class ManagementQueries
{
    private const string CreateApiMutation = """
        mutation($workspaceId: ID!, $path: [String!]!, $name: String!, $kind: ApiKind) {
          pushWorkspaceChanges(
            input: {
              changes: [
                {
                  api: {
                    create: {
                      name: $name
                      path: $path
                      referenceId: "api"
                      workspaceId: $workspaceId
                      kind: $kind
                    }
                  }
                }
              ]
            }
          ) {
            changes {
              referenceId
              error { message code }
              result { ... on Api { id name path kind } }
            }
            errors { message code }
          }
        }
        """;

    private const string ListApisQuery = """
        query($workspaceId: ID!, $first: Int, $after: Version) {
          workspaceById(workspaceId: $workspaceId) {
            apis(first: $first, after: $after) {
              edges {
                node { id name path kind }
              }
              pageInfo { hasNextPage endCursor }
            }
          }
        }
        """;

    private const string UpdateApiSettingsMutation = """
        mutation($input: UpdateApiSettingsInput!) {
          updateApiSettings(input: $input) {
            api { name path }
            errors { message code }
          }
        }
        """;

    private const string CreateApiKeyMutation = """
        mutation($input: CreateApiKeyInput!) {
          createApiKey(input: $input) {
            result {
              key { id name }
              secret
            }
            errors { message code }
          }
        }
        """;

    private const string ListApiKeysQuery = """
        query($workspaceId: ID!, $first: Int, $after: String) {
          workspaceById(workspaceId: $workspaceId) {
            apiKeys(first: $first, after: $after) {
              edges {
                node { id name }
              }
              pageInfo { hasNextPage endCursor }
            }
          }
        }
        """;

    private const string CreateClientMutation = """
        mutation($input: CreateClientInput!) {
          createClient(input: $input) {
            client { id name }
            errors { message code }
          }
        }
        """;

    private const string ListClientsQuery = """
        query($apiId: ID!, $first: Int, $after: String) {
          node(id: $apiId) {
            ... on Api {
              clients(first: $first, after: $after) {
                edges {
                  node { id name }
                }
                pageInfo { hasNextPage endCursor }
              }
            }
          }
        }
        """;

    public static OperationRequest BuildCreateApi(
        string workspaceId,
        string name,
        string[] path,
        string? kind)
    {
        var variables = new Dictionary<string, object?>
        {
            ["workspaceId"] = workspaceId,
            ["name"] = name,
            ["path"] = path,
            ["kind"] = kind?.ToUpperInvariant()
        };

        return new OperationRequest(CreateApiMutation, variables: variables);
    }

    public static OperationRequest BuildListApis(
        string workspaceId,
        int first,
        string? after)
    {
        var variables = new Dictionary<string, object?>
        {
            ["workspaceId"] = workspaceId,
            ["first"] = first,
            ["after"] = after
        };

        return new OperationRequest(ListApisQuery, variables: variables);
    }

    public static OperationRequest BuildUpdateApiSettings(
        string apiId,
        bool? treatDangerousAsBreaking,
        bool? allowBreakingSchemaChanges)
    {
        var input = new Dictionary<string, object?> { ["apiId"] = apiId };

        if (treatDangerousAsBreaking.HasValue)
        {
            input["treatDangerousChangesAsBreaking"] = treatDangerousAsBreaking.Value;
        }

        if (allowBreakingSchemaChanges.HasValue)
        {
            input["allowBreakingSchemaChanges"] = allowBreakingSchemaChanges.Value;
        }

        var variables = new Dictionary<string, object?> { ["input"] = input };

        return new OperationRequest(UpdateApiSettingsMutation, variables: variables);
    }

    public static OperationRequest BuildCreateApiKey(
        string workspaceId,
        string name,
        string? apiId,
        string? stageName)
    {
        var input = new Dictionary<string, object?>
        {
            ["workspaceId"] = workspaceId,
            ["name"] = name
        };

        if (apiId is not null)
        {
            input["apiId"] = apiId;
        }

        if (stageName is not null)
        {
            input["stageName"] = stageName;
        }

        var variables = new Dictionary<string, object?> { ["input"] = input };

        return new OperationRequest(CreateApiKeyMutation, variables: variables);
    }

    public static OperationRequest BuildListApiKeys(
        string workspaceId,
        int first,
        string? after)
    {
        var variables = new Dictionary<string, object?>
        {
            ["workspaceId"] = workspaceId,
            ["first"] = first,
            ["after"] = after
        };

        return new OperationRequest(ListApiKeysQuery, variables: variables);
    }

    public static OperationRequest BuildCreateClient(
        string apiId,
        string name)
    {
        var input = new Dictionary<string, object?>
        {
            ["apiId"] = apiId,
            ["name"] = name
        };

        var variables = new Dictionary<string, object?> { ["input"] = input };

        return new OperationRequest(CreateClientMutation, variables: variables);
    }

    public static OperationRequest BuildListClients(
        string apiId,
        int first,
        string? after)
    {
        var variables = new Dictionary<string, object?>
        {
            ["apiId"] = apiId,
            ["first"] = first,
            ["after"] = after
        };

        return new OperationRequest(ListClientsQuery, variables: variables);
    }
}
