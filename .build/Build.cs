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

[AzurePipelines(
    suffix: "test-pr-hotchocolate",
    AzurePipelinesImage.UbuntuLatest,
    InvokedTargets = new[] { nameof(Test) })]
[GitHubActions(
    "sonar-pr-hotchocolate",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(SonarPr) },
    ImportGitHubTokenAs = nameof(GitHubToken),
    ImportSecrets = new[] { nameof(SonarToken) },
    AutoGenerate = false)]
[GitHubActions(
    "tests-pr-hotchocolate",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.PullRequest },
    InvokedTargets = new[] { nameof(Test) },
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
    AbsolutePath GreenDonutDirectory => SourceDirectory / "GreenDonut";
    Solution GreenDonutSolution => ProjectModelTasks.ParseSolution(GreenDonutDirectory / "GreenDonut.sln");
    AbsolutePath StrawberryShakeDirectory => SourceDirectory / "StrawberryShake";
    Solution StrawberryShakeClientSolution => ProjectModelTasks.ParseSolution(StrawberryShakeDirectory / "Client" / "StrawberryShake.Client.sln");
    Solution StrawberryShakeCodeGenerationSolution => ProjectModelTasks.ParseSolution(StrawberryShakeDirectory / "CodeGeneration" / "StrawberryShake.CodeGeneration.sln");

    AbsolutePath OutputDirectory => RootDirectory / "output";

    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(c => c
                .SetProjectFile(GreenDonutSolution));
            DotNetRestore(c => c
                .SetProjectFile(HotChocolateSolution));
            DotNetRestore(c => c
                .SetProjectFile(StrawberryShakeClientSolution));
            DotNetRestore(c => c
                .SetProjectFile(StrawberryShakeCodeGenerationSolution));
        });

    Target CompileHC => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(c => c
                .SetProjectFile(HotChocolateSolution)
                .SetNoRestore(InvokedTargets.Contains(Restore))
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));
        });

    [Partition(4)] readonly Partition TestPartition;
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";

    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(
        GreenDonutSolution.GetProjects("*.Tests").Concat(
            HotChocolateSolution.GetProjects("*.Tests")).Concat(
            StrawberryShakeClientSolution.GetProjects("*.Tests")).Concat(
            StrawberryShakeCodeGenerationSolution.GetProjects("*.Tests")));

    Target Test => _ => _
        .DependsOn(Restore)
        .Produces(TestResultDirectory / "*.trx")
        .Partition(() => TestPartition)
        .Executes(() => DotNetTest(TestSettings));

    Target Cover => _ => _.DependsOn(Restore)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Partition(() => TestPartition)
        .Executes(() => DotNetTest(CoverSettings));

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

    string ChangelogFile => RootDirectory / "CHANGELOG.md";
    AbsolutePath PackageDirectory => OutputDirectory / "packages";
    // IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);

    Target PackHC => _ => _
        .DependsOn(Restore)
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
            DotNetBuild(SonarBuildGreenDonut);
            DotNetBuild(SonarBuildHotChocolate);
            DotNetBuild(SonarBuildStrawberryShakeClient);
            DotNetBuild(SonarBuildStrawberryShakeCodeGeneration);
            SonarScannerEnd(SonarEndSettings);
        });

    Target SonarFull => _ => _
        .DependsOn(Cover)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Executes(() =>
        {
            Logger.Info("Creating Sonar analysis for version: {0}", GitVersion.SemVer);
            SonarScannerBegin(SonarBeginFullSettings);
            DotNetBuild(SonarBuildGreenDonut);
            DotNetBuild(SonarBuildHotChocolate);
            DotNetBuild(SonarBuildStrawberryShakeClient);
            DotNetBuild(SonarBuildStrawberryShakeCodeGeneration);
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

    DotNetBuildSettings SonarBuildGreenDonut(DotNetBuildSettings settings) =>
        SonarBuildBaseSettings(settings)
            .SetProjectFile(GreenDonutSolution);

    DotNetBuildSettings SonarBuildHotChocolate(DotNetBuildSettings settings) =>
        SonarBuildBaseSettings(settings)
            .SetProjectFile(HotChocolateSolution);

    DotNetBuildSettings SonarBuildStrawberryShakeClient(DotNetBuildSettings settings) =>
        SonarBuildBaseSettings(settings)
            .SetProjectFile(StrawberryShakeClientSolution);

    DotNetBuildSettings SonarBuildStrawberryShakeCodeGeneration(DotNetBuildSettings settings) =>
        SonarBuildBaseSettings(settings)
            .SetProjectFile(StrawberryShakeCodeGenerationSolution);

    DotNetBuildSettings SonarBuildBaseSettings(DotNetBuildSettings settings) =>
        settings
            .SetNoRestore(InvokedTargets.Contains(Restore))
            .SetConfiguration(Configuration.Debug)
            .SetWorkingDirectory(RootDirectory);
}

