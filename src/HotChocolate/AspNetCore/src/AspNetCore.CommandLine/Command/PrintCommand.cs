using System.CommandLine;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The print command can be used to print the schema to the console output.
/// </summary>
internal sealed class PrintCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrintCommand"/> class.
    /// </summary>
    public PrintCommand(IServiceProvider services) : base("print")
    {
        Description = "Prints the graphql schema to the console output";

        var schemaNameOption = new SchemaNameOption();

        Options.Add(schemaNameOption);

        SetAction(
            (parseResult, cancellationToken) =>
            {
                var output = parseResult.InvocationConfiguration.Output;
                var schemaName = parseResult.GetValue(schemaNameOption);

                return ExecuteAsync(output, services, schemaName, cancellationToken);
            });
    }

    private static async Task ExecuteAsync(
        TextWriter output,
        IServiceProvider services,
        string? schemaName,
        CancellationToken cancellationToken)
    {
        var provider = services.GetRequiredService<IRequestExecutorProvider>();

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
