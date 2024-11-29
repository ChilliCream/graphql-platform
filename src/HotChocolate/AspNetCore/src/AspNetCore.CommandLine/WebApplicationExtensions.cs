using System.CommandLine.Parsing;
using HotChocolate.AspNetCore.CommandLine;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Common extension of HostBuilder to run the GraphQL cli.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Extension method to run GraphQL commands on <see cref="IHostBuilder"/> with the provided
    /// arguments. The method either starts the server or executes the cli based on the provided
    /// arguments. You can use these arguments to invoke different tasks for CI / CD purposes,
    /// e.g. exporting the schema.
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
    /// Extension method to run GraphQL commands on <see cref="IHostBuilder"/> with the provided
    /// arguments. The method either starts the server or executes the cli based on the provided
    /// arguments. You can use these arguments to invoke different tasks for CI / CD purposes,
    /// e.g. exporting the schema.
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
    /// Extension method to run GraphQL commands on <see cref="IHost"/> with the provided
    /// arguments. The method either starts the server or executes the cli based on the provided
    /// arguments. You can use these arguments to invoke different tasks for CI / CD purposes,
    /// e.g. exporting the schema.
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
    /// Extension method to run GraphQL commands on <see cref="IHost"/> with the provided
    /// arguments. The method either starts the server or executes the cli based on the provided
    /// arguments. You can use these arguments to invoke different tasks for CI / CD purposes,
    /// e.g. exporting the schema.
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
        => args is ["schema", ..,];
}
