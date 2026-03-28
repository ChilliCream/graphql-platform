using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;

public static class ApiInvocationContextExtensions
{
    public static Task<string> GetOrPromptForApiIdAsync(
        this InvocationContext context,
        string message)
        => context.GetOrSelectApiId(message);

    public static async Task<string> GetOrSelectApiId(
        this InvocationContext context,
        string message)
    {
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        var console = context.BindingContext.GetRequiredService<INitroConsole>();

        if (apiId is null)
        {
            var ct = context.BindingContext.GetRequiredService<CancellationToken>();
            var client = context.BindingContext.GetRequiredService<IApisClient>();
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
