using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ApiDetailPrompt
{
    private readonly IApiDetailPrompt_Api _data;

    private ApiDetailPrompt(IApiDetailPrompt_Api data)
    {
        _data = data;
    }

    public ApiDetailPromptResult ToObject()
    {
        return new ApiDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name,
            Path = string.Join("/", _data.Path),
            Workspace = _data.Workspace is { } workspace
                ? new ApiDetailPromptWorkspace { Name = workspace.Name }
                : null,
            ApiDetailPromptSettings = new ApiDetailPromptSettings
            {
                ApiDetailPromptSchemaRegistry = new ApiDetailPromptSchemaRegistrySettings
                {
                    TreatDangerousAsBreaking = _data.Settings.SchemaRegistry.TreatDangerousAsBreaking,
                    AllowBreakingSchemaChanges = _data.Settings.SchemaRegistry.AllowBreakingSchemaChanges
                }
            }
        };
    }

    public static ApiDetailPrompt From(IApiDetailPrompt_Api data)
        => new(data);

    public class ApiDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required string Path { get; init; }

        public required ApiDetailPromptWorkspace? Workspace { get; init; }

        public required ApiDetailPromptSettings ApiDetailPromptSettings { get; init; }
    }

    public class ApiDetailPromptWorkspace
    {
        public required string Name { get; init; }
    }

    public class ApiDetailPromptSettings
    {
        public required ApiDetailPromptSchemaRegistrySettings ApiDetailPromptSchemaRegistry { get; init; }
    }

    public class ApiDetailPromptSchemaRegistrySettings
    {
        public required bool TreatDangerousAsBreaking { get; init; }

        public required bool AllowBreakingSchemaChanges { get; init; }
    }
}
