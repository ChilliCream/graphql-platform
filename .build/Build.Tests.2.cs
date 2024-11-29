using System.Linq;
using Colorful;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.ReportGenerator;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.ProjectModel.ProjectModelTasks;

partial class Build
{
    [Parameter] readonly bool EnableCoverage;

    Target TestCookieCrumble => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "CookieCrumble" / "CookieCrumble.sln"));

    Target TestGreenDonut => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "GreenDonut" / "GreenDonut.sln"));

    Target TestHotChocolateAnalyzers => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Analyzers" / "HotChocolate.Analyzers.sln"));

    Target TestHotChocolateApolloFederation => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "ApolloFederation" / "HotChocolate.ApolloFederation.sln"));

    Target TestHotChocolateAspNetCore => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "AspNetCore" / "HotChocolate.AspNetCore.sln"));

    Target TestHotChocolateAzureFunctions => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "AzureFunctions" / "HotChocolate.AzureFunctions.sln"));

    Target TestHotChocolateCodeGeneration => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "CodeGeneration" / "HotChocolate.CodeGeneration.sln"));

    Target TestHotChocolateCaching => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Caching" / "HotChocolate.Caching.sln"));

    Target TestHotChocolateCore => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Core" / "HotChocolate.Core.sln"));

    Target TestHotChocolateCostAnalysis => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "CostAnalysis" / "HotChocolate.CostAnalysis.sln"));

    Target TestHotChocolateData => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Data" / "HotChocolate.Data.sln"));

    Target TestHotChocolateDiagnostics => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Diagnostics" / "HotChocolate.Diagnostics.sln"));

    Target TestHotChocolateFusion => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Fusion" / "HotChocolate.Fusion.sln"));

    Target TestHotChocolateLanguage => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Language" / "HotChocolate.Language.sln"));

    Target TestHotChocolateMarten => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Marten" / "HotChocolate.Marten.sln"));

    Target TestHotChocolateMongoDb => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "MongoDb" / "HotChocolate.MongoDb.sln"));

    Target TestHotChocolateOpenApi => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "OpenApi" / "HotChocolate.OpenApi.sln"));

    Target TestHotChocolatePersistedOperations => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "PersistedOperations" / "HotChocolate.PersistedOperations.sln"));

    Target TestHotChocolateRaven => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Raven" / "HotChocolate.Raven.sln"));

    Target TestHotChocolateSkimmed => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Skimmed" / "HotChocolate.Skimmed.sln"));

    Target TestHotChocolateSpatial => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Spatial" / "HotChocolate.Spatial.sln"));

    Target TestHotChocolateUtilities => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Utilities" / "HotChocolate.Utilities.sln"));

    Target TestStrawberryShakeClient => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "StrawberryShake" / "Client" / "StrawberryShake.Client.sln"));

    Target TestStrawberryShakeCodeGeneration => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "StrawberryShake" / "CodeGeneration" / "StrawberryShake.CodeGeneration.sln"));

    Target TestStrawberryShakeTooling => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunClientTests(SourceDirectory / "StrawberryShake" / "Tooling" / "StrawberryShake.Tooling.sln"));

    void RunClientTests(AbsolutePath solutionFile)
    {
        RunTests(solutionFile);
    }

    void RunTests(AbsolutePath solutionFile)
    {
        var solutionDirectory = solutionFile.Parent!;
        var testDirectory = solutionDirectory / "test";

        DotNetBuild(c => c
            .SetProjectFile(solutionFile)
            .SetConfiguration(Debug));

        // we only select test projects that are located in the solutions test directory.
        // this will ensure that on build we do not execute referenced tests from other solutions.
        var testProjects = ParseSolution(solutionFile)
            .GetProjects("*.Tests")
            .Where(t => t.Path.ToString().StartsWith(testDirectory))
            .ToArray();

        Console.WriteLine("╬============================================");
        Console.WriteLine("║ Prepared Tests:");
        Console.WriteLine($"║ {RootDirectory.GetRelativePathTo(solutionDirectory)}:");

        foreach (var testProject in testProjects)
        {
            Console.WriteLine($"║ - {RootDirectory.GetRelativePathTo(testProject.Path.Parent!)}:");
        }
        Console.WriteLine("╬================================");

        try
        {
            if (EnableCoverage)
            {
                DotNetTest(c => c
                    .SetConfiguration(Debug)
                    .SetNoRestore(true)
                    .SetNoBuild(true)
                    .ResetVerbosity()
                    .SetResultsDirectory(TestResultDirectory)
                    .EnableCollectCoverage()
                    .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
                    .SetProcessArgumentConfigurator(a => a.Add("--collect:\"XPlat Code Coverage\""))
                    .SetExcludeByFile("*.Generated.cs")
                    .CombineWith(testProjects, (_, v) => _
                        .SetProjectFile(v)
                        .SetLoggers($"trx;LogFileName={v.Name}.trx")
                        .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml")));
            }
            else
            {
                DotNetTest(
                    c => c
                        .SetProjectFile(solutionFile)
                        .SetConfiguration(Debug)
                        .SetNoRestore(true)
                        .SetNoBuild(true)
                        .ResetVerbosity()
                        .SetResultsDirectory(TestResultDirectory)
                        .CombineWith(testProjects, (_, v) => _
                            .SetProjectFile(v)
                            .SetLoggers($"trx;LogFileName={v.Name}.trx")));
            }
        }
        finally
        {
            UploadTestsAndMismatches();
        }
    }
}
