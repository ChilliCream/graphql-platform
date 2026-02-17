using System.CommandLine;
using HotChocolate.Execution;
using HotChocolate.Execution.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The export command can be used to export the schema to a file.
/// </summary>
internal sealed class ExportCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportCommand"/> class.
    /// </summary>
    public ExportCommand(IHost host) : base("export")
    {
        Description = "Export the graphql schema to a schema file";

        Options.Add(Opt<OutputOption>.Instance);
        Options.Add(Opt<SchemaNameOption>.Instance);

        SetAction(
            (parseResult, cancellationToken) =>
            {
                var output = parseResult.InvocationConfiguration.Output;
                var outputFile = parseResult.GetValue(Opt<OutputOption>.Instance);
                var schemaName = parseResult.GetValue(Opt<SchemaNameOption>.Instance);

                return ExecuteAsync(output, host, outputFile, schemaName, cancellationToken);
            });
    }

    private static async Task ExecuteAsync(
        TextWriter output,
        IHost host,
        FileInfo? outputFile,
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
        outputFile ??= new FileInfo(System.IO.Path.Combine(Environment.CurrentDirectory, "schema.graphqls"));
        var result = await SchemaFileExporter.Export(outputFile.FullName, executor, cancellationToken);

        await output.WriteLineAsync("Exported Files:");
        await output.WriteLineAsync($"- {result.SchemaFileName}");
        await output.WriteLineAsync($"- {result.SettingsFileName}");
    }
}
