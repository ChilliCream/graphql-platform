using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
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
        console.WriteLine("Creating an MCP Feature Collection");
        console.WriteLine();

        const string apiMessage = "For which api do you want to create an MCP Feature Collection?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        var name = await context
            .OptionOrAskAsync("Name", Opt<McpFeatureCollectionNameOption>.Instance, cancellationToken);

        var input = new CreateMcpFeatureCollectionInput { Name = name, ApiId = apiId };
        var result =
            await client.CreateMcpFeatureCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CreateMcpFeatureCollection.Errors);

        var createdMcpFeatureCollection = data.CreateMcpFeatureCollection.McpFeatureCollection;
        if (createdMcpFeatureCollection is null)
        {
            throw Exit("Could not create MCP Feature Collection.");
        }

        console.OkLine($"MCP Feature Collection {createdMcpFeatureCollection.Name.AsHighlight()} created.");

        if (createdMcpFeatureCollection is IMcpFeatureCollectionDetailPrompt_McpFeatureCollection detail)
        {
            context.SetResult(McpFeatureCollectionDetailPrompt.From(detail).ToObject([]));
        }

        return ExitCodes.Success;
    }
}
