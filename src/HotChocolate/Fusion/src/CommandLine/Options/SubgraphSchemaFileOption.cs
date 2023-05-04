using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Options;

internal sealed class SubgraphSchemaFileOption : Option<FileInfo?>
{
    public SubgraphSchemaFileOption() : base("--schema-file")
    {
        Description = "The subgraph schema file.";
        AddAlias("--schema");
        AddAlias("-s");
    }
}
