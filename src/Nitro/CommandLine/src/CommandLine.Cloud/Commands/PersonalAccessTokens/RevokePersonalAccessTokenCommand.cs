using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.PersonalAccessToken;

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
