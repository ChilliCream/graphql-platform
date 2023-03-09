using System.CommandLine;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// The schema command
/// </summary>
internal sealed class SchemaCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaCommand"/>.
    /// </summary>
    public SchemaCommand() : base("schema")
    {
        Description = "Schema management commands.";

        AddCommand(new ExportCommand());
    }
}
