using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static Helpers;

partial class Build : NukeBuild
{
    readonly HashSet<string> ExcludedTests = new()
    {
        "HotChocolate.Types.Selections.PostgreSql.Tests"
    };

    [Partition(4)] readonly Partition TestPartition;

    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(
        ProjectModelTasks.ParseSolution(AllSolutionFile).GetProjects("*.Tests")
                .Where((t => !ExcludedTests.Contains(t.Name))));

    Target Test => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);
            DotNetBuild(c => c
                .SetProjectFile(AllSolutionFile)
                .SetConfiguration(Debug));

            try
            {
                DotNetTest(
                    TestSettings,
                    degreeOfParallelism: DegreeOfParallelism,
                    completeOnFailure: true);
            }
            finally
            {
                TestResultDirectory.GlobFiles("*.trx").ForEach(x =>
                    DevOpsPipeLine?.PublishTestResults(
                        type: AzurePipelinesTestResultsType.VSTest,
                        title: $"{Path.GetFileNameWithoutExtension(x)} ({DevOpsPipeLine.StageDisplayName})",
                        files: new string[] { x }));
            }
        });

    Target Cover => _ => _.DependsOn(Compile)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Partition(() => TestPartition)
        .Executes(() =>
        {
            DotNetTest(
                CoverSettings,
                degreeOfParallelism: DegreeOfParallelism,
                completeOnFailure: true);

            TestResultDirectory.GlobFiles("*.trx").ForEach(x =>
                DevOpsPipeLine?.PublishTestResults(
                    type: AzurePipelinesTestResultsType.VSTest,
                    title: $"{Path.GetFileNameWithoutExtension(x)} ({DevOpsPipeLine.StageDisplayName})",
                    files: new string[] { x }));
        });

    Target ReportCoverage => _ => _.DependsOn(Restore)
        .DependsOn(Cover)
        .Consumes(Cover)
        .Executes(() =>
        {
            ReportGenerator(_ => _
                .SetReports(TestResultDirectory / "*.xml")
                .SetReportTypes(ReportTypes.Cobertura, ReportTypes.HtmlInline_AzurePipelines)
                .SetTargetDirectory(CoverageReportDirectory)
                .SetAssemblyFilters("-*Tests"));

            if (DevOpsPipeLine is { })
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
                .SetLogger($"trx;LogFileName={v.Name}.trx"));

    IEnumerable<DotNetTestSettings> CoverNoBuildSettingsOnly50(DotNetTestSettings settings) =>
        TestBaseSettings(settings)
            .SetNoBuild(true)
            .EnableCollectCoverage()
            .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
            .SetExcludeByFile("*.Generated.cs")
            .SetFramework("net5.0")
            .CombineWith(TestProjects, (_, v) => _
                .SetProjectFile(v)
                .SetLogger($"trx;LogFileName={v.Name}.trx")
                .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"));

    IEnumerable<DotNetTestSettings> CoverNoBuildSettings(DotNetTestSettings settings) =>
        TestBaseSettings(settings)
            .SetNoBuild(true)
            .EnableCollectCoverage()
            .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
            .SetExcludeByFile("*.Generated.cs")
            .CombineWith(TestProjects, (_, v) => _
                .SetProjectFile(v)
                .SetLogger($"trx;LogFileName={v.Name}.trx")
                .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"));

    IEnumerable<DotNetTestSettings> CoverSettings(DotNetTestSettings settings) =>
        TestBaseSettings(settings)
            .EnableCollectCoverage()
            .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
            .SetExcludeByFile("*.Generated.cs")
            .CombineWith(TestProjects, (_, v) => _
                .SetProjectFile(v)
                .SetLogger($"trx;LogFileName={v.Name}.trx")
                .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"));

    DotNetTestSettings TestBaseSettings(DotNetTestSettings settings) =>
        settings
            .SetConfiguration(Debug)
            .SetNoRestore(true)
            .SetNoBuild(true)
            .ResetVerbosity()
            .SetResultsDirectory(TestResultDirectory);
}

