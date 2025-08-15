using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ShowEnvironmentCommand : Command
{
    public ShowEnvironmentCommand() : base("show")
    {
        Description = "Shows details of an environment";

        AddArgument(Opt<IdArgument>.Instance);

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
        string id,
        CancellationToken cancellationToken)
    {
        var result = await client.ShowEnvironmentCommandQuery.ExecuteAsync(id, cancellationToken);

        var data = result.EnsureData();

        if (data.Node is IEnvironmentDetailPrompt_Environment node)
        {
            context.SetResult(EnvironmentDetailPrompt.From(node).ToObject());
        }
        else
        {
            console.ErrorLine(
                $"Could not find a environment with id {id.EscapeMarkup().AsHighlight()}");
        }

        return ExitCodes.Success;
    }
}
