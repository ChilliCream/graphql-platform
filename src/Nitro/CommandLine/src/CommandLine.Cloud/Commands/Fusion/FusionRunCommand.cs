using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

public class FusionRunCommand : Command
{
    public FusionRunCommand() : base("run")
    {
        base.Description = "Starts a Fusion gateway with the specified configuration";

        var archiveOption = new Option<FileInfo>("--fusion-archive")
        {
            Description = "The path to the Fusion configuration file",
            IsRequired = true
        };
        archiveOption.AddAlias("--far");
        archiveOption.AddAlias("-f");
        archiveOption.LegalFilePathsOnly();

        AddOption(archiveOption);

        this.SetHandler(async context =>
        {
            var archiveFile = context.ParseResult.GetValueForOption(archiveOption)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            await ExecuteAsync(archiveFile, console, context.GetCancellationToken());
        });
    }

    private static async Task ExecuteAsync(
        FileInfo archiveFile,
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        if (!archiveFile.Exists)
        {
            throw new ExitException($"Archive file '{archiveFile.FullName}' does not exist.");
        }

        var port = GetRandomUnusedPort();

        var host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(new UriBuilder("http", "localhost", port).ToString())
            .ConfigureServices(services =>
            {
                services.AddRouting()
                    .AddGraphQLGatewayServer()
                    .AddFileSystemConfiguration(archiveFile.FullName);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(e => e.MapGraphQL());
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
