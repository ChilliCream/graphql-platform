using Colorful;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Helpers;
using static System.IO.Path;

partial class Build
{
    Target Format => _ => _
        .Executes(() =>
        {
            DotNetBuildSonarSolution(AllSolutionFile);

            DotNetBuild(c => c
                .SetProjectFile(AllSolutionFile)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVersion(GitVersion.SemVer));

            foreach (Project project in ProjectModelTasks.ParseSolution(AllSolutionFile).AllProjects)
            {
                DotNet($@"format ""{project}"" --verbosity diag");
            }
        });
}
