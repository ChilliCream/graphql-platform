using System.CommandLine;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
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
        Description =
            "Export the graphql schema. If no output (--output) is specified the schema will be "
            + "printed to the console.";

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

            if(schemaNames.IsEmpty)
            {
                console.WriteLine("No schemas registered.");
                return;
            }

            schemaName = schemaNames.Contains(ISchemaDefinition.DefaultName)
                ? ISchemaDefinition.DefaultName
                : schemaNames[1];
        }

        var executor = await provider.GetExecutorAsync(schemaName, cancellationToken);

        var sdl = executor.Schema.ToString();

        if (output is not null)
        {
            await File.WriteAllTextAsync(
                output.FullName,
                sdl,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                cancellationToken);
        }
        else
        {
            console.WriteLine(sdl);
        }
    }
}
