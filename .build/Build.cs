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


    Target GenerateMatrix => _ => _
        .DependsOn(Clean, Restore)
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);
            Solution all = ProjectModelTasks.ParseSolution(AllSolutionFile);

            var testProjects = all.GetProjects("*.Tests")
                .Select(p => new TestProject
                {
                    Name = Path.GetFileNameWithoutExtension(p.Path),
                    Path = p.Path
                })
                .ToList();

            var matrix = new
            {
                test = testProjects.Select(p => new
                {
                    name = p.Name,
                    path = p.Path
                }).ToArray()
            };

            File.WriteAllText(
                RootDirectory / ".github" / "matrix.json",
                JsonConvert.SerializeObject(matrix));
        });
}


[Serializable]
public class TestProject
{
    public string Name { get; set; }
    public string Path { get; set; }
}
