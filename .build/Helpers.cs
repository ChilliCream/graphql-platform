using System.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

static class Helpers
{
    static readonly string[] Directories =
    {
        "GreenDonut",
        Path.Combine("HotChocolate", "ApolloFederation"),
        Path.Combine("HotChocolate", "AspNetCore"),
        Path.Combine("HotChocolate", "AzureFunctions"),
        Path.Combine("HotChocolate", "Core"),
        Path.Combine("HotChocolate", "CostAnalysis"),
        Path.Combine("HotChocolate", "Caching"),
        Path.Combine("HotChocolate", "Diagnostics"),
        Path.Combine("HotChocolate", "Language"),
        Path.Combine("HotChocolate", "PersistedOperations"),
        Path.Combine("HotChocolate", "Utilities"),
        Path.Combine("HotChocolate", "Data"),
        Path.Combine("HotChocolate", "Marten"),
        Path.Combine("HotChocolate", "MongoDb"),
        Path.Combine("HotChocolate", "OpenApi"),
        Path.Combine("HotChocolate", "Pagination"),
        Path.Combine("HotChocolate", "Primitives"),
        Path.Combine("HotChocolate", "Raven"),
        Path.Combine("HotChocolate", "Skimmed"),
        Path.Combine("HotChocolate", "Fusion"),
        Path.Combine("HotChocolate", "Spatial"),
        Path.Combine("StrawberryShake", "Client"),
        Path.Combine("StrawberryShake", "CodeGeneration"),
        Path.Combine("StrawberryShake", "MetaPackages"),
        Path.Combine("StrawberryShake", "Tooling"),
        "CookieCrumble"
    };

    static IEnumerable<string> GetAllProjects(
        string sourceDirectory,
        IEnumerable<string> directories,
        Func<string, bool> include = null)
    {
        foreach (var directory in directories)
        {
            var fullDirectory = Path.Combine(sourceDirectory, directory);
            foreach (var file in Directory.EnumerateFiles(fullDirectory, "*.csproj", SearchOption.AllDirectories))
            {
                if (!(include?.Invoke(file) ?? true)
                    || file.Contains("benchmark", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("demo", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("sample", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("examples", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                yield return file;
            }
        }
    }

    public static IReadOnlyCollection<Output> DotNetBuildSonarSolution(
        string solutionFile,
        IEnumerable<string> directories = null,
        Func<string, bool> include = null)
    {
        if (File.Exists(solutionFile))
        {
            return Array.Empty<Output>();
        }

        directories ??= Directories;

        var projects = GetAllProjects(Path.GetDirectoryName(solutionFile), directories, include);
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

    public static void TryDelete(string fileName)
    {
        if(File.Exists(fileName))
        {
            File.Delete(fileName);
        }
    }
}
