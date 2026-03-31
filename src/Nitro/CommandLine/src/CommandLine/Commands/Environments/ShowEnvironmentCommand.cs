using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class ShowEnvironmentCommand : Command
{
    public ShowEnvironmentCommand(
        INitroConsole console,
        IEnvironmentsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("show")
    {
        Description = "Show details of an environment.";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        IEnvironmentsClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetValue(Opt<IdArgument>.Instance)!;

        var model = await client.GetEnvironmentAsync(id, cancellationToken);

        if (model is IShowEnvironmentCommandQuery_Node_Environment environmentModel)
        {
            resultHolder.SetResult(new ObjectResult(EnvironmentDetailPrompt.From(environmentModel).ToObject()));
            return ExitCodes.Success;
        }

        throw Exit($"The environment with ID '{id}' was not found.");
    }
}
