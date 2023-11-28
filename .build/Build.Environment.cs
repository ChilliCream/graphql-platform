using Nuke.Common.IO;

partial class Build
{
    const string Debug = "Debug";
    const string Release = "Release";
    const string Net50 = "net5.0";
    const string Net60 = "net6.0";
    const string Net70 = "net7.0";

    readonly int DegreeOfParallelism = 2;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath AllSolutionFile => SourceDirectory / "All.sln";
    AbsolutePath TestSolutionFile => TemporaryDirectory / "Build.Test.sln";
    AbsolutePath PackSolutionFile => SourceDirectory / "Build.Pack.sln";

    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";
    AbsolutePath CoverageReportDirectory => OutputDirectory / "coberage-reports";
    AbsolutePath PackageDirectory => OutputDirectory / "packages";
    AbsolutePath StarWarsTemplateNuSpec => RootDirectory / "templates" / "StarWars" / "HotChocolate.Templates.StarWars.nuspec";
    AbsolutePath HotChocolateDirectoryBuildProps => SourceDirectory / "HotChocolate" / "Directory.Build.Props";
    AbsolutePath StarWarsProj => RootDirectory / "templates" / "StarWars" / "content" / "StarWars.csproj";
    AbsolutePath EmptyServerTemplateNuSpec => RootDirectory / "templates" / "Server" / "HotChocolate.Templates.Server.nuspec";
    AbsolutePath EmptyServerProj => RootDirectory / "templates" / "Server" / "content" / "HotChocolate.Server.Template.csproj";
    AbsolutePath TemplatesNuSpec => RootDirectory / "templates" / "v12" / "HotChocolate.Templates.nuspec";
    AbsolutePath EmptyServer12Proj => RootDirectory / "templates" / "v12" / "server" / "HotChocolate.Template.Server.csproj";
    AbsolutePath EmptyAzf12Proj => RootDirectory / "templates" / "v12" / "function" / "HotChocolate.Template.AzureFunctions.csproj";
    AbsolutePath EmptyAzfUp12Proj => RootDirectory / "templates" / "v12" / "function-isolated" / "HotChocolate.Template.AzureFunctions.Isolated.csproj";
    AbsolutePath Gateway13Proj => RootDirectory / "templates" / "v12" / "gateway" / "HotChocolate.Template.Gateway.csproj";
    AbsolutePath GatewayManaged13Proj => RootDirectory / "templates" / "v12" / "gateway-bcp" / "HotChocolate.Template.Gateway.Managed.csproj";
    AbsolutePath FSharpTypes => SourceDirectory/"HotChocolate" /"Core" / "src" / "Types.FSharp" / "HotChocolate.Types.FSharp.fsproj";
}
