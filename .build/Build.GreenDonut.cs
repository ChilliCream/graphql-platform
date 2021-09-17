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


    [Partition(3)] readonly Partition TestPartition;

    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(
        ProjectModelTasks.ParseSolution(AllSolutionFile).GetProjects("*.Tests")
                .Where((t => !ExcludedTests.Contains(t.Name))));

    IEnumerable<Project> CoverProjects => TestPartition.GetCurrent(
        ProjectModelTasks.ParseSolution(AllSolutionFile).GetProjects("*.Tests")
                .Where((t => !ExcludedCover.Contains(t.Name))));

    Target TestGreenDonut => _ => _
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() =>
        {
            DotNetBuild(c => c
                .SetProjectFile(GreenDonutSolution)
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
}
