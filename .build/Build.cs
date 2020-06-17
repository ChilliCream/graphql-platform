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

[GitHubActions(
    "sonar-pr-hotchocolate",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(SonarPrHC) },
    ImportGitHubTokenAs = nameof(GitHubToken),
    ImportSecrets = new[] { nameof(SonarToken) },
    AutoGenerate = false)]
[GitHubActions(
    "tests-pr-hotchocolate",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(TestHC) },
    ImportGitHubTokenAs = nameof(GitHubToken),
    AutoGenerate = false)]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.CompileHC);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath HotChocolateDirectory => SourceDirectory / "HotChocolate";
    Solution HotChocolateSolution => ProjectModelTasks.ParseSolution(HotChocolateDirectory / "HotChocolate.sln");

    AbsolutePath OutputDirectory => RootDirectory / "output";

    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    Target Clean => _ => _
        .Before(RestoreHC)
        .Executes(() =>
        {
        });

    Target RestoreHC => _ => _
        .Executes(() =>
        {
            DotNetRestore(c => c
                .SetProjectFile(HotChocolateSolution));
        });

    Target CompileHC => _ => _
        .DependsOn(RestoreHC)
        .Executes(() =>
        {
            DotNetBuild(c => c
                .SetProjectFile(HotChocolateSolution)
                .SetNoRestore(InvokedTargets.Contains(RestoreHC))
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));
        });

    [Partition(2)] readonly Partition TestPartition;
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";
    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(HotChocolateSolution.GetProjects("*.Tests"));

    Target TestHC => _ => _
        .DependsOn(RestoreHC)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Partition(() => TestPartition)
        .Executes(() =>
        {
            DotNetTest(_ => _
                .SetConfiguration(Configuration.Debug)
                .SetNoRestore(InvokedTargets.Contains(RestoreHC))
                .ResetVerbosity()
                .SetResultsDirectory(TestResultDirectory)
                .When(InvokedTargets.Contains(CoverHC) || IsServerBuild, _ => _
                    .EnableCollectCoverage()
                    .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
                    .SetExcludeByFile("*.Generated.cs"))
                .CombineWith(TestProjects, (_, v) => _
                    .SetProjectFile(v)
                    .SetLogger($"trx;LogFileName={v.Name}.trx")
                    .When(InvokedTargets.Contains(CoverHC) || IsServerBuild, _ => _
                        .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"))));
        });

    Target CoverHC => _ => _
        .DependsOn(TestHC);

    string ChangelogFile => RootDirectory / "CHANGELOG.md";
    AbsolutePath PackageDirectory => OutputDirectory / "packages";
    // IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);

    Target PackHC => _ => _
        .DependsOn(RestoreHC)
        .Executes(() =>
        {
            DotNetPack(_ => _
                .SetProject(HotChocolateSolution)
                .SetNoBuild(InvokedTargets.Contains(CompileHC))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetVersion(GitVersion.SemVer));
            //.SetPackageReleaseNotes(GetNuGetReleaseNotes(ChangelogFile, GitRepository)));
        });

    Target SonarPrHC => _ => _
        .DependsOn(CoverHC)
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

            var gitHubPrNumber = gitHubRefParts[^2];

            SonarScannerBegin(c => c
                .SetProjectKey("HotChocolate")
                .SetName("HotChocolate")
                .SetServer(SonarServer)
                .SetLogin(SonarToken)
                .AddDotCoverPaths(TestResultDirectory / "*.xml")
                .SetVSTestReports(TestResultDirectory / "*.trx")
                .SetWorkingDirectory(HotChocolateDirectory)
                .SetArgumentConfigurator(t => t
                    .Add("/o:{0}", "chillicream")
                    .Add("/d:sonar.pullrequest.provider={0}", "github")
                    .Add("/d:sonar.pullrequest.github.repository={0}", GitHubRepository)
                    .Add("/d:sonar.pullrequest.key={0}", gitHubPrNumber)
                    .Add("/d:sonar.pullrequest.branch={0}", GitHubHeadRef)
                    .Add("/d:sonar.pullrequest.base={0}", GitHubBaseRef)
                    .Add("/d:sonar.cs.roslyn.ignoreIssues={0}", "true")));

            DotNetBuild(c => c
                .SetProjectFile(HotChocolateSolution)
                .SetNoRestore(InvokedTargets.Contains(RestoreHC))
                .SetConfiguration(Configuration.Debug));

            SonarScannerEnd(c => c
                .SetLogin(SonarToken)
                .SetWorkingDirectory(HotChocolateDirectory));
        });

    Target SonarFullHC => _ => _
        .DependsOn(CoverHC)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Executes(() =>
        {
            SonarScannerBegin(c => c
                .SetProjectKey("HotChocolate")
                .SetName("HotChocolate")
                .SetVersion(GitVersion.SemVer)
                .SetServer(SonarServer)
                .SetLogin(SonarToken)
                .AddDotCoverPaths(TestResultDirectory / "*.xml")
                .SetVSTestReports(TestResultDirectory / "*.trx")
                .SetWorkingDirectory(HotChocolateDirectory)
                .SetArgumentConfigurator(t => t
                    .Add("/d:sonar.cs.roslyn.ignoreIssues={0}", "true")));

            DotNetBuild(c => c
                .SetProjectFile(HotChocolateSolution)
                .SetNoRestore(InvokedTargets.Contains(RestoreHC))
                .SetConfiguration(Configuration.Debug));

            SonarScannerEnd(c => c
                .SetLogin(SonarToken)
                .SetWorkingDirectory(HotChocolateDirectory));
        });
}
