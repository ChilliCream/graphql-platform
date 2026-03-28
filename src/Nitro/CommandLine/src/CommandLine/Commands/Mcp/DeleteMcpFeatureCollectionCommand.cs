using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class DeleteMcpFeatureCollectionCommand : Command
{
    public DeleteMcpFeatureCollectionCommand(
        INitroConsole console,
        IApisClient apisClient,
        IMcpClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("delete")
    {
        Description = "Deletes an MCP Feature Collection";

        Options.Add(Opt<ForceOption>.Instance);
        Arguments.Add(Opt<OptionalIdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, apisClient, client, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IMcpClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Deleting an MCP Feature Collection");
        console.WriteLine();

        const string apiMessage = "For which API do you want to delete an MCP Feature Collection?";
        const string mcpFeatureCollectionMessage = "Which MCP Feature Collection do you want to delete?";

        var mcpFeatureCollectionId = parseResult.GetValue(Opt<OptionalIdArgument>.Instance);

        if (mcpFeatureCollectionId is null)
        {
            if (!console.IsInteractive)
            {
                throw Exit("The MCP Feature Collection ID is required in non-interactive mode.");
            }

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

            console.WriteLine("Selected MCP Feature Collection: " + selectedMcpFeatureCollection.Name);

            mcpFeatureCollectionId = selectedMcpFeatureCollection.Id;
            console.OkQuestion(mcpFeatureCollectionMessage, mcpFeatureCollectionId);
        }
        else
        {
            console.OkQuestion(mcpFeatureCollectionMessage, mcpFeatureCollectionId);
        }

        // TODO: Fix
        var force = parseResult.GetValue(Opt<ForceOption>.Instance); // is not null;
        if (!force)
        {
            var confirmed = await console.ConfirmAsync(
                $"Do you want to delete the MCP Feature Collection with the ID {mcpFeatureCollectionId}?"
                    .EscapeMarkup(),
                cancellationToken);

            if (!confirmed)
            {
                console.OkLine("Aborted.");
                return ExitCodes.Success;
            }
        }

        var deletedMcpFeatureCollection = await client.DeleteMcpFeatureCollectionAsync(
            mcpFeatureCollectionId,
            cancellationToken);
        console.PrintMutationErrorsAndExit(deletedMcpFeatureCollection.Errors);

        if (deletedMcpFeatureCollection.McpFeatureCollection is not IMcpFeatureCollectionDetailPrompt_McpFeatureCollection detail)
        {
            throw Exit("Could not delete the MCP Feature Collection.");
        }

        console.OkLine($"MCP Feature Collection {detail.Name.AsHighlight()} was deleted.");
        resultHolder.SetResult(new ObjectResult(McpFeatureCollectionDetailPrompt.From(detail).ToObject()));

        return ExitCodes.Success;
    }
}
