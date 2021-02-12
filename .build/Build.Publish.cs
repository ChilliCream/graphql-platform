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
        .Requires(() => Configuration.Equals(Release))
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

            var analyzerProject = ProjectModelTasks
                .ParseSolution(SgSolutionFile)
                .GetProjects("*.Analyzers")
                .Single();
            
            Project parsedProject = ProjectModelTasks.ParseProject(analyzerProject);
            ProjectItem packageReference = parsedProject.Items
                .Single(t => 
                    t.ItemType == "PackageReference" &&
                    t.IsImported == false &&
                    t.EvaluatedInclude == "StrawberryShake.CodeGeneration.CSharp");
            packageReference.SetMetadataValue("Version", GitVersion.SemVer);
            parsedProject.Save();

            DotNetBuild(c => c
                .SetProjectFile(analyzerProject)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));

            DotNetPack(c => c
                .SetProject(analyzerProject)
                .SetNoBuild(InvokedTargets.Contains(Compile))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetVersion(GitVersion.SemVer));
                
            var analyzerTestProject = ProjectModelTasks
                .ParseSolution(SgSolutionFile)
                .GetProjects("*.Tests")
                .Single();

            parsedProject = ProjectModelTasks.ParseProject(analyzerTestProject);
            packageReference = parsedProject.Items
                .Single(t => 
                    t.ItemType == "PackageReference" &&
                    t.IsImported == false &&
                    t.EvaluatedInclude == "StrawberryShake.CodeGeneration.CSharp.Analyzers");
            packageReference.SetMetadataValue("Version", GitVersion.SemVer);
            parsedProject.Save();
        });

    Target Publish => _ => _
        .DependsOn(Clean, Test, Pack)
        .Consumes(Pack)
        .Requires(() => NuGetSource)
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.Equals(Release))
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
