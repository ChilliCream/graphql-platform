namespace ChilliCream.Nitro.CommandLine;

internal static class Prompts
{
    public const string DeleteClient = "Which client do you want to delete?";

    public const string DeleteMcpFeatureCollection =
        "Which MCP Feature Collection do you want to delete?";

    public const string DeleteOpenApiCollection =
        "Which OpenAPI collection do you want to delete?";

    public const string SelectDefaultWorkspace =
        "Which workspace do you want to use as your default?";

    public const string CreateApiKeyScope =
        "Do you want to create the API key scoped to an API or the whole workspace?";

    public const string TreatDangerousChangesAsBreaking =
        "Treat dangerous changes as breaking?";

    public const string AllowBreakingSchemaChanges =
        "Allow breaking schema changes when no client breaks?";

    public const string SetAsDefaultWorkspace = "Set as default workspace?";

    public const string SelectApiForCreateMockSchema =
        "For which API do you want to create a mock schema?";

    public const string SelectApiForListMockSchemas =
        "For which API do you want to list the mock schemas?";

    public const string SelectApiForDeleteClient =
        "For which API do you want to delete a client?";

    public const string SelectApiForCreateClient =
        "For which API do you want to create a client?";

    public const string SelectApiForListClientVersions =
        "For which API do you want to list client versions?";

    public const string SelectApiForListClients =
        "For which API do you want to list the clients?";

    public const string SelectApiForEditStages =
        "For which API do you want to edit the stages?";

    public const string SelectApiForForceDeleteStage =
        "For which API do you want to force delete a stage?";

    public const string SelectApiForDisplayClients =
        "For which API do you want to display the clients?";

    public const string SelectApiForCreateApiKey =
        "For which API do you want to create an API key?";

    public const string SelectApiForDeleteMcpFeatureCollection =
        "For which API do you want to delete an MCP Feature Collection?";

    public const string SelectApiForListMcpFeatureCollections =
        "For which API do you want to list the MCP Feature Collections?";

    public const string SelectApiForCreateMcpFeatureCollection =
        "For which API do you want to create an MCP Feature Collection?";

    public const string SelectApiForDeleteOpenApiCollection =
        "For which API do you want to delete an OpenAPI collection?";

    public const string SelectApiForListOpenApiCollections =
        "For which API do you want to list the OpenAPI collections?";

    public const string SelectApiForCreateOpenApiCollection =
        "For which API do you want to create an OpenAPI collection?";

    public static string ConfirmDeleteApiKey(string keyId)
        => $"Do you really want to delete API key with ID '{keyId}'?";

    public static string ConfirmDeleteApi(string apiName)
        => $"Do you really want to delete API {apiName}";

    public static string ConfirmDeleteStage(string stageName)
        => $"Do you really want to force delete stage {stageName}";

    public static string ConfirmRevokePersonalAccessToken(string patId)
        => $"Do you really want to delete PAT with ID {patId}";

    public static string ConfirmDeleteClient(string clientId)
        => $"Do you want to delete the client with ID {clientId}?";

    public static string ConfirmDeleteMcpFeatureCollection(string id)
        => $"Do you want to delete the MCP Feature Collection with the ID {id}?";

    public static string ConfirmDeleteOpenApiCollection(string id)
        => $"Do you want to delete the OpenAPI collection with the ID {id}?";
}
