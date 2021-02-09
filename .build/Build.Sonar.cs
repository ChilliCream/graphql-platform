using System.IO;
using Colorful;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using Nuke.Common.Utilities.Collections;
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

            if (!InvokedTargets.Contains(Cover))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            DotNetRestore(c => c
                .SetProjectFile(AllSolutionFile)
                .SetProcessWorkingDirectory(RootDirectory));

            SonarScannerBegin(SonarBeginPrSettings);
            DotNetBuild(SonarBuildAll);
            DotNetTest(CoverNoBuildSettingsOnly50);
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
                .Add("/d:sonar.cs.roslyn.ignoreIssues={0}", "true"));

    SonarScannerBeginSettings SonarBeginFullSettings(SonarScannerBeginSettings settings) =>
        SonarBeginBaseSettings(settings).SetVersion(GitVersion.SemVer);

    SonarScannerBeginSettings SonarBeginBaseSettings(SonarScannerBeginSettings settings) =>
        SonarBaseSettings(settings)
            .SetProjectKey("HotChocolate")
            .SetName("HotChocolate")
            .SetServer(SonarServer)
            .SetLogin(SonarToken)
            .AddOpenCoverPaths(TestResultDirectory / "*.xml")
            .SetVSTestReports(TestResultDirectory / "*.trx")
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
            .SetProcessWorkingDirectory(RootDirectory);

    DotNetBuildSettings SonarBuildAll(DotNetBuildSettings settings) =>
        SonarBuildBaseSettings(settings)
            .SetProjectFile(AllSolutionFile);

    DotNetBuildSettings SonarBuildBaseSettings(DotNetBuildSettings settings) =>
        settings
            .SetNoRestore(InvokedTargets.Contains(Restore))
            .SetConfiguration("Debug")
            .SetProcessWorkingDirectory(RootDirectory);
}
