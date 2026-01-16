using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.ApiKeys;

internal sealed class DeleteApiKeyCommand : Command
{
    public DeleteApiKeyCommand() : base("delete")
    {
        Description = "Deletes an API key by ID";

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
        string keyId,
        CancellationToken cancellationToken)
    {
        var choice = await context.ConfirmWhenNotForced(
            $"Do you really want to delete api key with id {keyId}",
            cancellationToken);

        if (!choice)
        {
            throw Exit("Api key was not deleted");
        }

        var result = await client.DeleteApiKeyCommandMutation
            .ExecuteAsync(new DeleteApiKeyInput { ApiKeyId = keyId }, cancellationToken);

        console.EnsureNoErrors(result);

        var data = console.EnsureData(result);

        console.PrintErrorsAndExit(data.DeleteApiKey.Errors);

        var changeResult = data.DeleteApiKey.ApiKey;
        if (changeResult is null)
        {
            throw Exit("Could not delete api key");
        }

        console.OkLine(
            $"ApiKey {changeResult.Name.AsHighlight()} [dim](ID:{changeResult.Id})[/] was deleted");

        context.SetResult(ApiKeyDetailPrompt.From(changeResult).ToObject());

        return ExitCodes.Success;
    }
}
