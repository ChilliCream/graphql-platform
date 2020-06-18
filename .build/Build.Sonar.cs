using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;

partial class Build : NukeBuild
{
    [Parameter] readonly string SonarToken;
    [Parameter] readonly string SonarServer = "https://sonarcloud.io";

     Target SonarPr => _ => _
        .DependsOn(Cover)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Executes(() =>
        {
            string[] gitHubRefParts = GitHubRef.Split('/');
            if (gitHubRefParts.Length < 4)
            {
                Logger.Error("The GitHub_Ref variable has not the expected structure. {0}", GitHubRef);
                return;
            }

            SonarScannerBegin(c => SonarBeginPrSettings(c, gitHubRefParts[^2]));
            DotNetBuild(SonarBuildAll);
            SonarScannerEnd(SonarEndSettings);
        });

    Target Sonar => _ => _
        // .DependsOn(Cover)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Executes(() =>
        {


            Logger.Info("Creating Sonar analysis for version: {0} ...", GitVersion.SemVer);
            SonarScannerBegin(SonarBeginFullSettings);
            DotNetBuild(SonarBuildAll);
            SonarScannerEnd(SonarEndSettings);
        });

    SonarScannerBeginSettings SonarBeginPrSettings(SonarScannerBeginSettings settings, string gitHubPrNumber) =>
        SonarBeginBaseSettings(settings)
            .SetArgumentConfigurator(t => t
                .Add("/o:{0}", "chillicream")
                .Add("/d:sonar.pullrequest.provider={0}", "github")
                .Add("/d:sonar.pullrequest.github.repository={0}", GitHubRepository)
                .Add("/d:sonar.pullrequest.key={0}", gitHubPrNumber)
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
            .SetArgumentConfigurator(t => t
                .Add("/o:{0}", "chillicream")
                .Add("/d:sonar.cs.roslyn.ignoreIssues={0}", "true"));

    SonarScannerBeginSettings SonarBaseSettings(SonarScannerBeginSettings settings) =>
        settings
            .SetLogin(SonarToken)
            .SetWorkingDirectory(RootDirectory);

    SonarScannerEndSettings SonarEndSettings(SonarScannerEndSettings settings) =>
        settings
            .SetLogin(SonarToken)
            .SetWorkingDirectory(RootDirectory);

    DotNetBuildSettings SonarBuildAll(DotNetBuildSettings settings) =>
        SonarBuildBaseSettings(settings)
            .SetProjectFile(AllSolutionFile);

    DotNetBuildSettings SonarBuildBaseSettings(DotNetBuildSettings settings) =>
        settings
            .SetNoRestore(InvokedTargets.Contains(Restore))
            .SetConfiguration(Configuration.Debug)
            .SetWorkingDirectory(RootDirectory);
}
