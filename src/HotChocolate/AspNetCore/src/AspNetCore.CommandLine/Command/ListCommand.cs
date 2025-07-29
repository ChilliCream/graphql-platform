using System.CommandLine;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The export command can be used to export the schema to a file.
/// </summary>
internal sealed class ListCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportCommand"/> class.
    /// </summary>
    public ListCommand() : base("list")
    {
        Description = "List all registered GraphQL schemas.";

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            Bind.FromServiceProvider<IHost>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static Task ExecuteAsync(
        IConsole console,
        IHost host,
        CancellationToken cancellationToken)
    {
        var schemaNames = host.Services.GetRequiredService<IRequestExecutorProvider>().SchemaNames;

        if (schemaNames.IsEmpty)
        {
            console.WriteLine("No schemas registered.");
            return Task.CompletedTask;
        }

        foreach (var name in schemaNames)
        {
            console.WriteLine(name);
        }

        return Task.CompletedTask;
    }
}
