using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Commands;

/// <summary>
/// The export command
/// </summary>
internal sealed class ExportCommand : Command
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExportCommand"/>.
    /// </summary>
    public ExportCommand() : base("export")
    {
        Description = "Export commands.";

        AddCommand(new ExportGraphCommand());
    }
}
