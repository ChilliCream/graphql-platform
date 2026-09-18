namespace HotChocolate.Serialization;

/// <summary>
/// Provides the supported GraphQL specification editions and their wire names.
/// </summary>
public static class GraphQLSpecVersions
{
    private static readonly IReadOnlyList<string> s_supportedValues =
        Array.AsReadOnly(["october-2021", "september-2025"]);

    /// <summary>
    /// Gets the supported GraphQL specification edition wire names.
    /// </summary>
    public static IReadOnlyList<string> SupportedValues => s_supportedValues;

    /// <summary>
    /// Parses a GraphQL specification edition wire name.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="version">The parsed specification edition, or the default value when parsing fails.</param>
    /// <returns><c>true</c> when <paramref name="value"/> identifies a supported specification edition.</returns>
    public static bool TryParse(string? value, out GraphQLSpecVersion version)
    {
        if (value is not null)
        {
            var trimmedValue = value.AsSpan().Trim();

            if (trimmedValue.Equals("october-2021", StringComparison.OrdinalIgnoreCase)
                || trimmedValue.Equals("2021-10", StringComparison.OrdinalIgnoreCase))
            {
                version = GraphQLSpecVersion.October2021;
                return true;
            }

            if (trimmedValue.Equals("september-2025", StringComparison.OrdinalIgnoreCase)
                || trimmedValue.Equals("2025-09", StringComparison.OrdinalIgnoreCase))
            {
                version = GraphQLSpecVersion.September2025;
                return true;
            }
        }

        version = default;
        return false;
    }

    /// <summary>
    /// Gets the canonical wire name for a GraphQL specification edition.
    /// </summary>
    /// <param name="version">The specification edition.</param>
    /// <returns>The canonical wire name.</returns>
    public static string GetWireName(GraphQLSpecVersion version)
        => version switch
        {
            GraphQLSpecVersion.October2021 => s_supportedValues[0],
            GraphQLSpecVersion.September2025 => s_supportedValues[1],
            _ => throw ThrowHelper.GraphQLSpecVersions_UnknownVersion(version)
        };
}
