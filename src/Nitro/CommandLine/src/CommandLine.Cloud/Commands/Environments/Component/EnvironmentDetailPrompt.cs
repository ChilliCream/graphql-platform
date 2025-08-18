using ChilliCream.Nitro.CommandLine.Cloud.Client;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class EnvironmentDetailPrompt
{
    private readonly IEnvironmentDetailPrompt_Environment _data;

    private EnvironmentDetailPrompt(IEnvironmentDetailPrompt_Environment data)
    {
        _data = data;
    }

    public object ToObject()
    {
        return new
        {
            _data.Id,
            _data.Name,
            Workspace = _data.Workspace is { } workspace
                ? new { workspace.Name }
                : null
        };
    }

    public static EnvironmentDetailPrompt From(IEnvironmentDetailPrompt_Environment data)
        => new(data);
}
