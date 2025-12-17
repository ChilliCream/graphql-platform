namespace HotChocolate.Adapters.OpenApi.Packaging;

internal static class FileNames
{
    public const string ArchiveMetadata = "archive-metadata.json";

    public static string GetEndpointOperationPath(OpenApiEndpointKey key)
        => $"endpoints/{GetEndpointDirectoryName(key)}/operation.graphql";

    public static string GetEndpointSettingsPath(OpenApiEndpointKey key)
        => $"endpoints/{GetEndpointDirectoryName(key)}/settings.json";

    public static string GetModelFragmentPath(string name)
        => $"models/{name}/fragment.graphql";

    public static string GetModelSettingsPath(string name)
        => $"models/{name}/settings.json";

    private static string GetEndpointDirectoryName(OpenApiEndpointKey key)
        => $"{key.HttpMethod}_{key.Route}";
}
