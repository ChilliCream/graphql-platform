using System.CommandLine;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The list command can be used to list all registered schemas.
/// </summary>
internal sealed class ListCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListCommand"/> class.
    /// </summary>
    public ListCommand(IServiceProvider services) : base("list")
    {
        Description = "List all registered GraphQL schemas.";

        SetAction(parseResult => ExecuteAsync(parseResult.InvocationConfiguration.Output, services));
    }

    private static async Task ExecuteAsync(TextWriter output, IServiceProvider services)
    {
        var schemaNames = services.GetRequiredService<IRequestExecutorProvider>().SchemaNames;

        if (schemaNames.IsEmpty)
        {
            await output.WriteLineAsync("No schemas registered.");
            return;
        }

        foreach (var name in schemaNames)
        {
            await output.WriteLineAsync(name);
        }
    }
}
