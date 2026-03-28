using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class ShowEnvironmentCommand : Command
{
    public ShowEnvironmentCommand(
        INitroConsole console,
        IEnvironmentsClient client,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Shows details of an environment";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IEnvironmentsClient client,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var data = await client.ShowEnvironmentAsync(id, cancellationToken);

        if (data is IShowEnvironmentCommandQuery_Node_Environment node)
        {
            resultHolder.SetResult(new ObjectResult(EnvironmentDetailPrompt.From(node).ToObject()));
        }
        else
        {
            console.ErrorLine(
                $"Could not find an environment with ID {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
