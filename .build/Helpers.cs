using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

class Helpers
{
    public static readonly string[] Directories =
    {
        "GreenDonut",
        // Path.Combine("HotChocolate", "ApolloFederation"),
        Path.Combine("HotChocolate", "AspNetCore"),
        Path.Combine("HotChocolate", "Core"),
        Path.Combine("HotChocolate", "Language"),
        Path.Combine("HotChocolate", "PersistedQueries"),
        Path.Combine("HotChocolate", "Utilities"),
        Path.Combine("HotChocolate", "Data"),
        Path.Combine("HotChocolate", "Filters"),
        Path.Combine("HotChocolate", "MongoDb"),
        Path.Combine("HotChocolate", "Stitching"),
        Path.Combine("HotChocolate", "Spatial"),
        Path.Combine("StrawberryShake", "Client"),
        Path.Combine("StrawberryShake", "CodeGeneration"),
        Path.Combine("StrawberryShake", "Tooling")
    };

    public static IEnumerable<string> GetAllProjects(
        string sourceDirectory, 
        IEnumerable<string> directories)
    {
        foreach (var directory in directories)
        {
            var fullDirectory = Path.Combine(sourceDirectory, directory);
            foreach (var file in Directory.EnumerateFiles(fullDirectory, "*.csproj", SearchOption.AllDirectories))
            {
                if (file.Contains("benchmark", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("demo", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("sample", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("HotChocolate.Core.Tests", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("HotChocolate.Utilities.Introspection.Tests", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("HotChocolate.Types.Selection", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                yield return file;
            }
        }
    }

    public static IReadOnlyCollection<Output> DotNetBuildSonarSolution(
        string solutionFile,
        IEnumerable<string> directories = null)
    {
        if (File.Exists(solutionFile))
        {
            return Array.Empty<Output>();
        }

        directories ??= Directories;

        IEnumerable<string> projects = GetAllProjects(Path.GetDirectoryName(solutionFile), directories);
        var workingDirectory = Path.GetDirectoryName(solutionFile);
        var list = new List<Output>();

        list.AddRange(DotNetTasks.DotNet($"new sln -n {Path.GetFileNameWithoutExtension(solutionFile)}", workingDirectory));

        var projectsArg = string.Join(" ", projects.Select(t => $"\"{t}\""));

        list.AddRange(DotNetTasks.DotNet($"sln \"{solutionFile}\" add {projectsArg}", workingDirectory));

        return list;
    }

    public static IReadOnlyCollection<Output> DotNetBuildTestSolution(
        string solutionFile,
        IEnumerable<Project> projects)
    {
        if (File.Exists(solutionFile))
        {
            return Array.Empty<Output>();
        }

        var workingDirectory = Path.GetDirectoryName(solutionFile);
        var list = new List<Output>();

        list.AddRange(DotNetTasks.DotNet($"new sln -n {Path.GetFileNameWithoutExtension(solutionFile)}", workingDirectory));

        var projectsArg = string.Join(" ", projects.Select(t => $"\"{t}\""));

        list.AddRange(DotNetTasks.DotNet($"sln \"{solutionFile}\" add {projectsArg}", workingDirectory));

        return list;
    }
}
