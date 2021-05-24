using System.IO;
using Colorful;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.SonarScanner;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static Helpers;

partial class Build : NukeBuild
{
    [Parameter] readonly string From;
    [Parameter] readonly string To;
    [Parameter] readonly bool Breaking;

    Target CheckPublicApi => _ => _
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
                .SetProperty("RequireDocumentationOfPublicApiChanges", true));
        });

    Target AddUnshipped => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // first we ensure that the All.sln exists.
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            // new we restore our local dotnet tools including dotnet-format
            DotNetToolRestore(c => c.SetProcessWorkingDirectory(RootDirectory));

            // last we run the actual dotnet format command.
            DotNet($@"format ""{AllSolutionFile}"" --fix-analyzers warn --diagnostics RS0016", workingDirectory: RootDirectory);
        });

    Target DiffShippedApi => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // first we ensure that the All.sln exists.
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            var from = string.IsNullOrEmpty(From) ? GitRepository.Branch : From;
            var to = string.IsNullOrEmpty(To) ? "main" : To;

            if (from == to)
            {
                Console.WriteLine("Nothing to diff here.");
                return;
            }

            AbsolutePath shippedPath = SourceDirectory / "**" / "PublicAPI.Shipped.txt";

            Git($@" --no-pager diff --minimal -U0 --word-diff ""{from}"" ""{to}"" -- ""{shippedPath}""", RootDirectory);
        });

    Target DisplayUnshippedApi => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // first we ensure that the All.sln exists.
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }


        });

    Target MarkApiShipped => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // first we ensure that the All.sln exists.
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            foreach (var file in Directory.GetFiles(SourceDirectory, "PublicApi.Shipped.txt", SearchOption.AllDirectories))
            {

            }

        });
}
