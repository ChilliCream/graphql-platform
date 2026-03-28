using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class CreateMcpFeatureCollectionCommand : Command
{
    public CreateMcpFeatureCollectionCommand() : base("create")
    {
        Description = "Creates a new MCP Feature Collection";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<McpFeatureCollectionNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IMcpClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IMcpClient client,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Creating an MCP Feature Collection");
        console.WriteLine();

        const string apiMessage = "For which API do you want to create an MCP Feature Collection?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var name = await context
            .OptionOrAskAsync("Name", Opt<McpFeatureCollectionNameOption>.Instance, cancellationToken);

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
        context.SetResult(McpFeatureCollectionDetailPrompt.From(detail).ToObject());

        return ExitCodes.Success;
    }
}
