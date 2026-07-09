namespace HotChocolate.Fusion.Composition.Benchmarks;

/// <summary>
/// Resolves the paths of the large federated graph benchmark corpus (the big-federated-graphs checkout) without
/// hard-coding machine-specific locations. The corpus root is taken from the
/// <c>BIG_FEDERATED_GRAPHS</c> environment variable when set; otherwise the directories above the
/// benchmark binary are searched for a sibling <c>big-federated-graphs</c> checkout.
/// </summary>
internal static class CorpusPaths
{
    private const string EnvironmentVariable = "BIG_FEDERATED_GRAPHS";
    private const string CorpusDirectoryName = "big-federated-graphs";

    private static readonly Lazy<string> s_root = new(ResolveRoot);

    public static string SubgraphsPath
        => System.IO.Path.Combine(s_root.Value, "schemas", "edge0-v2", "subgraphs");

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
            $"The large federated graph benchmark corpus was not found. Set the {EnvironmentVariable} "
            + $"environment variable to the {CorpusDirectoryName} checkout, or place the checkout "
            + "next to an ancestor directory of the benchmark binary.");
    }
}
