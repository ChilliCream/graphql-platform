using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class CreatePersonalAccessTokenCommand : Command
{
    public CreatePersonalAccessTokenCommand(
        INitroConsole console,
        IPersonalAccessTokensClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new personal access token";

        Options.Add(Opt<OptionalDescriptionOption>.Instance);
        Options.Add(Opt<ExpiresOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var description = await console
            .PromptAsync(
                "Description of the Personal Access Token",
                defaultValue: null,
                parseResult,
                Opt<OptionalDescriptionOption>.Instance,
                ct);

        var expires = parseResult.GetValue(Opt<ExpiresOption>.Instance);

        var expiresAt = DateTimeOffset.UtcNow.AddDays(expires);

        await using (var activity = console.StartActivity("Creating personal access token..."))
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
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    await console.Error.WriteLineAsync(errorMessage);
                }

                return ExitCodes.Error;
            }

            var result = data.Result;
            if (result is null)
            {
                activity.Fail();
                await console.Error.WriteLineAsync("Could not create personal access token.");
                return ExitCodes.Error;
            }

            activity.Success("Successfully created personal access token!");

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
