using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

class Helpers
{
    static readonly string[] _directories = new string[]
    {
        "GreenDonut",
        Path.Combine("HotChocolate", "ApolloFederation"),
        Path.Combine("HotChocolate", "AspNetCore"),
        Path.Combine("HotChocolate", "Core"),
        Path.Combine("HotChocolate", "Language"),
        Path.Combine("HotChocolate", "PersistedQueries"),
        Path.Combine("HotChocolate", "Utilities"),
        Path.Combine("HotChocolate", "Data"),
        Path.Combine("HotChocolate", "Filters"),
        Path.Combine("HotChocolate", "MongoDb"),
        Path.Combine("HotChocolate", "Stitching"),
        Path.Combine("HotChocolate", "Spatial")
    };

    public static IEnumerable<string> GetAllProjects(string sourceDirectory)
    {
        foreach (var directory in _directories)
        {
            var fullDirectory = Path.Combine(sourceDirectory, directory);
            foreach (var file in Directory.EnumerateFiles(fullDirectory, "*.csproj", SearchOption.AllDirectories))
            {
                if (file.Contains("benchmark", StringComparison.OrdinalIgnoreCase)
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
