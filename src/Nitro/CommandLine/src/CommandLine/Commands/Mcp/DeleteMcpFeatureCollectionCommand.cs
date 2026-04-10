using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class DeleteMcpFeatureCollectionCommand : Command
{
    public DeleteMcpFeatureCollectionCommand() : base("delete")
    {
        Description = "Delete an MCP feature collection.";

        Arguments.Add(Opt<OptionalIdArgument>.Instance);
        Options.Add(Opt<OptionalForceOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("mcp delete \"<mcp-feature-collection-id>\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var client = services.GetRequiredService<IMcpClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var mcpFeatureCollectionId = parseResult.GetValue(Opt<OptionalIdArgument>.Instance);

        if (mcpFeatureCollectionId is null)
        {
            if (!console.IsInteractive)
            {
                throw MissingRequiredOption("id");
            }

            const string apiMessage = "For which API do you want to delete an MCP Feature Collection?";
            const string mcpFeatureCollectionMessage = "Which MCP Feature Collection do you want to delete?";

            var workspaceId = parseResult.GetWorkspaceId(sessionService);

            var selectedApi = await SelectApiPrompt
                .New(apisClient, workspaceId)
                .Title(apiMessage)
                .RenderAsync(console, cancellationToken) ?? throw NoApiSelected();

            var apiId = selectedApi.Id;

            var selectedMcpFeatureCollection = await SelectMcpFeatureCollectionPrompt
                .New(client, apiId)
                .Title(mcpFeatureCollectionMessage)
                .RenderAsync(console, cancellationToken) ?? throw NoMcpFeatureCollectionSelected();

            mcpFeatureCollectionId = selectedMcpFeatureCollection.Id;
        }

        var force = parseResult.GetValue(Opt<OptionalForceOption>.Instance);
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you want to delete the MCP Feature Collection with the ID {mcpFeatureCollectionId}?"
                    .EscapeMarkup(),
                cancellationToken);

            if (!confirmed)
            {
                throw Exit("The MCP Feature Collection was not deleted.");
            }
        }

        await using (var activity = console.StartActivity(
            $"Deleting MCP feature collection '{mcpFeatureCollectionId.EscapeMarkup()}'",
            "Failed to delete the MCP feature collection."))
        {
            var data = await client.DeleteMcpFeatureCollectionAsync(
                mcpFeatureCollectionId,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IMcpFeatureCollectionNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IError err => Messages.UnexpectedMutationError(err),
                        _ => Messages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            if (data.McpFeatureCollection is not IMcpFeatureCollectionDetailPrompt_McpFeatureCollection detail)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Deleted MCP feature collection '{mcpFeatureCollectionId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(McpFeatureCollectionDetailPrompt.From(detail).ToObject()));

            return ExitCodes.Success;
        }
    }
}
