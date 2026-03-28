using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class DeleteApiCommand : Command
{
    public DeleteApiCommand(
        INitroConsole console,
        IApisClient client,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Deletes an API by ID";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<ForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient client,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var apiId = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var apiResult = await client.GetApiForDeleteAsync(apiId, cancellationToken);
        if (apiResult is not IDeleteApiCommandQuery_Node_Api { Name: { } apiName })
        {
            throw Exit($"API with ID {apiId.AsHighlight()} was not found");
        }

        // TODO: Fix
        var force = parseResult.GetValue(Opt<ForceOption>.Instance); // is not null;
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you really want to delete API {apiName.AsHighlight()}",
                cancellationToken);

            if (!confirmed)
            {
                throw Exit("API was not deleted");
            }
        }

        var data = await client.DeleteApiAsync(apiId, cancellationToken);
        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.Api is not IApiDetailPrompt_Api api)
        {
            throw Exit("Could not delete API");
        }

        console.OkLine(
            $"API {api.Name.AsHighlight()} [dim](ID: {api.Id})[/] was deleted");

        resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(api).ToObject()));

        return ExitCodes.Success;
    }
}
