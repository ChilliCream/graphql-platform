using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class RevokePersonalAccessTokenCommand : Command
{
    public RevokePersonalAccessTokenCommand(
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder) : base("revoke")
    {
        Description = "Revokes a personal access token";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<ForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                resultHolder,
                parseResult.GetValue(Opt<IdArgument>.Instance)!,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder,
        string patId,
        CancellationToken cancellationToken)
    {
        var confirmMessage = $"Do you really want to delete PAT with ID {patId}";
        var force = await parseResult.ConfirmWhenNotForced(confirmMessage, console, cancellationToken);
        if (!force)
        {
            throw Exit("PAT was not deleted");
        }

        var data = await client.RevokePersonalAccessTokenAsync(patId, cancellationToken);
        console.PrintMutationErrorsAndExit(data.Errors);

        if (data.PersonalAccessToken is not IPersonalAccessTokenDetailPrompt_PersonalAccessToken token)
        {
            throw Exit("Could not delete PAT");
        }

        console.OkLine(
            $"PersonalAccessToken {token.Description.AsHighlight()} [dim](ID: {token.Id})[/] was deleted");

        resultHolder.SetResult(new ObjectResult(PersonalAccessTokenDetailPrompt.From(token).ToObject()));

        return ExitCodes.Success;
    }
}
