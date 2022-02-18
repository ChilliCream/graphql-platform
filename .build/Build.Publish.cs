using System;
using System.Collections.Generic;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Helpers;
using static Nuke.Common.Tools.NuGet.NuGetTasks;


partial class Build
{
    // IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);
    [Parameter("NuGet Source for Packages")] readonly string NuGetSource = "https://api.nuget.org/v3/index.json";
    [Parameter("NuGet Api Key")] readonly string NuGetApiKey;

    Target Pack => _ => _
        .DependsOn(PackLocal)
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
        .Requires(() => Configuration.Equals(Release));


    Target PackLocal => _ => _
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
        .Executes(() =>
        {
            var projFile = File.ReadAllText(StarWarsProj);
            File.WriteAllText(StarWarsProj, projFile.Replace("11.1.0", GitVersion.SemVer));

            projFile = File.ReadAllText(EmptyServerProj);
            File.WriteAllText(EmptyServerProj, projFile.Replace("11.1.0", GitVersion.SemVer));

            projFile = File.ReadAllText(EmptyServer12Proj);
            File.WriteAllText(EmptyServer12Proj, projFile.Replace("11.1.0", GitVersion.SemVer));

            projFile = File.ReadAllText(EmptyAzf12Proj);
            File.WriteAllText(EmptyAzf12Proj, projFile.Replace("11.1.0", GitVersion.SemVer));
  
            DotNetBuildSonarSolution(
                PackSolutionFile,
                include: file =>
                    !Path.GetFileNameWithoutExtension(file)
                        .EndsWith("tests", StringComparison.OrdinalIgnoreCase));

            DotNetRestore(c => c.SetProjectFile(PackSolutionFile));

            DotNetBuild(c => c
                .SetNoRestore(true)
                .SetProjectFile(RootDirectory / "src/StrawberryShake/CodeGeneration/src/CodeGeneration.CSharp.Server/StrawberryShake.CodeGeneration.CSharp.Server.csproj")
                .SetOutputDirectory(RootDirectory / "src/StrawberryShake/Tooling/src/.server")
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));

            DotNetBuild(c => c
                .SetNoRestore(true)
                .SetProjectFile(RootDirectory / "src/StrawberryShake/CodeGeneration/src/CodeGeneration.CSharp.Server/StrawberryShake.CodeGeneration.CSharp.Server.csproj")
                .SetOutputDirectory(RootDirectory / "src/StrawberryShake/SourceGenerator/src/.server")
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));

            DotNetBuild(c => c
                .SetNoRestore(true)
                .SetProjectFile(PackSolutionFile)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));

            DotNetPack(c => c
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetProject(PackSolutionFile)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));

            NuGetPack(c => c
                .SetVersion(GitVersion.SemVer)
                .SetOutputDirectory(PackageDirectory)
                .SetConfiguration(Configuration)
                .CombineWith(
                    t => t.SetTargetPath(StarWarsTemplateNuSpec),
                    t => t.SetTargetPath(EmptyServerTemplateNuSpec),
                    t => t.SetTargetPath(TemplatesNuSpec)));
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
