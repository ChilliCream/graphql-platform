using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.Execution;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Helpers;
using Newtonsoft.Json;
using Nuke.Common.ProjectModel;
using System.Linq;
using System;
using System.IO;
using Serilog;

[UnsetVisualStudioEnvironmentVariables]
partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? Debug : Release;

    [CI] readonly AzurePipelines DevOpsPipeLine;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);
            DotNetRestore(c => c.SetProjectFile(AllSolutionFile));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            DotNetBuild(c => c
                .SetProjectFile(AllSolutionFile)
                .SetNoRestore(InvokedTargets.Contains(Restore))
                .SetConfiguration(Configuration)
                .SetInformationalVersion(SemVersion)
                .SetFileVersion(Version)
                .SetAssemblyVersion(Version)
                .SetVersion(SemVersion));
        });

    Target Reset => _ => _
        .Executes(() =>
        {
            TryDelete(AllSolutionFile);
            TryDelete(TestSolutionFile);
            TryDelete(PackSolutionFile);

            DotNetBuildSonarSolution(AllSolutionFile);
            DotNetRestore(c => c.SetProjectFile(AllSolutionFile));
        });

    Target CreateAllSln => _ => _
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);
        });

    Target GenerateMatrix => _ => _
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);
            var all = ProjectModelTasks.ParseSolution(AllSolutionFile);

            var testProjects = all.GetProjects("*.Tests")
                .Select(p => new TestProject
                {
                    Name = Path.GetFileNameWithoutExtension(p.Path),
                    Path = Path.GetRelativePath(RootDirectory, p.Path)
                })
                .OrderBy(p => p.Name)
                .ToList();

            var matrix = new
            {
                include = testProjects.Select(p => new
                {
                    name = p.Name,
                    path = p.Path,
                    directoryPath = Path.GetDirectoryName(p.Path)
                }).ToArray()
            };

            File.WriteAllText(
                RootDirectory / "matrix.json",
                JsonConvert.SerializeObject(matrix));
        });

    Target Accept => _ => _
        .Executes(() =>
        {
            foreach (var mismatchDir in Directory.GetDirectories(
                RootDirectory, "__mismatch__", SearchOption.AllDirectories))
            {
                Log.Information("Analyzing {0} ...", mismatchDir);

                var snapshotDir = Directory.GetParent(mismatchDir)!.FullName;
                foreach (var mismatch in Directory.GetFiles(
                    mismatchDir, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var snapshot = Path.Combine(snapshotDir, Path.GetFileName(mismatch));
                    if (File.Exists(snapshot))
                    {
                        File.Delete(snapshot);
                        File.Move(mismatch, snapshot);
                    }
                }

                foreach (var mismatch in Directory.GetFiles(
                    mismatchDir, "*.*", SearchOption.AllDirectories))
                {
                    File.Delete(mismatch);
                }

                Directory.Delete(mismatchDir, true);
            }
        });
}

[Serializable]
public class TestProject
{
    public string Name { get; set; }
    public string Path { get; set; }
}
