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
    public CreateMcpFeatureCollectionCommand() : base("create")
    {
        Description = "Create a new MCP feature collection.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<McpFeatureCollectionNameOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            mcp create \
              --name "my-collection" \
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
        var apisClient = services.GetRequiredService<IApisClient>();
        var client = services.GetRequiredService<IMcpClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        const string apiMessage = "For which API do you want to create an MCP Feature Collection?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, cancellationToken);

        var name = await console
            .PromptAsync("Name", defaultValue: null, parseResult, Opt<McpFeatureCollectionNameOption>.Instance, cancellationToken);

        await using (var activity = console.StartActivity(
            $"Creating MCP feature collection '{name.EscapeMarkup()}' for API '{apiId.EscapeMarkup()}'",
            "Failed to create the MCP feature collection."))
        {
            var data = await client.CreateMcpFeatureCollectionAsync(
                apiId,
                name,
                cancellationToken);

            if (data.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in data.Errors)
                {
                    var errorMessage = error switch
                    {
                        IApiNotFoundError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                    return ExitCodes.Error;
                }
            }

            if (data.McpFeatureCollection is not IMcpFeatureCollectionDetailPrompt_McpFeatureCollection detail)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Created MCP feature collection '{name.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(McpFeatureCollectionDetailPrompt.From(detail).ToObject()));

            return ExitCodes.Success;
        }
    }
}
