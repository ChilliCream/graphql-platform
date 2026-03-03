namespace ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

/// <summary>
/// Resolved values from .nitro/settings.json for the current directory context.
/// </summary>
internal sealed class ProjectContext
{
    public string? WorkspaceId { get; init; }

    public string? CloudUrl { get; init; }

    public string? DefaultStage { get; init; }

    public IReadOnlyList<string> StyleTags { get; init; } = [];

    public ApiSettings? ActiveApi { get; init; }

    public ClientSettings? ActiveClient { get; init; }

    public McpCollectionSettings? ActiveMcpCollection { get; init; }

    public OpenApiCollectionSettings? ActiveOpenApiCollection { get; init; }

    /// <summary>
    /// The directory containing .nitro/settings.json (used to resolve relative paths).
    /// </summary>
    public string? SettingsRoot { get; init; }
}
