using System;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Helpers;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using Nuke.Common.Utilities.Collections;
using System.Linq;

partial class Build
{
    // IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);
    [Parameter("NuGet Source for Packages")] readonly string NuGetSource = "https://api.nuget.org/v3/index.json";
    [Parameter("NuGet Api Key")] readonly string NuGetApiKey;
    [Parameter("NuGet Source for Packages")] readonly string MyGetSource = "https://www.myget.org/F/hotchocolate/api/v3/index.json";
    [Parameter("MyGet Api Key")] readonly string MyGetApiKey;

    Target Pack => _ => _
        .DependsOn(PackLocal)
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
        .Requires(() => Configuration.Equals(Release))
        .Executes(() =>
        {
            /*
            var packages = PackageDirectory.GlobFiles("*.*.nupkg");

            DotNetNuGetPush(
                _ => _
                    .SetSource(MyGetSource)
                    .SetApiKey(MyGetApiKey)
                    .CombineWith(
                        packages,
                        (_, v) => _.SetTargetPath(v)),
                degreeOfParallelism: 2,
                completeOnFailure: true);
            */
        });

    Target PackLocal => _ => _
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
        .Executes(() =>
        {
            var projFile = File.ReadAllText(EmptyServer12Proj);
            File.WriteAllText(EmptyServer12Proj, projFile.Replace("14.0.0-preview.build.0", SemVersion));

            projFile = File.ReadAllText(EmptyAzf12Proj);
            File.WriteAllText(EmptyAzf12Proj, projFile.Replace("14.0.0-preview.build.0", SemVersion));

            projFile = File.ReadAllText(Gateway13Proj);
            File.WriteAllText(Gateway13Proj, projFile.Replace("14.0.0-preview.build.0", SemVersion));

            projFile = File.ReadAllText(GatewayManaged13Proj);
            File.WriteAllText(GatewayManaged13Proj, projFile.Replace("14.0.0-preview.build.0", SemVersion));

            projFile = File.ReadAllText(GatewayAspire13Proj);
            File.WriteAllText(GatewayAspire13Proj, projFile.Replace("14.0.0-preview.build.0", SemVersion));

            DotNetBuildSonarSolution(
                PackSolutionFile,
                include: file =>
                    !Path.GetFileNameWithoutExtension(file)
                        .EndsWith("tests", StringComparison.OrdinalIgnoreCase));

            DotNetRestore(c => c.SetProjectFile(PackSolutionFile));

            DotNetBuild(c => c
                .SetNoRestore(true)
                .SetProjectFile(PackSolutionFile)
                .SetConfiguration(Configuration)
                .SetInformationalVersion(SemVersion)
                .SetFileVersion(Version)
                .SetAssemblyVersion(Version)
                .SetVersion(SemVersion));

            DotNetPack(c => c
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetProject(PackSolutionFile)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetInformationalVersion(SemVersion)
                .SetFileVersion(Version)
                .SetAssemblyVersion(Version)
                .SetVersion(SemVersion));

            NuGetPack(c => c
                .SetVersion(SemVersion)
                .SetOutputDirectory(PackageDirectory)
                .SetConfiguration(Configuration)
                .CombineWith(t => t.SetTargetPath(TemplatesNuSpec)));
        });

    Target Publish => _ => _
        .DependsOn(Clean, Test, Pack)
        .Consumes(Pack)
        .Requires(() => NuGetSource)
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.Equals(Release))
        .Executes(() =>
        {
            var packages = PackageDirectory.GlobFiles("*.*.nupkg");

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
