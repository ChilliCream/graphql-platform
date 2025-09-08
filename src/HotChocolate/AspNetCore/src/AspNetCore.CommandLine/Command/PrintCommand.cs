using System.CommandLine;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The print command can be used to print the schema to the console output
/// </summary>
internal sealed class PrintCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportCommand"/> class.
    /// </summary>
    public PrintCommand() : base("print")
    {
        Description = "Prints the graphql schema to the console output";

        AddOption(Opt<SchemaNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            Bind.FromServiceProvider<IHost>(),
            Opt<SchemaNameOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        IHost host,
        string? schemaName,
        CancellationToken cancellationToken)
    {
        var provider = host.Services.GetRequiredService<IRequestExecutorProvider>();

        if (schemaName is null)
        {
            var schemaNames = provider.SchemaNames;

            if (schemaNames.IsEmpty)
            {
                console.WriteLine("No schemas registered.");
                return;
            }

            schemaName = schemaNames.Contains(ISchemaDefinition.DefaultName)
                ? ISchemaDefinition.DefaultName
                : schemaNames[0];
        }

        var executor = await provider.GetExecutorAsync(schemaName, cancellationToken);
        console.WriteLine(executor.Schema.ToString());
    }
}
