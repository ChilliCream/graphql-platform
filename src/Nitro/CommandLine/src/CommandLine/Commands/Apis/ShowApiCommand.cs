using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ShowApiCommand : Command
{
    public ShowApiCommand(
        INitroConsole console,
        IApisClient client,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Shows details of an API";

        Arguments.Add(Opt<IdArgument>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient client,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var data = await client.ShowApiAsync(id, cancellationToken);

        if (data is IShowApiCommandQuery_Node_Api node)
        {
            resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(node).ToObject()));
        }
        else
        {
            console.ErrorLine($"Could not find an API with ID '{id}'.");
        }

        return ExitCodes.Success;
    }
}
