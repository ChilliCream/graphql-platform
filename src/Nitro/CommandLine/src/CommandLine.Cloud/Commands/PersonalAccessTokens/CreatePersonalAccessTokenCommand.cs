using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Commands.PersonalAccessToken;

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
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
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

        var input =
            new CreatePersonalAccessTokenInput { Description = description, ExpiresAt = expiresAt };

        var result = await client.CreatePersonalAccessTokenCommandMutation
            .ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);

        var data = console.EnsureData(result);

        console.PrintErrorsAndExit(data.CreatePersonalAccessToken.Errors);

        var changeResult = data.CreatePersonalAccessToken.Result;
        if (changeResult is null)
        {
            throw Exit("Could not create pat.");
        }

        console.OkLine(
            $"Secret: {changeResult.Secret.AsHighlight()} {"This secret will not be available later!"
                .AsDescription()}");

        if (changeResult.Token is IPersonalAccessTokenDetailPrompt_PersonalAccessToken detail)
        {
            context.SetResult(
                new
                {
                    changeResult.Secret,
                    Details = PersonalAccessTokenDetailPrompt.From(detail).ToObject()
                });
        }

        return ExitCodes.Success;
    }
}
