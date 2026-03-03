namespace ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

internal sealed class ProjectSettings
{
    public string? Version { get; set; }

    public string? WorkspaceId { get; set; }

    public string? CloudUrl { get; set; }

    public string? DefaultStage { get; set; }

    public List<string>? StyleTags { get; set; }

    public List<ApiSettings>? Apis { get; set; }

    public List<ClientSettings>? Clients { get; set; }

    public List<McpCollectionSettings>? McpCollections { get; set; }

    public List<OpenApiCollectionSettings>? OpenApiCollections { get; set; }
}

internal sealed class ApiSettings
{
    public required string Id { get; set; }

    public string? Name { get; set; }

    public string? Path { get; set; }

    public string? SchemaFile { get; set; }

    public string? DefaultStage { get; set; }

    public string? DefaultTag { get; set; }
}

internal sealed class ClientSettings
{
    public required string Id { get; set; }

    public string? Name { get; set; }

    public string? Path { get; set; }

    public string? OperationsFile { get; set; }

    public string? DefaultStage { get; set; }
}

internal sealed class McpCollectionSettings
{
    public required string Id { get; set; }

    public string? Name { get; set; }

    public string? Path { get; set; }

    public List<string>? PromptPatterns { get; set; }

    public List<string>? ToolPatterns { get; set; }

    public string? DefaultStage { get; set; }
}

internal sealed class OpenApiCollectionSettings
{
    public required string Id { get; set; }

    public string? Name { get; set; }

    public string? Path { get; set; }

    public List<string>? FilePatterns { get; set; }

    public string? DefaultStage { get; set; }
}
