using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class DeleteMcpFeatureCollectionCommand : Command
{
   public DeleteMcpFeatureCollectionCommand() : base("delete")
    {
        Description = "Deletes an MCP Feature Collection";

        AddOption(Opt<ForceOption>.Instance);
        AddArgument(Opt<OptionalIdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<OptionalIdArgument>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        string? mcpFeatureCollectionId,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Deleting an MCP Feature Collection");
        console.WriteLine();

        const string apiMessage = "For which api do you want to delete an MCP Feature Collection?";
        const string mcpFeatureCollectionMessage = "Which MCP Feature Collection do you want to delete?";

        if (mcpFeatureCollectionId is null)
        {
            if (!console.IsHumanReadable())
            {
                throw Exit("The MCP Feature Collection id is required in non-interactive mode.");
            }

            var workspaceId = context.RequireWorkspaceId();

            var selectedApi = await SelectApiPrompt
                .New(client, workspaceId)
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

        var shouldDelete = await context.ConfirmWhenNotForced(
            $"Do you want to delete the MCP Feature Collection with the id {mcpFeatureCollectionId}?"
                .EscapeMarkup(),
            cancellationToken);

        if (!shouldDelete)
        {
            console.OkLine("Aborted.");
            return ExitCodes.Success;
        }

        var input = new DeleteMcpFeatureCollectionByIdInput { McpFeatureCollectionId = mcpFeatureCollectionId };
        var result =
            await client.DeleteMcpFeatureCollectionByIdCommandMutation.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.DeleteMcpFeatureCollectionById.Errors);

        var deletedMcpFeatureCollection = data.DeleteMcpFeatureCollectionById.McpFeatureCollection;
        if (deletedMcpFeatureCollection is null)
        {
            throw Exit("Could not delete the MCP Feature Collection.");
        }

        console.OkLine($"MCP Feature Collection {deletedMcpFeatureCollection.Name.AsHighlight()} was deleted.");

        if (deletedMcpFeatureCollection is IMcpFeatureCollectionDetailPrompt_McpFeatureCollection detail)
        {
            context.SetResult(McpFeatureCollectionDetailPrompt.From(detail).ToObject([]));
        }

        return ExitCodes.Success;
    }
}
