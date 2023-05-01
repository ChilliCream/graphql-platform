using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Options;

internal sealed class SubgraphConfigFileOption : Option<FileInfo?>
{
    public SubgraphConfigFileOption() : base("--config-file")
    {
        Description = "The subgraph configuration file.";
        AddAlias("--config");
        AddAlias("-c");
}
}
