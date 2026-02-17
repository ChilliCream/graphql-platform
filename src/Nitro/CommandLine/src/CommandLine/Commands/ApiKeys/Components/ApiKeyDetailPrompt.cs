using ChilliCream.Nitro.CommandLine.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;

internal sealed class ApiKeyDetailPrompt
{
    private readonly IApiKeyDetailPrompt_ApiKey _data;

    private ApiKeyDetailPrompt(IApiKeyDetailPrompt_ApiKey data)
    {
        _data = data;
    }

    public ApiKeyDetailPromptResult ToObject()
    {
        return new ApiKeyDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name,
            Workspace = _data.Workspace is { } workspace
                ? new ApiKeyDetailPromptWorkspace { Name = workspace.Name }
                : null
        };
    }

    public static ApiKeyDetailPrompt From(IApiKeyDetailPrompt_ApiKey data)
        => new(data);

    public class ApiKeyDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required ApiKeyDetailPromptWorkspace? Workspace { get; init; }
    }

    public class ApiKeyDetailPromptWorkspace
    {
        public required string Name { get; init; }
    }
}
