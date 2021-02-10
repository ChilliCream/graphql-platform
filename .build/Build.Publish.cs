using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Helpers;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using Project = Microsoft.Build.Evaluation.Project;


partial class Build : NukeBuild
{
    // IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);
    [Parameter("NuGet Source for Packages")] readonly string NuGetSource = "https://api.nuget.org/v3/index.json";
    [Parameter("NuGet Api Key")] readonly string NuGetApiKey;

    Target Pack => _ => _
        .DependsOn(Restore, PackLocal)
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
        .Requires(() => Configuration.Equals("Release"))
        .Executes(() =>
        {
            var projFile = File.ReadAllText(StarWarsProj);
            File.WriteAllText(StarWarsProj, projFile.Replace("11.0.0-rc.1", GitVersion.SemVer));

            projFile = File.ReadAllText(EmptyServerProj);
            File.WriteAllText(EmptyServerProj, projFile.Replace("11.0.0-rc.1", GitVersion.SemVer));
        });

    Target PackLocal => _ => _
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
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

            NuGetPack(c => c
                .SetVersion(GitVersion.SemVer)
                .SetOutputDirectory(PackageDirectory)
                .SetConfiguration(Configuration)
                .CombineWith(
                    t => t.SetTargetPath(StarWarsTemplateNuSpec),
                    t => t.SetTargetPath(EmptyServerTemplateNuSpec)));

            var projects = ProjectModelTasks.ParseSolution(SgSolutionFile)
                    .AllProjects.Where(x => !x.Name.Contains("Tests"));

            foreach (Nuke.Common.ProjectModel.Project project in projects)
            {
                Project parsedProject = ProjectModelTasks.ParseProject(project);
                ProjectItem packageReference = parsedProject.Items
                    .FirstOrDefault(x => x.ItemType == "PackageReference" &&
                        !x.IsImported &&
                        x.EvaluatedInclude == "StrawberryShake.CodeGeneration.CSharp");

                packageReference?.SetMetadataValue("Version", GitVersion.SemVer);

                parsedProject.Save();
            }

            DotNetPack(c => c
                .SetProject(SgSolutionFile)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetProcessWorkingDirectory(Path.GetDirectoryName(SgSolutionFile))
                .SetVersion(GitVersion.SemVer));

            // update test projects that use the source generators
            foreach (Nuke.Common.ProjectModel.Project project in projects)
            {
                Project parsedProject = ProjectModelTasks.ParseProject(project);
                ProjectItem packageReference = parsedProject.Items
                    .FirstOrDefault(x => x.ItemType == "PackageReference" &&
                        !x.IsImported &&
                        x.EvaluatedInclude == "StrawberryShake.CodeGeneration.CSharp.Analyzers");

                packageReference?.SetMetadataValue("Version", GitVersion.SemVer);

                parsedProject.Save();
            }

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
        .Requires(() => Configuration.Equals("Release"))
        .Executes(() =>
        {
            IReadOnlyCollection<AbsolutePath> packages = PackageDirectory.GlobFiles("*.nupkg");

            DotNetNuGetPush(
                _ => _
                    .SetSource(NuGetSource)
                    .SetApiKey(NuGetApiKey)
                    .CombineWith(
                        packages,
                        (_, v) => _.SetTargetPath(v)),
                degreeOfParallelism: 2,
                completeOnFailure: true);
        });


}
