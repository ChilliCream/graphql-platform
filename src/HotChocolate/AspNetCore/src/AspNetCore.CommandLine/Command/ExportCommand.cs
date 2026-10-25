using System.CommandLine;
using HotChocolate.Execution;
using HotChocolate.Execution.Internal;
using HotChocolate.Serialization;
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

        var outputOption = new OutputOption();
        var schemaNameOption = new SchemaNameOption();
        var semanticNonNullOption = new SemanticNonNullOption();
        var specVersionOption = new SpecVersionOption();

        Options.Add(outputOption);
        Options.Add(schemaNameOption);
        Options.Add(semanticNonNullOption);
        Options.Add(specVersionOption);

        SetAction(
            (parseResult, cancellationToken) =>
            {
                var output = parseResult.InvocationConfiguration.Output;
                var outputFile = parseResult.GetValue(outputOption);
                var schemaName = parseResult.GetValue(schemaNameOption);
                var semanticNonNull = parseResult.GetValue(semanticNonNullOption);
                var specVersionValue = parseResult.GetValue(specVersionOption);
                GraphQLSpecVersion? specVersion = null;

                if (specVersionValue is not null)
                {
                    GraphQLSpecVersions.TryParse(specVersionValue, out var parsedSpecVersion);
                    specVersion = parsedSpecVersion;
                }

                return ExecuteAsync(
                    output,
                    host,
                    outputFile,
                    schemaName,
                    semanticNonNull,
                    specVersion,
                    cancellationToken);
            });
    }

    private static async Task ExecuteAsync(
        TextWriter output,
        IHost host,
        FileInfo? outputFile,
        string? schemaName,
        bool semanticNonNull,
        GraphQLSpecVersion? specVersion,
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
        var result = await SchemaFileExporter.Export(
            outputFile.FullName,
            executor,
            semanticNonNull,
            specVersion,
            cancellationToken);

        await output.WriteLineAsync("Exported Files:");
        await output.WriteLineAsync($"- {result.SchemaFileName}");
        await output.WriteLineAsync($"- {result.SettingsFileName}");
    }
}
