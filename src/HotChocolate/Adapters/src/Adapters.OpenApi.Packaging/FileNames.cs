namespace HotChocolate.Adapters.OpenApi.Packaging;

internal static class FileNames
{
    public const string ArchiveMetadata = "archive-metadata.json";

    public static string GetEndpointOperationPath(string name)
        => $"endpoints/{name}/operation.graphql";

    public static string GetEndpointSettingsPath(string name)
        => $"endpoints/{name}/settings.json";

    public static string GetModelFragmentPath(string name)
        => $"models/{name}/fragment.graphql";

    public static FileKind GetFileKind(string fileName)
    {
        switch (Path.GetFileName(fileName))
        {
            case "operation.graphql":
                return FileKind.Operation;

            case "settings.json":
                return FileKind.Settings;

            case "archive-metadata.json":
                return FileKind.Metadata;

            case "fragment.graphql":
                return FileKind.Fragment;

            default:
                return FileKind.Unknown;
        }
    }
}
