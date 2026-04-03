using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis;

internal sealed class ShowApiCommand : Command
{
    public ShowApiCommand() : base("show")
    {
        Description = "Show details of an API.";

        Arguments.Add(Opt<IdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("api show \"<api-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var client = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var id = parseResult.GetRequiredValue(Opt<IdArgument>.Instance);

        var data = await client.GetApiAsync(id, cancellationToken);

        if (data is IShowApiCommandQuery_Node_Api node)
        {
            resultHolder.SetResult(new ObjectResult(ApiDetailPrompt.From(node).ToObject()));
            return ExitCodes.Success;
        }

        throw Exit($"The API with ID '{id}' was not found.");
    }
}
