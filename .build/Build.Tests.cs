using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotCover;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.InspectCode;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Tools.Slack;
using Nuke.Common.Tools.SonarScanner;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.Common.ControlFlow;
using static Nuke.Common.Gitter.GitterTasks;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tools.InspectCode.InspectCodeTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static Nuke.Common.Tools.Slack.SlackTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Helpers;


partial class Build : NukeBuild
{
    [Partition(6)] readonly Partition TestPartition;

    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(
        ProjectModelTasks.ParseSolution(AllSolutionFile).GetProjects("*.Tests"));

    Target Test => _ => _
        .DependsOn(Restore)
        .Produces(TestResultDirectory / "*.trx")
        .Partition(() => TestPartition)
        .Executes(() =>
        {
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }
            return DotNetTest(TestSettings);
        });

    Target Cover => _ => _.DependsOn(Restore)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Partition(() => TestPartition)
        .Executes(() =>
        {
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }
            return DotNetTest(CoverSettings);
        });

    IEnumerable<DotNetTestSettings> TestSettings(DotNetTestSettings settings) =>
        TestBaseSettings(settings)
            .CombineWith(TestProjects, (_, v) => _
                .SetProjectFile(v)
                .SetLogger($"trx;LogFileName={v.Name}.trx"));

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
            .SetConfiguration(Configuration.Debug)
            .SetNoRestore(InvokedTargets.Contains(Restore))
            .ResetVerbosity()
            .SetResultsDirectory(TestResultDirectory);
}
