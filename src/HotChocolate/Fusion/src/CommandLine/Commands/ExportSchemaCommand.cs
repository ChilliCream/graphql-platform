using System.CommandLine;
using System.Text;
using HotChocolate.Fusion.CommandLine.Helpers;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.CommandLine.Commands;

internal sealed class ExportSchemaCommand : Command
{
    public ExportSchemaCommand() : base("schema")
    {
        var fusionPackageFile = new Option<FileInfo?>("--package-file");
        fusionPackageFile.AddAlias("--package");
        fusionPackageFile.AddAlias("-p");

        var schemaFile = new Option<FileInfo?>("--file");
        schemaFile.AddAlias("-f");

        AddOption(fusionPackageFile);
        AddOption(schemaFile);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IConsole>(),
            fusionPackageFile,
            schemaFile,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IConsole console,
        FileInfo? packageFile,
        FileInfo? schemaFile,
        CancellationToken cancellationToken)
    {
        packageFile ??=
            new FileInfo(System.IO.Path.Combine(Environment.CurrentDirectory, "gateway" + Extensions.FusionPackage));

        if (!packageFile.Exists)
        {
            if (Directory.Exists(packageFile.FullName))
            {
                packageFile =
                    new FileInfo(System.IO.Path.Combine(packageFile.FullName, "gateway" + Extensions.FusionPackage));
            }
            else if (!packageFile.Extension.EqualsOrdinal(Extensions.FusionPackage) &&
                     !packageFile.Extension.EqualsOrdinal(Extensions.ZipPackage))
            {
                packageFile = new FileInfo(packageFile.FullName + Extensions.FusionPackage);
            }

            if (!packageFile.Exists)
            {
                console.WriteLine($"The package file `{packageFile.FullName}` does not exist.");
                return 1;
            }
        }

        schemaFile ??= new FileInfo(System.IO.Path.Combine(packageFile.DirectoryName!, "schema.graphql"));

        await using var package = FusionGraphPackage.Open(packageFile.FullName);

        var schema = await package.GetSchemaAsync(cancellationToken);
        var options = new SyntaxSerializerOptions { Indented = true, MaxDirectivesPerLine = 0, };

        await File.WriteAllTextAsync(schemaFile.FullName, schema.ToString(options),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), cancellationToken);

        return 0;
    }
}
