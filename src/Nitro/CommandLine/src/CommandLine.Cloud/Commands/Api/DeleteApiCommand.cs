using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI;

internal sealed class DeleteApiCommand : Command
{
    public DeleteApiCommand() : base("delete")
    {
        Description = "Deletes a api by id";

        AddArgument(Opt<IdArgument>.Instance);
        AddOption(Opt<ForceOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string apiId,
        CancellationToken cancellationToken)
    {
        var apiResult = await client.DeleteApiCommandQuery.ExecuteAsync(apiId, cancellationToken);
        if (apiResult is not
            {
                Data.Node: IDeleteApiCommandQuery_Api
                {
                    Name: { } apiName
                }
            })
        {
            throw Exit($"Api with id {apiId.AsHighlight()} was not found");
        }

        var choice = await context.ConfirmWhenNotForced(
            $"Do you really want to delete api {apiName.AsHighlight()}",
            cancellationToken);

        if (!choice)
        {
            throw Exit("Api was not deleted");
        }

        var result = await client.DeleteApiCommandMutation
            .ExecuteAsync(apiId, cancellationToken);

        console.EnsureNoErrors(result);

        var data = console.EnsureData(result);

        console.PrintErrorsAndExit(data.DeleteApiById.Errors);

        if (data.DeleteApiById.Api is not IApiDetailPrompt_Api changeResult)
        {
            throw Exit("Could not delete api");
        }

        console.OkLine(
            $"Api {changeResult.Name.AsHighlight()} [dim](ID:{changeResult.Id})[/] was deleted");

        context.SetResult(ApiDetailPrompt.From(changeResult).ToResult());

        return ExitCodes.Success;
    }
}
