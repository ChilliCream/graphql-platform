using System;
using System.Collections.Generic;
using System.IO;

static class Helpers
{
    static readonly string[] Directories =
    {
        "GreenDonut",
        Path.Combine("HotChocolate", "ApolloFederation"),
        Path.Combine("HotChocolate", "AspNetCore"),
        Path.Combine("HotChocolate", "AzureFunctions"),
        Path.Combine("HotChocolate", "Core"),
        Path.Combine("HotChocolate", "Caching"),
        Path.Combine("HotChocolate", "Diagnostics"),
        Path.Combine("HotChocolate", "Language"),
        Path.Combine("HotChocolate", "PersistedQueries"),
        Path.Combine("HotChocolate", "Utilities"),
        Path.Combine("HotChocolate", "Data"),
        Path.Combine("HotChocolate", "Marten"),
        Path.Combine("HotChocolate", "MongoDb"),
        Path.Combine("HotChocolate", "OpenApi"),
        Path.Combine("HotChocolate", "Raven"),
        Path.Combine("HotChocolate", "Skimmed"),
        Path.Combine("HotChocolate", "Fusion"),
        Path.Combine("HotChocolate", "Spatial"),
        Path.Combine("StrawberryShake", "Client"),
        Path.Combine("StrawberryShake", "CodeGeneration"),
        Path.Combine("StrawberryShake", "MetaPackages"),
        Path.Combine("StrawberryShake", "Tooling"),
        "CookieCrumble",
    };

    static readonly string[] IgnoredProjectSegments = new[]
    {
        "benchmark",
        "demo",
        "sample",
        "examples",
    };

    static bool ProjectContainsIgnoredSegment(ReadOnlySpan<char> path)
    {
        foreach (var ignoredSegment in IgnoredProjectSegments)
        {
            if (path.Contains(ignoredSegment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<string> GetAllProjects(
        string sourceDirectory,
        IEnumerable<string> directories,
        Func<string, bool> include = null)
    {
        directories ??= Directories;

        foreach (var directory in directories)
        {
            var fullDirectory = Path.Combine(sourceDirectory, directory);
            foreach (var file in Directory.EnumerateFiles(fullDirectory, "*.csproj", SearchOption.AllDirectories))
            {
                bool shouldInclude = include?.Invoke(file) ?? true;
                if (!shouldInclude)
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(sourceDirectory, file);
                if (ProjectContainsIgnoredSegment(relativePath))
                {
                    continue;
                }

                yield return file;
            }
        }
    }

    public static void TryDelete(string fileName)
    {
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }
    }
}
