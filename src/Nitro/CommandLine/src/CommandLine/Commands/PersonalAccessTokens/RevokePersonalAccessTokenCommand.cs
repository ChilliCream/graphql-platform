using System.CommandLine.Invocation;
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
    public RevokePersonalAccessTokenCommand() : base("revoke")
    {
        Description = "Revokes a personal access token";

        AddArgument(Opt<IdArgument>.Instance);
        AddOption(Opt<ForceOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IPersonalAccessTokensClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IPersonalAccessTokensClient client,
        string patId,
        CancellationToken cancellationToken)
    {
        var confirmMessage = $"Do you really want to delete PAT with ID {patId}";
        var force = await context.ConfirmWhenNotForced(confirmMessage, cancellationToken);
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

        context.SetResult(PersonalAccessTokenDetailPrompt.From(token).ToObject());

        return ExitCodes.Success;
    }
}
