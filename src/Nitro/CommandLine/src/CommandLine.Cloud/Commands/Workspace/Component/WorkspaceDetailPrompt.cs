using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class WorkspaceDetailPrompt
{
    private readonly IWorkspaceDetailPrompt_Workspace _data;

    private WorkspaceDetailPrompt(IWorkspaceDetailPrompt_Workspace data)
    {
        _data = data;
    }

    public object ToObject()
    {
        return new { _data.Id, _data.Name, _data.Personal };
    }

    public static WorkspaceDetailPrompt From(IWorkspaceDetailPrompt_Workspace data)
        => new(data);
}
