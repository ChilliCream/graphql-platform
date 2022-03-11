using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Codecov;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static Helpers;
using static Nuke.Common.ProjectModel.ProjectModelTasks;

partial class Build
{
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

    void RunTests(AbsolutePath solutionFile)
    {
        DotNetBuild(c => c
            .SetProjectFile(solutionFile)
            .SetConfiguration(Debug));

        IEnumerable<Project> testProjects = ParseSolution(solutionFile).GetProjects("*.Tests");

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
