using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ShowApiCommand : Command
{
    public ShowApiCommand(
        INitroConsole console,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Shows details of an API";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        IApisClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var data = await client.ShowApiAsync(id, cancellationToken);

        if (data is IShowApiCommandQuery_Node_Api node)
        {
            resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(node).ToObject()));
            return ExitCodes.Success;
        }

        throw Exit($"The API with ID '{id}' was not found.");
    }
}
