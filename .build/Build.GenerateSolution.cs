using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common.Tooling;
using Nuke.Common.ProjectModel;

partial class Build
{
    [PackageExecutable(
        packageId: "Microsoft.VisualStudio.SlnGen.Tool",
        packageExecutable: "slngen.exe",
        // Must be set for tools shipping multiple versions
        Framework = "net8.0")]
    readonly Tool SlnGen;

    IReadOnlyCollection<Output> DotNetBuildSonarSolution(
        string solutionOutputFile,
        IEnumerable<string> directories = null,
        Func<string, bool> include = null)
    {
        var projects = Helpers.GetAllProjects(SourceDirectory, directories, include);
        var result = CreateSolution(solutionOutputFile, projects);
        return result;
    }

    IReadOnlyCollection<Output> DotNetBuildTestSolution(
        string solutionOutputFile,
        IEnumerable<Project> projects)
    {
        var projectPaths = projects.Select(p => (string) p.Path);
        var result = CreateSolution(solutionOutputFile, projectPaths);
        return result;
    }

    IReadOnlyCollection<Output> CreateSolution(
        string solutionOutputFile,
        IEnumerable<string> projectAbsolutePaths)
    {
        if (File.Exists(solutionOutputFile))
        {
            return Array.Empty<Output>();
        }

        var solutionDirectory = Path.GetDirectoryName(solutionOutputFile)!;
        var workingDirectory = solutionDirectory;

        var arguments = new Arguments();

        foreach (var projectAbsolutePath in projectAbsolutePaths)
        {
            var projectRelativePath = Path.GetRelativePath(workingDirectory, projectAbsolutePath);
            arguments.Add($"{projectRelativePath}");
        }

        // https://microsoft.github.io/slngen/
        arguments.Add("--solutionfile {value}", solutionOutputFile);
        arguments.Add("--launch {value}", "false");
        arguments.Add("--verbosity {value}", "minimal");
        arguments.Add("--folders {value}", "true");

        var result = SlnGen(
            arguments: arguments.RenderForExecution(),
            workingDirectory: workingDirectory);

        return result;
    }
}
