using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// A command line builder for the GraphQL server.
/// </summary>
internal sealed class App
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="App"/>.
    /// </summary>
    /// <param name="host">
    /// The host that is used to resolve services from the GraphQL Server.
    /// </param>
    public App(IHost host)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(host);
        serviceCollection.AddSingleton<ExportCommand>();
        serviceCollection.AddSingleton<ListCommand>();
        serviceCollection.AddSingleton<PrintCommand>();
        serviceCollection.AddSingleton<SchemaCommand>();
        serviceCollection.AddSingleton<GraphQLRootCommand>();
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public int Invoke(string args, TextWriter? output = null)
    {
        var rootCommand = _serviceProvider.GetRequiredService<GraphQLRootCommand>();
        var parseResult = rootCommand.Parse(args);
        var configuration = output is null ? null : new InvocationConfiguration { Output = output };

        return parseResult.Invoke(configuration);
    }

    public int Invoke(string[] args, TextWriter? output = null)
    {
        var rootCommand = _serviceProvider.GetRequiredService<GraphQLRootCommand>();
        var parseResult = rootCommand.Parse(args);
        var configuration = output is null ? null : new InvocationConfiguration { Output = output };

        return parseResult.Invoke(configuration);
    }

    public async Task<int> InvokeAsync(string[] args, TextWriter? output = null)
    {
        var rootCommand = _serviceProvider.GetRequiredService<GraphQLRootCommand>();
        var parseResult = rootCommand.Parse(args);
        var configuration = output is null ? null : new InvocationConfiguration { Output = output };

        return await parseResult.InvokeAsync(configuration);
    }

    public async Task<int> InvokeAsync(string args, TextWriter? output = null)
    {
        var rootCommand = _serviceProvider.GetRequiredService<GraphQLRootCommand>();
        var parseResult = rootCommand.Parse(args);
        var configuration = output is null ? null : new InvocationConfiguration { Output = output };

        return await parseResult.InvokeAsync(configuration);
    }
}
