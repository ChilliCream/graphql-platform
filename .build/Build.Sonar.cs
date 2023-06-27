using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using static System.IO.Path;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Helpers;

partial class Build
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
            Logger.Info($"GitHubRepository: {GitHubRepository}");
            Logger.Info($"GitHubHeadRef: {GitHubHeadRef}");
            Logger.Info($"GitHubBaseRef: {GitHubBaseRef}");
            Logger.Info($"GitHubPRNumber: {GitHubPRNumber}");

            TryDelete(SonarSolutionFile);
            DotNetBuildSonarSolution(AllSolutionFile);
            DotNetBuildSonarSolution(SonarSolutionFile, include: IsRelevantForSonar);

            DotNetRestore(c => c
                .SetProjectFile(SonarSolutionFile)
                .SetProcessWorkingDirectory(RootDirectory));

            SonarScannerBegin(SonarBeginPrSettings);
            DotNetBuild(SonarBuildAll);
            try
            {
                DotNetTest(
                    c => CoverNoBuildSettingsOnlyNet60(c, CoverProjects),
                    degreeOfParallelism: DegreeOfParallelism,
                    completeOnFailure: true);
            }
            catch { }
            SonarScannerEnd(SonarEndSettings);
        });

    Target Sonar => _ => _
        .Executes(() =>
        {
            TryDelete(SonarSolutionFile);
            DotNetBuildSonarSolution(AllSolutionFile);
            DotNetBuildSonarSolution(SonarSolutionFile, include: IsRelevantForSonar);

            DotNetRestore(c => c
                .SetProjectFile(SonarSolutionFile)
                .SetProcessWorkingDirectory(RootDirectory));

            Logger.Info("Creating Sonar analysis for version: {0} ...", SemVersion);

            SonarScannerBegin(SonarBeginFullSettings);
            DotNetBuild(SonarBuildAll);
            try
            {
                DotNetTest(
                    c => CoverNoBuildSettingsOnlyNet60(c, CoverProjects),
                    degreeOfParallelism: DegreeOfParallelism,
                    completeOnFailure: true);
            }
            catch { }
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
            .SetVersion(SemVersion)
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
            .SetProjectFile(SonarSolutionFile)
            .SetNoRestore(true)
            .SetConfiguration(Debug)
            .SetProcessWorkingDirectory(RootDirectory)
            .SetFramework(Net60);

    bool IsRelevantForSonar(string fileName)
        => !ExcludedCover.Contains(GetFileNameWithoutExtension(fileName)) &&
            !fileName.Contains("example") &&
            !fileName.Contains("sample");
}
