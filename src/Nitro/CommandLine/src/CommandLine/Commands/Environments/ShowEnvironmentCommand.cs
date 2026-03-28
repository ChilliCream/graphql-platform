using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class ShowEnvironmentCommand : Command
{
    public ShowEnvironmentCommand() : base("show")
    {
        Description = "Shows details of an environment";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IEnvironmentsClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IEnvironmentsClient client,
        string id,
        CancellationToken cancellationToken)
    {
        var data = await client.ShowEnvironmentAsync(id, cancellationToken);

        if (data is IShowEnvironmentCommandQuery_Node_Environment node)
        {
            context.SetResult(EnvironmentDetailPrompt.From(node).ToObject());
        }
        else
        {
            console.ErrorLine(
                $"Could not find an environment with ID {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
