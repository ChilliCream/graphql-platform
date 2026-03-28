using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ShowApiCommand : Command
{
    public ShowApiCommand() : base("show")
    {
        Description = "Shows details of an API";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IApisClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IApisClient client,
        string id,
        CancellationToken cancellationToken)
    {
        var data = await client.ShowApiAsync(id, cancellationToken);

        if (data is IShowApiCommandQuery_Node_Api node)
        {
            context.SetResult(ApiDetailPrompt.From(node).ToObject());
        }
        else
        {
            console.WriteErrorLine(
                $"Could not find an API with ID '{id}'.");
        }

        return ExitCodes.Success;
    }
}
