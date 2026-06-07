using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class UnpublishClientCommand : Command
{
    public UnpublishClientCommand() : base("unpublish")
    {
        Description = "Unpublish a client version from a stage.";

        Options.Add(Opt<ClientTagsToUnpublishOption>.Instance);
        Options.Add(Opt<OptionalStageNameOption>.Instance);
        Options.Add(Opt<OptionalClientIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            client unpublish \
              --client-id "<client-id>" \
              --stage "dev" \
              --tag "v1"
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
        var stagesClient = services.GetRequiredService<IStagesClient>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        string clientId;
        string? apiId = null;
        var clientIdArg = parseResult.GetValue(Opt<OptionalClientIdOption>.Instance);
        if (console.IsInteractive && clientIdArg is null)
        {
            apiId = await console.GetOrPromptForApiIdAsync(
                "For which API?", parseResult, apisClient, sessionService, cancellationToken);

            var selectedClient = await SelectClientPrompt
                .New(client, apiId)
                .Title("Select a client from the list below.")
                .RenderAsync(console, cancellationToken) ?? throw NoClientSelected();

            clientId = selectedClient.Id;
        }
        else
        {
            clientId = parseResult.GetRequiredOptionalValue(Opt<OptionalClientIdOption>.Instance);
        }

        var stageArg = parseResult.GetValue(Opt<OptionalStageNameOption>.Instance);
        if (string.IsNullOrEmpty(stageArg) && console.IsInteractive)
        {
            apiId ??= await client.GetClientApiIdAsync(clientId, cancellationToken)
                ?? throw new ExitException("The client was not found.");
        }

        var stage = await console.GetOrPromptForStageNameAsync(
            "Which stage?",
            parseResult,
            Opt<OptionalStageNameOption>.Instance,
            stagesClient,
            apiId ?? string.Empty,
            cancellationToken);

        var tags = parseResult.GetValue(Opt<ClientTagsToUnpublishOption>.Instance)?.ToArray() ?? [];
        if (tags.Length == 0)
        {
            if (!console.IsInteractive)
            {
                throw MissingRequiredOption(Opt<ClientTagsToUnpublishOption>.Instance.Name);
            }

            var tag = await console.PromptAsync("Which tag?", defaultValue: null, cancellationToken);
            tags = [tag];
        }

        await using var activity = console.StartActivity(
            $"Unpublishing client '{clientId.EscapeMarkup()}' from stage '{stage.EscapeMarkup()}'",
            "Failed to unpublish the client.");

        foreach (var tag in tags)
        {
            await using var unpublishActivity = activity.StartChildActivity(
                $"Unpublishing tag '{tag.EscapeMarkup()}'",
                "Failed to unpublish tag.");

            var result = await client.UnpublishClientVersionAsync(
                clientId,
                stage,
                tag,
                cancellationToken);

            if (result.Errors?.Count > 0)
            {
                await unpublishActivity.FailAllAsync();

                foreach (var error in result.Errors)
                {
                    var errorMessage = error switch
                    {
                        IConcurrentOperationError err => err.Message,
                        IStageNotFoundError err => err.Message,
                        IClientVersionNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IClientNotFoundError err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                throw new ExitException();
            }

            unpublishActivity.Success($"Unpublished tag '{tag.EscapeMarkup()}'.");
        }

        activity.Success($"Unpublished client '{clientId.EscapeMarkup()}' from stage '{stage.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
