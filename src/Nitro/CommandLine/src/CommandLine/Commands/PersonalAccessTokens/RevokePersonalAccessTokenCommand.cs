using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Client;
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
            Bind.FromServiceProvider<IApiClient>(),
            Opt<IdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string patId,
        CancellationToken cancellationToken)
    {
        var confirmMessage = $"Do you really want to delete path with id {patId}";
        var force = await context.ConfirmWhenNotForced(confirmMessage, cancellationToken);
        if (!force)
        {
            throw Exit("PAT was not deleted");
        }

        var result = await client.RevokePersonalAccessTokenCommandMutation
            .ExecuteAsync(new RevokePersonalAccessTokenInput { Id = patId }, cancellationToken);

        console.EnsureNoErrors(result);

        var data = console.EnsureData(result);

        console.PrintErrorsAndExit(data.RevokePersonalAccessToken.Errors);

        var changeResult = data.RevokePersonalAccessToken.PersonalAccessToken;
        if (changeResult is null)
        {
            throw Exit("Could not delete PAT");
        }

        console.OkLine(
            $"PersonalAccessToken {changeResult.Description.AsHighlight()} [dim](ID:{changeResult.Id})[/] was deleted");

        context.SetResult(PersonalAccessTokenDetailPrompt.From(changeResult).ToObject());

        return ExitCodes.Success;
    }
}
