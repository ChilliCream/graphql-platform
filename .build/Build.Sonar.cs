using Colorful;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Helpers;
using System.Collections.Generic;
using Nuke.Common.ProjectModel;
using Nuke.Common.IO;
using System.Linq;

partial class Build : NukeBuild
{
    [Parameter] readonly string SonarToken;
    [Parameter] readonly string SonarServer = "https://sonarcloud.io";

    Target SonarPr => _ => _
        .Requires(() => GitHubRepository != null)
        .Requires(() => GitHubHeadRef != null)
        .Requires(() => GitHubBaseRef != null)
        .Requires(() => GitHubPRNumber != null)
        .Executes(() =>
        {
            Console.WriteLine($"GitHubRepository: {GitHubRepository}");
            Console.WriteLine($"GitHubHeadRef: {GitHubHeadRef}");
            Console.WriteLine($"GitHubBaseRef: {GitHubBaseRef}");
            Console.WriteLine($"GitHubPRNumber: {GitHubPRNumber}");

            DotNetBuildSonarSolution(AllSolutionFile);

            DotNetRestore(c => c
                .SetProjectFile(AllSolutionFile)
                .SetProcessWorkingDirectory(RootDirectory));

            SonarScannerBegin(SonarBeginPrSettings);
            /* 
            DotNetBuild(SonarBuildAll);
            DotNetTest(
                CoverNoBuildSettingsOnly50,
                degreeOfParallelism: DegreeOfParallelism, 
                completeOnFailure: true);
            */
            SonarScannerEnd(SonarEndSettings);
        });

    [Partition(5)] readonly Partition SonarPartition;

    IEnumerable<string> SonarDirectories => SonarPartition.GetCurrent(Helpers.Directories);

    IEnumerable<Project> GetSonarTestProjects(AbsolutePath solution) => 
        ProjectModelTasks.ParseSolution(solution).GetProjects("*.Tests")
            .Where((t => !ExcludedTests.Contains(t.Name)));

    Target SonarPrSplit => _ => _
        .Requires(() => GitHubRepository != null)
        .Requires(() => GitHubHeadRef != null)
        .Requires(() => GitHubBaseRef != null)
        .Requires(() => GitHubPRNumber != null)
        .Executes(() =>
        {
            Console.WriteLine($"GitHubRepository: {GitHubRepository}");
            Console.WriteLine($"GitHubHeadRef: {GitHubHeadRef}");
            Console.WriteLine($"GitHubBaseRef: {GitHubBaseRef}");
            Console.WriteLine($"GitHubPRNumber: {GitHubPRNumber}");

            DotNetBuildSonarSolution(SonarSolutionFile, SonarDirectories);

            DotNetRestore(c => c
                .SetProjectFile(SonarSolutionFile)
                .SetProcessWorkingDirectory(RootDirectory));

            SonarScannerBegin(SonarBeginPrSettings);
            DotNetBuild(
                c => SonarBuildAll(c, SonarSolutionFile));
            DotNetTest(
                c => CoverNoBuildSettingsOnly50(c, GetSonarTestProjects(SonarSolutionFile)),
                degreeOfParallelism: DegreeOfParallelism, 
                completeOnFailure: true);
            SonarScannerEnd(SonarEndSettings);
        });

    Target Sonar => _ => _
        .DependsOn(Cover)
        .Consumes(Cover)
        .Executes(() =>
        {
            if (!InvokedTargets.Contains(Cover))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            Logger.Info("Creating Sonar analysis for version: {0} ...", GitVersion.SemVer);
            SonarScannerBegin(SonarBeginFullSettings);
            //DotNetBuild(SonarBuildAll);
            SonarScannerEnd(SonarEndSettings);
        });

        // sonar.project.monorepo.enabled

    SonarScannerBeginSettings SonarBeginPrSettings(SonarScannerBeginSettings settings) =>
        SonarBeginBaseSettings(settings)
            .SetProcessArgumentConfigurator(t => t
                .Add("/o:{0}", "chillicream")
                .Add("/d:sonar.pullrequest.provider={0}", "github")
                .Add("/d:sonar.pullrequest.github.repository={0}", GitHubRepository)
                .Add("/d:sonar.pullrequest.key={0}", GitHubPRNumber)
                .Add("/d:sonar.pullrequest.branch={0}", GitHubHeadRef)
                .Add("/d:sonar.pullrequest.base={0}", GitHubBaseRef)
                .Add("/d:sonar.cs.roslyn.ignoreIssues={0}", "true"))
            .SetFramework(Net50);

    SonarScannerBeginSettings SonarBeginFullSettings(SonarScannerBeginSettings settings) =>
        SonarBeginBaseSettings(settings)
            .SetVersion(GitVersion.SemVer)
            .SetFramework(Net50);

    SonarScannerBeginSettings SonarBeginBaseSettings(SonarScannerBeginSettings settings) =>
        SonarBaseSettings(settings)
            .SetProjectKey("HotChocolate")
            .SetName("HotChocolate")
            .SetServer(SonarServer)
            .SetLogin(SonarToken)
            .AddOpenCoverPaths(TestResultDirectory / "*.xml")
            .SetVSTestReports(TestResultDirectory / "*.trx")
            .AddSourceExclusions("**/Generated/**/*.*,**/*.Designer.cs,**/*.generated.cs,**/*.js,**/*.html,**/*.css,**/Sample/**/*.*,**/Samples.*/**/*.*,**/*Tools.*/**/*.*,**/Program.Dev.cs, **/Program.cs,**/*.ts,**/*.tsx,**/*EventSource.cs,**/*EventSources.cs,**/*.Samples.cs,**/*Tests.*/**/*.*,**/*Test.*/**/*.*")
            .SetProcessArgumentConfigurator(t => t
                .Add("/o:{0}", "chillicream")
                .Add("/d:sonar.cs.roslyn.ignoreIssues={0}", "true"));

    SonarScannerBeginSettings SonarBaseSettings(SonarScannerBeginSettings settings) =>
        settings
            .SetLogin(SonarToken)
            .SetProcessWorkingDirectory(RootDirectory);

    SonarScannerEndSettings SonarEndSettings(SonarScannerEndSettings settings) =>
        settings
            .SetLogin(SonarToken)
            .SetProcessWorkingDirectory(RootDirectory)
            .SetFramework(Net50);

    DotNetBuildSettings SonarBuildAll(
        DotNetBuildSettings settings, 
        AbsolutePath solution) =>
        settings
            .SetProjectFile(solution)
            .SetNoRestore(true)
            .SetConfiguration(Debug)
            .SetProcessWorkingDirectory(RootDirectory);
}
