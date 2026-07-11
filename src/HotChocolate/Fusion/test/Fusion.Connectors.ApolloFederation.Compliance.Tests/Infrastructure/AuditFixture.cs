using System.Reflection;

namespace HotChocolate.Fusion;

/// <summary>
/// Reads embedded reference files (<c>tests.json</c>, <c>data.json</c>,
/// <c>subgraphs/*.graphql</c>) vendored from the federation-gateway-audit repository
/// under each suite's <c>Reference/</c> directory. Reference resources are material
/// for porting a suite; the live tests inline their query literals via
/// <see cref="ComplianceTestBase.RunAsync"/>.
/// </summary>
internal static class AuditFixture
{
    private const string SuitesPath = "Suites";
    private const string ReferencePath = "Reference";
    private static readonly Assembly s_assembly = typeof(AuditFixture).Assembly;
    private static readonly string s_assemblyName = s_assembly.GetName().Name!;

    /// <summary>
    /// Reads an embedded reference text file for the specified audit suite.
    /// </summary>
    /// <param name="suiteName">
    /// The suite's PascalCase folder name under <c>Suites/</c>
    /// (e.g. <c>"SimpleEntityCall"</c>).
    /// </param>
    /// <param name="relativePath">
    /// The path under the suite's <c>Reference/</c> directory
    /// (e.g. <c>"tests.json"</c> or <c>"email.graphql"</c>). Forward slashes are
    /// translated to embedded-resource path separators.
    /// </param>
    /// <returns>The UTF-8 contents of the embedded resource.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the expected embedded resource cannot be located.
    /// </exception>
    public static string LoadText(string suiteName, string relativePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(suiteName);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        var resourceName = BuildResourceName(suiteName, relativePath);

        using var stream = s_assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Embedded reference resource '{resourceName}' was not found.",
                resourceName);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string BuildResourceName(string suiteName, string relativePath)
    {
        // Embedded resource names replace directory separators with '.', so
        // 'subgraphs/email.graphql' becomes 'subgraphs.email.graphql'.
        var normalized = relativePath.Replace('/', '.').Replace('\\', '.');
        return $"{s_assemblyName}.{SuitesPath}.{suiteName}.{ReferencePath}.{normalized}";
    }
}
