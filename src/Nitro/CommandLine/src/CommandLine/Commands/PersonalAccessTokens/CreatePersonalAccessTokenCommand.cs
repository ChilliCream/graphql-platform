using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;

internal sealed class CreatePersonalAccessTokenCommand : Command
{
    public CreatePersonalAccessTokenCommand() : base("create")
    {
        Description = "Creates a new personal access token";

        AddOption(Opt<OptionalDescriptionOption>.Instance);
        AddOption(Opt<ExpiresOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IPersonalAccessTokensClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IPersonalAccessTokensClient client,
        CancellationToken ct)
    {
        console.WriteLine();
        console.WriteLine("Creating a pat key");
        console.WriteLine();

        var description = await context
            .OptionOrAskAsync(
                "Description of the Personal Access Token",
                Opt<OptionalDescriptionOption>.Instance,
                ct);

        var expires = context.ParseResult.GetValueForOption(Opt<ExpiresOption>.Instance);

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

        context.SetResult(
            new CreatePersonalAccessTokenCommandResult
            {
                Secret = result.Secret,
                Details = PersonalAccessTokenDetailPrompt.From(result.Token).ToObject()
            });

        return ExitCodes.Success;
    }

    public class CreatePersonalAccessTokenCommandResult
    {
        public required string Secret { get; init; }

        public required PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult Details { get; init; }
    }
}
