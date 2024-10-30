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

partial class Build
{
    readonly HashSet<string> ExcludedTests = new()
    {
        "HotChocolate.Types.Selections.PostgreSql.Tests"
    };

    readonly HashSet<string> ExcludedCover = new()
    {
        "HotChocolate.Types.Selections.PostgreSql.Tests",
        "HotChocolate.Configuration.Analyzers.Tests",
        "HotChocolate.Analyzers.Tests",
        "dotnet-graphql",
        "CodeGeneration.CSharp.Analyzers",
        "CodeGeneration.CSharp.Analyzers.Tests"
    };

    const int TestPartitionCount = 4;

    [Partition(TestPartitionCount)] readonly Partition TestPartition;

    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(
        ProjectModelTasks.ParseSolution(AllSolutionFile).GetProjects("*.Tests")
                .Where(t => !ExcludedTests.Contains(t.Name)));

    IEnumerable<Project> CoverProjects => TestPartition.GetCurrent(
        ProjectModelTasks.ParseSolution(AllSolutionFile).GetProjects("*.Tests")
                .Where(t => !ExcludedCover.Contains(t.Name)));

    Target Test => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .DependsOn(
            TestCookieCrumble,
            TestGreenDonut,
            TestHotChocolateAnalyzers,
            TestHotChocolateApolloFederation,
            TestHotChocolateAspNetCore,
            TestHotChocolateAzureFunctions,
            TestHotChocolateCodeGeneration,
            TestHotChocolateCaching,
            TestHotChocolateCore,
            TestHotChocolateData,
            TestHotChocolateDiagnostics,
            TestHotChocolateFusion,
            TestHotChocolateLanguage,
            TestHotChocolateMarten,
            TestHotChocolateMongoDb,
            TestHotChocolatePersistedOperations,
            TestHotChocolateRaven,
            TestHotChocolateSkimmed,
            TestHotChocolateSpatial,
            TestHotChocolateUtilities,
            TestStrawberryShakeClient,
            TestStrawberryShakeCodeGeneration,
            TestStrawberryShakeTooling);

    Target Cover => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Partition(TestPartitionCount)
        .Executes(() =>
        {
            try
            {
                DotNetBuildSonarSolution(AllSolutionFile);
                DotNetBuildTestSolution(TestSolutionFile, CoverProjects);

                DotNetBuild(c => c
                    .SetProjectFile(TestSolutionFile)
                    .SetConfiguration(Debug));

                DotNetTest(
                    CoverSettings,
                    degreeOfParallelism: DegreeOfParallelism,
                    completeOnFailure: true);
            }
            finally
            {
                UploadTestsAndMismatches();
            }
        });

    Target ReportCoverage => _ => _
        .DependsOn(Restore)
        .DependsOn(Cover)
        .Consumes(Cover)
        .Executes(() =>
        {
            ReportGenerator(_ => _
                .SetReports(TestResultDirectory / "*.xml")
                .SetReportTypes(ReportTypes.Cobertura, ReportTypes.HtmlInline_AzurePipelines)
                .SetTargetDirectory(CoverageReportDirectory)
                .SetAssemblyFilters("-*Tests"));

            if (DevOpsPipeLine is not null)
            {
                CoverageReportDirectory.GlobFiles("*.xml").ForEach(x =>
                    DevOpsPipeLine.PublishCodeCoverage(
                        AzurePipelinesCodeCoverageToolType.Cobertura,
                        x,
                        CoverageReportDirectory,
                        Directory.GetFiles(CoverageReportDirectory, "*.htm")));
            }
        });

    IEnumerable<DotNetTestSettings> TestSettings(DotNetTestSettings settings) =>
        TestBaseSettings(settings)
            .CombineWith(TestProjects, (_, v) => _
                .SetProjectFile(v)
                .SetLoggers($"trx;LogFileName={v.Name}.trx"));

    IEnumerable<DotNetTestSettings> CoverNoBuildSettingsOnlyNet60(
        DotNetTestSettings settings,
        IEnumerable<Project> projects) =>
        TestBaseSettings(settings)
            .EnableCollectCoverage()
            .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
            .SetProcessArgumentConfigurator(a => a.Add("--collect:\"XPlat Code Coverage\""))
            .SetExcludeByFile("*.Generated.cs")
            .SetFramework(Net60)
            .CombineWith(projects, (_, v) => _
                .SetProjectFile(v)
                .SetLoggers($"trx;LogFileName={v.Name}.trx")
                .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"));

    IEnumerable<DotNetTestSettings> CoverSettings(DotNetTestSettings settings) =>
        TestBaseSettings(settings)
            .EnableCollectCoverage()
            .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
            .SetProcessArgumentConfigurator(a => a.Add("--collect:\"XPlat Code Coverage\""))
            .SetExcludeByFile("*.Generated.cs")
            .CombineWith(TestProjects, (_, v) => _
                .SetProjectFile(v)
                .SetLoggers($"trx;LogFileName={v.Name}.trx")
                .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"));

    DotNetTestSettings TestBaseSettings(DotNetTestSettings settings) =>
        settings
            .SetConfiguration(Debug)
            .SetNoRestore(true)
            .SetNoBuild(true)
            .ResetVerbosity()
            .SetResultsDirectory(TestResultDirectory);

    void UploadTestsAndMismatches()
    {
        if (DevOpsPipeLine is not null)
        {
            TestResultDirectory.GlobFiles("*.trx")
                .ForEach(x =>
                    DevOpsPipeLine.PublishTestResults(
                        type: AzurePipelinesTestResultsType.VSTest,
                        title: $"{Path.GetFileNameWithoutExtension(x)} ({DevOpsPipeLine.StageDisplayName})",
                        files: new string[] { x }));

            var uploadDir = Path.Combine(RootDirectory, "mismatch");

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            foreach (var mismatchDir in Directory.GetDirectories(
                RootDirectory, "__mismatch__", SearchOption.AllDirectories))
            {
                foreach (var snapshot in Directory.GetFiles(mismatchDir, "*.*"))
                {
                    File.Copy(snapshot, Path.Combine(uploadDir, Path.GetFileName(snapshot)));
                }
            }

            DevOpsPipeLine.UploadArtifacts("foo", "__mismatch__", uploadDir);
        }
    }
}
