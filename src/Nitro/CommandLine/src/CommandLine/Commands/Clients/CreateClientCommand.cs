using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class CreateClientCommand : Command
{
    public CreateClientCommand() : base("create")
    {
        Description = "Create a new client.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<ClientNameOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client create \
              --name "my-client" \
              --api-id "<api-id>"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<IClientsClient>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var apiId = await console.GetOrPromptForApiIdAsync(
            "For which API do you want to create a client?",
            parseResult,
            apisClient,
            sessionService,
            cancellationToken);

        var name = await console
            .PromptAsync("Name", defaultValue: null, parseResult, Opt<ClientNameOption>.Instance, cancellationToken);

        await using (var activity = console.StartActivity(
            $"Creating client '{name.EscapeMarkup()}' for API '{apiId.EscapeMarkup()}'",
            "Failed to create the client."))
        {
            var data = await client.CreateClientAsync(apiId, name, cancellationToken);

            if (data.Errors?.Count > 0)
            {
                await activity.FailAllAsync();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        ICreateClientCommandMutation_CreateClient_Errors_ApiNotFoundError err => err.Message,
                        ICreateClientCommandMutation_CreateClient_Errors_UnauthorizedOperation err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (data.Client is not IClientDetailPrompt_Client createdClient)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Created client '{name.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(ClientDetailPrompt.From(createdClient).ToObject()));

            return ExitCodes.Success;
        }
    }
}
