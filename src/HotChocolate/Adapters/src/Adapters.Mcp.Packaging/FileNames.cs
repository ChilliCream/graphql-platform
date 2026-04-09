namespace HotChocolate.Adapters.Mcp.Packaging;

internal static class FileNames
{
    public const string ArchiveMetadata = "archive-metadata.json";

    public static string GetPromptSettingsPath(string name)
        => $"prompts/{name}/settings.json";

    public static string GetToolDocumentPath(string name)
        => $"tools/{name}/document.graphql";

    public static string GetToolSettingsPath(string name)
        => $"tools/{name}/settings.json";

    public static string GetToolViewPath(string name)
        => $"tools/{name}/view.html";
}
