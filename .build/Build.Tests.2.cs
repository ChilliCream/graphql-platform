using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.ProjectModel.ProjectModelTasks;

partial class Build
{
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

    Target TestHotChocolateCore => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Core" / "HotChocolate.Core.sln"));

    Target TestHotChocolateData => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Data" / "HotChocolate.Data.sln"));

    Target TestHotChocolateDiagnostics => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Diagnostics" / "HotChocolate.Diagnostics.sln"));

    Target TestHotChocolateFilters => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Filters" / "HotChocolate.Filters.sln"));

    Target TestHotChocolateFusion => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Fusion" / "HotChocolate.Fusion.sln"));

    Target TestHotChocolateLanguage => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Language" / "HotChocolate.Language.sln"));

    Target TestHotChocolateMongoDb => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "MongoDb" / "HotChocolate.MongoDb.sln"));

    Target TestHotChocolateNeo4J => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Neo4J" / "HotChocolate.Neo4J.sln"));

    Target TestHotChocolatePersistedQueries => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "PersistedQueries" / "HotChocolate.PersistedQueries.sln"));

    Target TestHotChocolateSpatial => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Spatial" / "HotChocolate.Spatial.sln"));

    Target TestHotChocolateStitching => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Stitching" / "HotChocolate.Stitching.sln"));

    Target TestHotChocolateUtilities => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "HotChocolate" / "Utilities" / "HotChocolate.Utilities.sln"));

    Target TestStrawberryShakeClient => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "StrawberryShake" / "Client" / "StrawberryShake.Client.sln"));

    Target TestStrawberryShakeCodeGeneration => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunTests(SourceDirectory / "StrawberryShake" / "CodeGeneration" / "StrawberryShake.CodeGeneration.sln"));

    Target TestStrawberryShakeSourceGenerator => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunClientTests(SourceDirectory / "StrawberryShake" / "SourceGenerator" / "StrawberryShake.SourceGenerator.sln"));

    Target TestStrawberryShakeTooling => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() => RunClientTests(SourceDirectory / "StrawberryShake" / "Tooling" / "StrawberryShake.Tooling.sln"));

    void RunClientTests(AbsolutePath solutionFile)
    {
        BuildCodeGenServer(true);
        RunTests(solutionFile);
    }

    void RunTests(AbsolutePath solutionFile)
    {
        DotNetBuild(c => c
            .SetProjectFile(solutionFile)
            .SetConfiguration(Debug));

        var testProjects = ParseSolution(solutionFile).GetProjects("*.Tests");

        try
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
        finally
        {
            UploadTestsAndMismatches();
        }
    }
}
