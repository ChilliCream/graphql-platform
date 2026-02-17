using System.CommandLine;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The list command can be used to list all registered schemas.
/// </summary>
internal sealed class ListCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListCommand"/> class.
    /// </summary>
    public ListCommand(IHost host) : base("list")
    {
        Description = "List all registered GraphQL schemas.";

        SetAction(parseResult => ExecuteAsync(parseResult.InvocationConfiguration.Output, host));
    }

    private static async Task ExecuteAsync(TextWriter output, IHost host)
    {
        var schemaNames = host.Services.GetRequiredService<IRequestExecutorProvider>().SchemaNames;

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
