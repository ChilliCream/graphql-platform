using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using static Nuke.Common.Tools.Codecov.CodecovTasks;
using static Helpers;
using System;

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
        "HotChocolate.Data.Neo4J.Integration.Tests",
        "HotChocolate.CodeGeneration.Neo4J.Tests",
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
        .Partition(TestPartitionCount)
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);
            DotNetBuildTestSolution(TestSolutionFile, TestProjects);

            DotNetBuild(c => c
                .SetProjectFile(TestSolutionFile)
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
                if (DevOpsPipeLine is not null)
                {
                    TestResultDirectory.GlobFiles("*.trx").ForEach(x =>
                    DevOpsPipeLine?.PublishTestResults(
                        type: AzurePipelinesTestResultsType.VSTest,
                        title: $"{Path.GetFileNameWithoutExtension(x)} ({DevOpsPipeLine.StageDisplayName})",
                        files: new string[] { x }));

                    string uploadDir = Path.Combine(RootDirectory, "mismatch");

                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    foreach (string mismatchDir in Directory.GetDirectories(
                        RootDirectory, "__mismatch__", SearchOption.AllDirectories))
                    {
                        foreach (string snapshot in Directory.GetFiles(mismatchDir, "*.*"))
                        {
                            File.Copy(snapshot, Path.Combine(uploadDir, Path.GetFileName(snapshot)));
                        }
                    }

                    DevOpsPipeLine.UploadArtifacts("foo", "__mismatch__", uploadDir);
                }
            }
        });

    Target ReportCoverage => _ => _.DependsOn(Restore)
        .DependsOn(Cover)
        .Consumes(Cover)
        .Executes(() =>
        {
            try
            {
                ReportGenerator(_ => _
                    .SetReports(TestResultDirectory / "*.xml")
                    .SetReportTypes(ReportTypes.Cobertura, ReportTypes.HtmlInline_AzurePipelines)
                    .SetTargetDirectory(CoverageReportDirectory)
                    .SetAssemblyFilters("-*Tests"));
            }
            finally
            {
                if (DevOpsPipeLine is not null)
                {
                    string uploadDir = Path.Combine(RootDirectory, "mismatch");

                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    foreach (string mismatchDir in Directory.GetDirectories(
                        RootDirectory, "__mismatch__", SearchOption.AllDirectories))
                    {
                        foreach (string snapshot in Directory.GetFiles(mismatchDir, "*.*"))
                        {
                            File.Copy(snapshot, Path.Combine(uploadDir, Path.GetFileName(snapshot)));
                        }
                    }

                    DevOpsPipeLine.UploadArtifacts("foo", "__mismatch__", uploadDir);
                }

                var coverageFiles = Directory.GetFiles(
                TestResultDirectory,
                "*.xml",
                SearchOption.AllDirectories);

                Codecov(_ => _
                    .SetToken(CodeCovToken)
                    .SetFiles(coverageFiles)
                    .SetRepositoryRoot(RootDirectory)
                    .SetVerbose(true)
                    .SetFramework(Net50));
            }
        });

    Target ReportCodecov => _ => _.DependsOn(Restore)
        .DependsOn(Cover)
        .Consumes(Cover)
        .Executes(() =>
        {
            var coverageFiles = Directory.GetFiles(
                TestResultDirectory,
                "*.xml",
                SearchOption.AllDirectories);

            Codecov(_ => _
                .SetToken(CodeCovToken)
                .SetFiles(coverageFiles)
                .SetRepositoryRoot(RootDirectory)
                .SetVerbose(true)
                .SetFramework(Net50));
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
}
