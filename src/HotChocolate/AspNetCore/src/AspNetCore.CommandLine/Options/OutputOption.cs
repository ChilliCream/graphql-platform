using System.CommandLine;

namespace HotChocolate.AspNetCore.CommandLine;

/// <summary>
/// A option for the schema command. The option is used to specify the path to the file
/// </summary>
internal sealed class OutputOption : Option<FileInfo?>
{
    public OutputOption() : base("--output")
    {
        Description = "The path to the file where the schema should be exported to. If no " +
            "output path is specified the schema will be printed to the console.";
    }
}
