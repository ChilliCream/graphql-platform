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
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.InspectCode;
using Nuke.Common.Tools.NuGet;
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
using static Nuke.Common.Tools.NuGet.NuGetTasks;


partial class Build : NukeBuild
{
    // IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);
    [Parameter("NuGet Source for Packages")] readonly string NuGetSource = "https://api.nuget.org/v3/index.json";
    [Parameter("NuGet Api Key")] readonly string NuGetApiKey;

    Target Pack => _ => _
        .DependsOn(Restore)
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            DotNetPack(c => c
                .SetProject(AllSolutionFile)
                .SetNoBuild(InvokedTargets.Contains(Compile))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetVersion(GitVersion.SemVer));

            var projFile = File.ReadAllText(StarWarsProj);
            File.WriteAllText(StarWarsProj, projFile.Replace("11.0.0-rc.1", GitVersion.SemVer));

            projFile = File.ReadAllText(EmptyServerProj);
            File.WriteAllText(EmptyServerProj, projFile.Replace("11.0.0-rc.1", GitVersion.SemVer));

            NuGetPack(c => c
                .SetVersion(GitVersion.SemVer)
                .SetOutputDirectory(PackageDirectory)
                .SetConfiguration(Configuration)
                .CombineWith(
                    t => t.SetTargetPath(StarWarsTemplateNuSpec),
                    t => t.SetTargetPath(EmptyServerTemplateNuSpec)));


            /*
            NuGetPack(c => c
                .SetVersion(GitVersion.SemVer)
                .SetOutputDirectory(PackageDirectory)
                .SetConfiguration(Configuration)
                .CombineWith(
                    t => t.SetTargetPath(StrawberryShakeNuSpec),
                    t => t.SetTargetPath(StarWarsTemplateNuSpec),
                    t => t.SetTargetPath(EmptyServerTemplateNuSpec)));
                    */

            //.SetPackageReleaseNotes(GetNuGetReleaseNotes(ChangelogFile, GitRepository)));
        });

    Target Publish => _ => _
        .DependsOn(Clean, Test, Pack)
        .Consumes(Pack)
        .Requires(() => NuGetSource)
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            IReadOnlyCollection<AbsolutePath> packages = PackageDirectory.GlobFiles("*.nupkg");

            DotNetNuGetPush(_ => _
                    .SetSource(NuGetSource)
                    .SetApiKey(NuGetApiKey)
                    .CombineWith(packages, (_, v) => _
                        .SetTargetPath(v)),
                degreeOfParallelism: 2,
                completeOnFailure: true);
        });
}
