using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ApiKeyDetailPrompt
{
    private readonly IApiKeyDetailPrompt_ApiKey _data;

    private ApiKeyDetailPrompt(IApiKeyDetailPrompt_ApiKey data)
    {
        _data = data;
    }

    public Result ToResult()
    {
        return new ObjectResult(new
        {
            _data.Id,
            _data.Name,
            Workspace = _data.Workspace is { } workspace
                ? new { workspace.Name }
                : null
        });
    }

    public static ApiKeyDetailPrompt From(IApiKeyDetailPrompt_ApiKey data)
        => new(data);
}
