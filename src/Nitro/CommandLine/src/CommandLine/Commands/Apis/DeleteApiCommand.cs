using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class DeleteApiCommand : Command
{
    public DeleteApiCommand() : base("delete")
    {
        Description = "Deletes an API by ID";

        AddArgument(Opt<IdArgument>.Instance);
        AddOption(Opt<ForceOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApisClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApisClient client,
        string apiId,
        CancellationToken cancellationToken)
    {
        var apiResult = await client.GetApiForDeleteAsync(apiId, cancellationToken);
        if (apiResult is not IDeleteApiCommandQuery_Node_Api { Name: { } apiName })
        {
            throw Exit($"API with ID {apiId.AsHighlight()} was not found");
        }

        var choice = await context.ConfirmWhenNotForced(
            $"Do you really want to delete API {apiName.AsHighlight()}",
            cancellationToken);

        if (!choice)
        {
            throw Exit("API was not deleted");
        }

        var data = await client.DeleteApiAsync(apiId, cancellationToken);
        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.Api is not IApiDetailPrompt_Api api)
        {
            throw Exit("Could not delete API");
        }

        console.OkLine(
            $"API {api.Name.AsHighlight()} [dim](ID: {api.Id})[/] was deleted");

        context.SetResult(ApiDetailPrompt.From(api).ToObject());

        return ExitCodes.Success;
    }
}
