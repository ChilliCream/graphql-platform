using System.CommandLine;
using System.Text;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Utilities;
using static System.IO.Path;
using static HotChocolate.Fusion.CommandLine.Extensions;

namespace HotChocolate.Fusion.CommandLine.Commands;

internal sealed class ExportGraphCommand : Command
{
    public ExportGraphCommand() : base("graph")
    {
        var fusionPackageFile = new Option<FileInfo?>("--package-file");
        fusionPackageFile.AddAlias("--package");
        fusionPackageFile.AddAlias("-p");

        var graphFile = new Option<FileInfo?>("--file");
        graphFile.AddAlias("-f");
        
        AddOption(fusionPackageFile);
        AddOption(graphFile);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            fusionPackageFile,
            graphFile,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task ExecuteAsync(
        IConsole console,
        FileInfo? packageFile,
        FileInfo? graphFile,
        CancellationToken cancellationToken)
    {
        packageFile ??= new FileInfo(Combine(Environment.CurrentDirectory, "gateway" + FusionPackage));
        
        if (!packageFile.Exists)
        {
            if (Directory.Exists(packageFile.FullName))
            {
                packageFile = new FileInfo(Combine(packageFile.FullName, "gateway" + FusionPackage));   
            }
            else if (!packageFile.Extension.EqualsOrdinal(FusionPackage) &&
                !packageFile.Extension.EqualsOrdinal(ZipPackage))
            {
                packageFile = new FileInfo(packageFile.FullName + FusionPackage);   
            }

            if (!packageFile.Exists)
            {
                console.WriteLine($"The package file `{packageFile.FullName}` does not exist.");
                return;
            }
        }

        graphFile ??= new FileInfo(Combine(packageFile.DirectoryName!, "fusion.graphql"));
        
        await using var package = FusionGraphPackage.Open(packageFile.FullName);

        var graph = await package.GetFusionGraphAsync(cancellationToken);
        var options = new SyntaxSerializerOptions { Indented = true, MaxDirectivesPerLine = 0, };
        
        await File.WriteAllTextAsync(graphFile.FullName, graph.ToString(options), Encoding.UTF8, cancellationToken);
    }
}