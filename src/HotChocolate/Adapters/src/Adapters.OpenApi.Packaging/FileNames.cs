namespace HotChocolate.Adapters.OpenApi.Packaging;

internal static class FileNames
{
    public const string ArchiveMetadata = "archive-metadata.json";

    public static string GetEndpointDocumentPath(OpenApiEndpointKey key)
        => $"endpoints/{GetEndpointDirectoryName(key)}/document.graphql";

    public static string GetEndpointSettingsPath(OpenApiEndpointKey key)
        => $"endpoints/{GetEndpointDirectoryName(key)}/settings.json";

    public static string GetModelDocumentPath(string name)
        => $"models/{name}/document.graphql";

    public static string GetModelSettingsPath(string name)
        => $"models/{name}/settings.json";

    private static string GetEndpointDirectoryName(OpenApiEndpointKey key)
        => $"{key.HttpMethod}_{key.Route}";
}
