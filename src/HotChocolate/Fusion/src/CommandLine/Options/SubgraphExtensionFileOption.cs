using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Options;

internal sealed class SubgraphExtensionFileOption : Option<List<FileInfo>?>
{
    public SubgraphExtensionFileOption() : base("--extension-file")
    {
        Description = "The subgraph schema extension files.";
        AddAlias("--extension");
        AddAlias("-e");
    }
}
