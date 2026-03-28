using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class CreateMcpFeatureCollectionCommand : Command
{
    public CreateMcpFeatureCollectionCommand(
        INitroConsole console,
        IApisClient apisClient,
        IMcpClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("create")
    {
        Description = "Creates a new MCP Feature Collection";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<McpFeatureCollectionNameOption>.Instance);

        SetAction(async (parseResult, cancellationToken)
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
        console.WriteLine("Creating an MCP Feature Collection");
        console.WriteLine();

        const string apiMessage = "For which API do you want to create an MCP Feature Collection?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, cancellationToken);

        var name = await console
            .PromptAsync("Name", defaultValue: null, parseResult, Opt<McpFeatureCollectionNameOption>.Instance, cancellationToken);

        var createdMcpFeatureCollection = await client.CreateMcpFeatureCollectionAsync(
            apiId,
            name,
            cancellationToken);
        console.PrintMutationErrorsAndExit(createdMcpFeatureCollection.Errors);

        if (createdMcpFeatureCollection.McpFeatureCollection is not IMcpFeatureCollectionDetailPrompt_McpFeatureCollection detail)
        {
            throw Exit("Could not create MCP Feature Collection.");
        }

        console.OkLine($"MCP Feature Collection {detail.Name.AsHighlight()} created.");
        resultHolder.SetResult(new ObjectResult(McpFeatureCollectionDetailPrompt.From(detail).ToObject()));

        return ExitCodes.Success;
    }
}
