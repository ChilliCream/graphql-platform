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
        string solutionFile,
        IEnumerable<string> projects)
    {
        if (File.Exists(solutionFile))
        {
            return Array.Empty<Output>();
        }

        var workingDirectory = Path.GetDirectoryName(solutionFile);
        var list = new List<Output>();

        list.AddRange(DotNetTasks.DotNet($"new sln -n {Path.GetFileNameWithoutExtension(solutionFile)}", workingDirectory));

        foreach (var projectFile in projects)
        {
            list.AddRange(DotNetTasks.DotNet($"sln add \"{projectFile}\"", workingDirectory));
        }

        return list;
    }
}
