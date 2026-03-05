using System.CommandLine;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The print command can be used to print the schema to the console output.
/// </summary>
internal sealed class PrintCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrintCommand"/> class.
    /// </summary>
    public PrintCommand(IHost host) : base("print")
    {
        Description = "Prints the graphql schema to the console output";

        Options.Add(Opt<SchemaNameOption>.Instance);

        SetAction(
            (parseResult, cancellationToken) =>
            {
                var output = parseResult.InvocationConfiguration.Output;
                var schemaName = parseResult.GetValue(Opt<SchemaNameOption>.Instance);

                return ExecuteAsync(output, host, schemaName, cancellationToken);
            });
    }

    private static async Task ExecuteAsync(
        TextWriter output,
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
                await output.WriteLineAsync("No schemas registered.");
                return;
            }

            schemaName = schemaNames.Contains(ISchemaDefinition.DefaultName)
                ? ISchemaDefinition.DefaultName
                : schemaNames[0];
        }

        var executor = await provider.GetExecutorAsync(schemaName, cancellationToken);
        await output.WriteLineAsync(executor.Schema.ToString());
    }
}
