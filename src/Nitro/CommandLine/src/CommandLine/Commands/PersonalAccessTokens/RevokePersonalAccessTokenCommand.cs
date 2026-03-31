using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
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
        ISessionService sessionService,
        IResultHolder resultHolder) : base("revoke")
    {
        Description = "Revoke a personal access token.";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<ForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                client,
                sessionService,
                resultHolder,
                parseResult.GetValue(Opt<IdArgument>.Instance)!,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        string patId,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var force = parseResult.GetValue(Opt<ForceOption>.Instance);
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you really want to delete PAT with ID {patId}",
                cancellationToken);

            if (!confirmed)
            {
                throw Exit("PAT was not deleted.");
            }
        }

        await using (var activity = console.StartActivity(
            $"Revoking personal access token '{patId.EscapeMarkup()}'",
            "Failed to revoke the personal access token."))
        {
            var data = await client.RevokePersonalAccessTokenAsync(patId, cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IPersonalAccessTokenNotFoundError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.PersonalAccessToken is not IPersonalAccessTokenDetailPrompt_PersonalAccessToken token)
            {
                activity.Fail();
                console.Error.WriteErrorLine("Could not revoke personal access token.");
                return ExitCodes.Error;
            }

            activity.Success($"Revoked personal access token '{patId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(PersonalAccessTokenDetailPrompt.From(token).ToObject()));

            return ExitCodes.Success;
        }
    }
}
