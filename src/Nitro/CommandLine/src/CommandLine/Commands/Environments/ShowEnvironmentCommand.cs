using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class ShowEnvironmentCommand : Command
{
    public ShowEnvironmentCommand() : base("show")
    {
        Description = "Show details of an environment.";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("environment show \"<environment-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var client = services.GetRequiredService<IEnvironmentsClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetRequiredValue(Opt<IdArgument>.Instance);

        var model = await client.GetEnvironmentAsync(id, cancellationToken);

        if (model is IShowEnvironmentCommandQuery_Node_Environment environmentModel)
        {
            resultHolder.SetResult(new ObjectResult(EnvironmentDetailPrompt.From(environmentModel).ToObject()));
            return ExitCodes.Success;
        }

        throw Exit($"The environment with ID '{id}' was not found.");
    }
}
