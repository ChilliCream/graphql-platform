using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

class Helpers
{
    public static IEnumerable<string> GetAllProjects(string sourceDirectory) =>
        Directory.EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(s => !s.Contains("VisualStudio"));

    public static IReadOnlyCollection<Output> DotNetBuildSonarSolution(
        string solutionFile)
    {
        if (File.Exists(solutionFile))
        {
            return Array.Empty<Output>();
        }

        IEnumerable<string> projects = GetAllProjects(Path.GetDirectoryName(solutionFile));
        var workingDirectory = Path.GetDirectoryName(solutionFile);
        var list = new List<Output>();

        list.AddRange(DotNetTasks.DotNet($"new sln -n {Path.GetFileNameWithoutExtension(solutionFile)}", workingDirectory));

        var projectsArg = string.Join(" ", projects.Select(t => $"\"{t}\""));

        list.AddRange(DotNetTasks.DotNet($"sln \"{solutionFile}\" add {projectsArg}", workingDirectory));

        return list;
    }
}
