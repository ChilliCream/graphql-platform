using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ApiDetailPrompt
{
    private readonly IApiDetailPrompt_Api _data;

    private ApiDetailPrompt(IApiDetailPrompt_Api data)
    {
        _data = data;
    }

    public Result ToResult()
    {
        return new ObjectResult(new
        {
            _data.Id,
            _data.Name,
            Path = string.Join("/", _data.Path),
            Workspace = _data.Workspace is { } workspace
                ? new { workspace.Name }
                : null,
            Settings = new
            {
                SchemaRegistry = new
                {
                    _data.Settings.SchemaRegistry.TreatDangerousAsBreaking,
                    _data.Settings.SchemaRegistry.AllowBreakingSchemaChanges
                }
            }
        });
    }

    public static ApiDetailPrompt From(IApiDetailPrompt_Api data)
        => new(data);
}
