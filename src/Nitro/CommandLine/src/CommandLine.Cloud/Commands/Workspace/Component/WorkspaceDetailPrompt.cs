using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class WorkspaceDetailPrompt
{
    private readonly IWorkspaceDetailPrompt_Workspace _data;

    private WorkspaceDetailPrompt(IWorkspaceDetailPrompt_Workspace data)
    {
        _data = data;
    }

    public WorkspaceDetailPromptResult ToObject()
    {
        return new WorkspaceDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name,
            Personal = _data.Personal
        };
    }

    public static WorkspaceDetailPrompt From(IWorkspaceDetailPrompt_Workspace data)
        => new(data);

    public class WorkspaceDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required bool Personal { get; init; }
    }
}
