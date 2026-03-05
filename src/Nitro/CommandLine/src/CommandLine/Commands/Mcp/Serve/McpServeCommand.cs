using System.CommandLine.Invocation;
using System.Net.Http.Headers;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Services;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Services;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.ProjectSettings;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve;

internal sealed class McpServeCommand : Command
{
    public McpServeCommand() : base("serve")
    {
        Description = "Start a local MCP server over stdio for AI coding assistants";

        AddOption(Opt<OptionalApiIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IHttpClientFactory>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        ISessionService sessionService,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // Write status to stderr only — stdout is reserved for JSON-RPC
        console.MarkupLineInterpolated($"[grey]Starting Nitro MCP server...[/]");

        // ProjectContext is loaded by the project settings middleware
        var projectContext = context.BindingContext.GetService<ProjectContext>();

        // Resolve API ID: flag > project settings > error
        var apiId = context.ParseResult.GetValueForOption(Opt<OptionalApiIdOption>.Instance);
        apiId ??= projectContext?.ActiveApi?.Id;

        if (apiId is null)
        {
            throw new ExitException("No API ID specified. Pass --api-id or set apiId in .nitro/settings.json");
        }

        // Ensure authenticated session
        var session =
            sessionService.Session
            ?? throw new ExitException($"Not authenticated. Run {"nitro login".AsCommand()} first.");

        var stage = projectContext?.ActiveApi?.DefaultStage ?? projectContext?.DefaultStage ?? "production";

        // Build the MCP server and run it over stdio
        var services = new ServiceCollection();

        services.AddSingleton(new NitroMcpContext(apiId, stage));
        services.AddSingleton(session);
        services.AddSingleton(sessionService);

        if (projectContext is not null)
        {
            services.AddSingleton(projectContext);
        }

        // Register HTTP client with auth for REST calls to the Nitro API
        services.AddHttpClient(
            "nitro-api",
            client =>
            {
                client.BaseAddress = new Uri($"https://{session.ApiUrl}/");
                if (session.Tokens?.AccessToken is { } token)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            });

        // Register the StrawberryShake GraphQL API client for validation tools
        services.AddHttpClient(
            ApiClient.ClientName,
            client =>
            {
                client.BaseAddress = new Uri($"https://{session.ApiUrl}/graphql");
                if (session.Tokens?.AccessToken is { } token)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add(Headers.GraphQLPreflight, "1");
            });
        services.AddApiClient();

        // Register shared services as singletons for DI
        services.AddSingleton<SchemaCache>();
        services.AddSingleton<SchemaSearchService>();
        services.AddSingleton<SchemaStatisticsService>();
        services.AddSingleton<NitroApiService>();

        // Register FusionInfoService for the get_fusion_info tool
        services.AddSingleton<FusionInfoService>();

        // Register ManagementService for API, API key, and client management tools
        services.AddSingleton<ManagementService>();

#pragma warning disable IL2026 // PublishAot is false; assembly scanning is safe here
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "nitro",
                    Version = typeof(McpServeCommand).Assembly.GetName().Version?.ToString() ?? "1.0.0"
                };
            })
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(McpServeCommand).Assembly)
            .WithPromptsFromAssembly(typeof(McpServeCommand).Assembly);
#pragma warning restore IL2026

        await using var serviceProvider = services.BuildServiceProvider();

        // Periodically refresh the access token for long-lived sessions
        _ = Task.Run(
            async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        await sessionService.LoadSessionAsync(cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OutOfMemoryException)
                    {
                        // Token refresh is best-effort; if it fails the MCP client
                        // will restart the server process.
                    }
                }
            },
            cancellationToken);

        var server = serviceProvider.GetRequiredService<McpServer>();
        await server.RunAsync(cancellationToken);

        return ExitCodes.Success;
    }
}
