using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class CreatePersonalAccessTokenCommand : Command
{
    public CreatePersonalAccessTokenCommand() : base("create")
    {
        Description = "Create a new personal access token.";

        Options.Add(Opt<OptionalPersonalAccessTokenDescriptionOption>.Instance);
        Options.Add(Opt<ExpiresOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            pat create \
              --description "CI/CD token" \
              --expires "30"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IPersonalAccessTokensClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var description = await console
            .PromptAsync(
                "Description of the Personal Access Token",
                defaultValue: null,
                parseResult,
                Opt<OptionalPersonalAccessTokenDescriptionOption>.Instance,
                ct);

        var expires = parseResult.GetValue(Opt<ExpiresOption>.Instance);

        var expiresAt = DateTimeOffset.UtcNow.AddDays(expires);

        await using (var activity = console.StartActivity(
            "Creating personal access token",
            "Failed to create the personal access token."))
        {
            var data = await client.CreatePersonalAccessTokenAsync(description, expiresAt, ct);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IUnauthorizedOperation err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            var result = data.Result;
            if (result is null)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Created personal access token '{description.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(
                new CreatePersonalAccessTokenCommandResult
                {
                    Secret = result.Secret,
                    Details = PersonalAccessTokenDetailPrompt.From(result.Token).ToObject()
                }));

            return ExitCodes.Success;
        }
    }

    public class CreatePersonalAccessTokenCommandResult
    {
        public required string Secret { get; init; }

        public required PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult Details { get; init; }
    }
}
