using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class EnvironmentDetailPrompt
{
    private readonly IEnvironmentDetailPrompt_Environment _data;

    private EnvironmentDetailPrompt(IEnvironmentDetailPrompt_Environment data)
    {
        _data = data;
    }

    public EnvironmentDetailPromptResult ToObject()
    {
        return new EnvironmentDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name,
            Workspace = _data.Workspace is { } workspace
                ? new EnvironmentDetailPromptWorkspace { Name = workspace.Name }
                : null
        };
    }

    public static EnvironmentDetailPrompt From(IEnvironmentDetailPrompt_Environment data)
        => new(data);

    public class EnvironmentDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required EnvironmentDetailPromptWorkspace? Workspace { get; init; }
    }

    public class EnvironmentDetailPromptWorkspace
    {
        public required string Name { get; init; }
    }
}
