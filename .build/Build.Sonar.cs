using Colorful;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Helpers;

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
            DotNetBuild(SonarBuildAll);
            DotNetTest(
                c => CoverNoBuildSettingsOnly50(c, TestProjects),
                degreeOfParallelism: DegreeOfParallelism,
                completeOnFailure: true);
            SonarScannerEnd(SonarEndSettings);
        });

    Target Sonar => _ => _
        .DependsOn(Cover)
        .Consumes(Cover)
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);

            DotNetRestore(c => c
                .SetProjectFile(AllSolutionFile)
                .SetProcessWorkingDirectory(RootDirectory));

            Logger.Info("Creating Sonar analysis for version: {0} ...", GitVersion.SemVer);
            SonarScannerBegin(SonarBeginFullSettings);
            DotNetBuild(SonarBuildAll);
            SonarScannerEnd(SonarEndSettings);
        });

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

    DotNetBuildSettings SonarBuildAll(DotNetBuildSettings settings) =>
        settings
            .SetProjectFile(AllSolutionFile)
            .SetNoRestore(true)
            .SetConfiguration(Debug)
            .SetProcessWorkingDirectory(RootDirectory);
}
