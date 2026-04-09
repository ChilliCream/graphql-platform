using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class RevokePersonalAccessTokenCommand : Command
{
    public RevokePersonalAccessTokenCommand() : base("revoke")
    {
        Description = "Revoke a personal access token.";

        Arguments.Add(Opt<IdArgument>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("pat revoke \"<pat-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IPersonalAccessTokensClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var patId = parseResult.GetRequiredValue(Opt<IdArgument>.Instance);
        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
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
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
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
