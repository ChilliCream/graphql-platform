using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Options;

internal sealed class SubgraphPackageFileOption : Option<FileInfo?>
{
    public SubgraphPackageFileOption() : base("--package-file")
    {
        AddAlias("--package");
        AddAlias("-p");
    }
}
