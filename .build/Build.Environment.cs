using Nuke.Common.IO;

partial class Build
{
    const string Debug = "Debug";
    const string Release = "Release";
    const string Net50 = "net5.0";
    const string Net60 = "net6.0";

    readonly int DegreeOfParallelism = 2;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath AllSolutionFile => SourceDirectory / "All.sln";
    AbsolutePath TestSolutionFile => TemporaryDirectory / "Build.Test.sln";
    AbsolutePath PackSolutionFile => SourceDirectory / "Build.Pack.sln";

    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";
    AbsolutePath CoverageReportDirectory => OutputDirectory / "coverage-reports";
    AbsolutePath PackageDirectory => OutputDirectory / "packages";
    AbsolutePath HotChocolateDirectoryBuildProps => SourceDirectory / "HotChocolate" / "Directory.Build.Props";
    AbsolutePath TemplatesNuSpec => RootDirectory / "templates" / "HotChocolate.Templates.nuspec";
    AbsolutePath EmptyServer12Proj => RootDirectory / "templates" / "server" / "HotChocolate.Template.Server.csproj";
    AbsolutePath EmptyAzf12Proj => RootDirectory / "templates" / "azure-function" / "HotChocolate.Template.AzureFunctions.csproj";
    AbsolutePath Gateway13Proj => RootDirectory / "templates" / "gateway" / "HotChocolate.Template.Gateway.csproj";
    AbsolutePath GatewayAspire13Proj => RootDirectory / "templates" / "gateway-aspire" / "HotChocolate.Template.Gateway.Aspire.csproj";
    AbsolutePath GatewayManaged13Proj => RootDirectory / "templates" / "gateway-managed" / "HotChocolate.Template.Gateway.Managed.csproj";
}
