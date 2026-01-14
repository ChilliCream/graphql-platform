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
    public ExportCommand() : base("export")
    {
        Description = "Export the graphql schema to a schema file";

        AddOption(Opt<OutputOption>.Instance);
        AddOption(Opt<SchemaNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            Bind.FromServiceProvider<IHost>(),
            Opt<OutputOption>.Instance,
            Opt<SchemaNameOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        IHost host,
        FileInfo? output,
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
        output ??= new FileInfo(System.IO.Path.Combine(Environment.CurrentDirectory, "schema.graphqls"));
        var result = await SchemaFileExporter.Export(output.FullName, executor, cancellationToken);

        // ReSharper disable LocalizableElement
        console.WriteLine("Exported Files:");
        console.WriteLine($"- {result.SchemaFileName}");
        console.WriteLine($"- {result.SettingsFileName}");
        // ReSharper restore LocalizableElement
    }
}
