using System;
using System.IO;

namespace HotChocolate.Fusion.Benchmarks;

/// <summary>
/// Resolves the paths of the Netflix benchmark corpus (the big-federated-graphs checkout) without
/// hard-coding machine-specific locations. The corpus root is taken from the
/// <c>BIG_FEDERATED_GRAPHS</c> environment variable when set; otherwise the directories above the
/// benchmark binary are searched for a sibling <c>big-federated-graphs</c> checkout.
/// </summary>
internal static class NetflixCorpusPaths
{
    private const string EnvironmentVariable = "BIG_FEDERATED_GRAPHS";
    private const string CorpusDirectoryName = "big-federated-graphs";

    private static readonly Lazy<string> s_root = new(ResolveRoot);

    public static string SchemaPath
        => System.IO.Path.Combine(s_root.Value, "schemas", "edge0-v2", "composed-fusion.graphqls");

    public static string Query1Path
        => System.IO.Path.Combine(s_root.Value, "schemas", "edge0", "operations", "Query1.graphql");

    public static string Query2Path
        => System.IO.Path.Combine(s_root.Value, "schemas", "edge0", "operations", "Query2.graphql");

    private static string ResolveRoot()
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable);

        if (!string.IsNullOrEmpty(configured))
        {
            return configured;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = System.IO.Path.Combine(current.FullName, CorpusDirectoryName);

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            $"The Netflix benchmark corpus was not found. Set the {EnvironmentVariable} "
            + $"environment variable to the {CorpusDirectoryName} checkout, or place the checkout "
            + "next to an ancestor directory of the benchmark binary.");
    }
}
