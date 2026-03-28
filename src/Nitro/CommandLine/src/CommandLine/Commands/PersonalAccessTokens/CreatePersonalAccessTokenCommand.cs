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

internal sealed class CreatePersonalAccessTokenCommand : Command
{
    public CreatePersonalAccessTokenCommand(
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new personal access token";

        Options.Add(Opt<OptionalDescriptionOption>.Instance);
        Options.Add(Opt<ExpiresOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, client, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        IResultHolder resultHolder,
        CancellationToken ct)
    {
        console.WriteLine();
        console.WriteLine("Creating a pat key");
        console.WriteLine();

        var description = await parseResult
            .OptionOrAskAsync(
                "Description of the Personal Access Token",
                Opt<OptionalDescriptionOption>.Instance,
                console,
                ct);

        var expires = parseResult.GetValue(Opt<ExpiresOption>.Instance);

        var expiresAt = DateTimeOffset.UtcNow.AddDays(expires);

        var data = await client.CreatePersonalAccessTokenAsync(description, expiresAt, ct);
        console.PrintMutationErrorsAndExit(data.Errors);

        var result = data.Result;
        if (result is null)
        {
            throw Exit("Could not create pat.");
        }

        console.OkLine(
            $"Secret: {result.Secret.AsHighlight()} {"This secret will not be available later!"
                .AsDescription()}");

        resultHolder.SetResult(new ObjectResult(
            new CreatePersonalAccessTokenCommandResult
            {
                Secret = result.Secret,
                Details = PersonalAccessTokenDetailPrompt.From(result.Token).ToObject()
            }));

        return ExitCodes.Success;
    }

    public class CreatePersonalAccessTokenCommandResult
    {
        public required string Secret { get; init; }

        public required PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult Details { get; init; }
    }
}
