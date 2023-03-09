using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Common extension of HostBuilder to run the GraphQL cli.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Extension method to run GraphQL commands on IHostBuilder interface with the provided
    /// arguments. The method starts a GraphQL server that listens for command-line arguments. You
    /// can use these arguments to invoke different tasks for CI / CD purposes, e.g. exporting the
    /// schema.
    /// </summary>
    /// <example>
    /// dotnet run -- schema export --output schema.graphql
    /// </example>
    /// <param name="builder">The IHostBuilder instance</param>
    /// <param name="args">Command line arguments to be passed to the host </param>
    public static async Task RunWithGraphQLCommandsAsync(
        this IHostBuilder builder,
        string[] args)
        => await builder.Build().RunWithGraphQLCommandsAsync(args);

    /// <summary>
    /// Extension method to run GraphQL commands on IHostBuilder interface with the provided
    /// arguments. The method starts a GraphQL server that listens for command-line arguments. You
    /// can use these arguments to invoke different tasks for CI / CD purposes, e.g. exporting the
    /// schema.
    /// </summary>
    /// <example>
    /// dotnet run -- schema export --output schema.graphql
    /// </example>
    /// <param name="builder">The IHostBuilder instance</param>
    /// <param name="args">Command line arguments to be passed to the host </param>
    public static void RunWithGraphQLCommands(
        this IHostBuilder builder,
        string[] args)
        => builder.Build().RunWithGraphQLCommands(args);

    /// <summary>
    /// Extension method to run GraphQL commands on IHostBuilder interface with the provided
    /// arguments. The method starts a GraphQL server that listens for command-line arguments. You
    /// can use these arguments to invoke different tasks for CI / CD purposes, e.g. exporting the
    /// schema.
    /// </summary>
    /// <example>
    /// dotnet run -- schema export --output schema.graphql
    /// </example>
    /// <param name="host">The IHost instance</param>
    /// <param name="args">Command line arguments to be passed to the host </param>
    public static async Task<int> RunWithGraphQLCommandsAsync(
        this IHost host,
        string[] args)
    {
        if (args.IsGraphQLCommand())
        {
            await new App(host).Build().InvokeAsync(args);
        }
        else
        {
            await host.RunAsync();
        }

        return 0;
    }

    /// <summary>
    /// Extension method to run GraphQL commands on IHostBuilder interface with the provided
    /// arguments. The method starts a GraphQL server that listens for command-line arguments. You
    /// can use these arguments to invoke different tasks for CI / CD purposes, e.g. exporting the
    /// schema.
    /// </summary>
    /// <example>
    /// dotnet run -- schema export --output schema.graphql
    /// </example>
    /// <param name="host">The IHost instance</param>
    /// <param name="args">Command line arguments to be passed to the host </param>
    public static void RunWithGraphQLCommands(
        this IHost host,
        string[] args)
    {
        if (args.IsGraphQLCommand())
        {
            new App(host).Build().Invoke(args);
        }
        else
        {
            host.Run();
        }
    }

    private static bool IsGraphQLCommand(this string[] args)
        => args is ["schema", ..];
}
