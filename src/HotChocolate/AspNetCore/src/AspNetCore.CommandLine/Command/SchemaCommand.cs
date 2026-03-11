using System.CommandLine;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// The schema command.
/// </summary>
internal sealed class SchemaCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaCommand"/>.
    /// </summary>
    public SchemaCommand(
        ExportCommand exportCommand,
        ListCommand listCommand,
        PrintCommand printCommand) : base("schema")
    {
        Description = "Schema management commands.";

        Subcommands.Add(exportCommand);
        Subcommands.Add(listCommand);
        Subcommands.Add(printCommand);
    }
}
