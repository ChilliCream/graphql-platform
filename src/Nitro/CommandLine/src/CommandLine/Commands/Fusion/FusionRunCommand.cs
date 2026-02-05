using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using ChilliCream.Nitro.CommandLine.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

public class FusionRunCommand : Command
{
    public FusionRunCommand() : base("run")
    {
        base.Description = "Starts a Fusion gateway with the specified archive."
            + Environment.NewLine
            + "This command only supports Fusion v2.";

        var archiveArgument = new Argument<FileInfo>("ARCHIVE_FILE")
        {
            Description = "The path to the Fusion archive file"
        };
        archiveArgument.LegalFilePathsOnly();

        AddArgument(archiveArgument);

        var portOption = new Option<int>("--port");
        portOption.AddAlias("-p");

        AddOption(portOption);

        this.SetHandler(async context =>
        {
            var archiveFile = context.ParseResult.GetValueForArgument(archiveArgument)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            var port = context.ParseResult.GetValueForOption(portOption);

            await ExecuteAsync(archiveFile, console, port, context.GetCancellationToken());
        });
    }

    private static async Task ExecuteAsync(
        FileInfo archiveFile,
        IAnsiConsole console,
        int? port,
        CancellationToken cancellationToken)
    {
        if (!archiveFile.Exists)
        {
            throw new ExitException($"Archive file '{archiveFile.FullName}' does not exist.");
        }

        port ??= GetRandomUnusedPort();

        var host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(new UriBuilder("http", "localhost", port.Value).ToString())
            .ConfigureServices(services =>
            {
                services
                    .AddCors()
                    .AddHeaderPropagation(c =>
                    {
                        c.Headers.Add("GraphQL-Preflight");
                        c.Headers.Add("Authorization");
                    });

                services
                    .AddHttpClient("fusion")
                    .AddHeaderPropagation();

                services.AddRouting()
                    .AddGraphQLGatewayServer()
                    .AddFileSystemConfiguration(archiveFile.FullName)
                    .ModifyRequestOptions(o => o.CollectOperationPlanTelemetry = true);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseHeaderPropagation();
                app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
                app.UseEndpoints(e => e.MapGraphQL()
                    .WithOptions(new HotChocolate.AspNetCore.GraphQLServerOptions
                    {
                        Tool = { ServeMode = HotChocolate.AspNetCore.GraphQLToolServeMode.Insider }
                    }));
            })
            .Build();

        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStarted.Register(() =>
        {
            var graphqlUrl = $"http://localhost:{port}/graphql";

            console.Success($"Starting server at {graphqlUrl}");

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(graphqlUrl) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", graphqlUrl);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", graphqlUrl);
                }
                else
                {
                    throw new NotSupportedException("Unsupported OS platform");
                }
            }
            catch
            {
                // If we can't open in the default browser, we don't do anything.
            }
        });

        await host.RunAsync(cancellationToken);
    }

    private static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
