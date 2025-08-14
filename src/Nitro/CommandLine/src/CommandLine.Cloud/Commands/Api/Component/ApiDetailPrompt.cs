using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Results;

namespace ChilliCream.Nitro.CLI;

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
