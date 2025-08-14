using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using ChilliCream.Nitro.CLI.Results;
using static ChilliCream.Nitro.CLI.ThrowHelper;

namespace ChilliCream.Nitro.CLI.Commands.ApiKey;

internal sealed class CreateApiKeyCommand : Command
{
    public CreateApiKeyCommand() : base("create")
    {
        Description = "Creates a new api key";

        AddOption(Opt<ApiKeyNameOption>.Instance);
        AddOption(Opt<WorkspaceIdOption>.Instance);
        AddOption(Opt<OptionalApiIdOption>.Instance);

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
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Creating a api key...");
        console.WriteLine();

        const string apiMessage = "For which api do you want to create a api key?";
        var apiId = await context.GetOrSelectApiId(apiMessage);
        var name = await context
            .OptionOrAskAsync("Name", Opt<ApiKeyNameOption>.Instance, cancellationToken);

        var input = new CreateApiKeyForApiInput { ApiId = apiId, Name = name };
        var result =
            await client.CreateApiKeyCommandMutation.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);

        var data = console.EnsureData(result);

        console.PrintErrorsAndExit(data.CreateApiKeyForApi.Errors);

        var changeResult = data.CreateApiKeyForApi.Result;
        if (changeResult is null)
        {
            throw Exit("Could not create api.");
        }

        console.OkLine(
            $"Secret: {changeResult.Secret.AsHighlight()} {"This secret will not be available later!".AsDescription()}");

        if (changeResult.Key is IApiKeyDetailPrompt_ApiKey detail)
        {
            context.SetResult(new
            {
                changeResult.Secret,
                Details = ApiKeyDetailPrompt.From(detail).ToResult()
            });
        }

        return ExitCodes.Success;
    }
}
