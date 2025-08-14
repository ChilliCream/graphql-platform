using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;

namespace ChilliCream.Nitro.CLI;

public static class ApiInvocationContextExtensions
{
    public static async Task<string> GetOrSelectApiId(
        this InvocationContext context,
        string message)
    {
        var ct = context.BindingContext.GetRequiredService<CancellationToken>();
        var client = context.BindingContext.GetRequiredService<IApiClient>();
        var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);

        if (apiId is null)
        {
            var workspaceId = context.RequireWorkspaceId();
            var selectedApi = await SelectApiPrompt
                .New(client, workspaceId)
                .Title(message)
                .RenderAsync(console, ct) ?? throw ThrowHelper.NoApiSelected();
            apiId = selectedApi.Id;
        }

        console.OkQuestion(message, apiId);

        return apiId;
    }
}
