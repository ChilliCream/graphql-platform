namespace HotChocolate.ApolloFederation;

/// <summary>
/// Enum defining all supported Apollo Federation v2 versions.
/// </summary>
public enum FederationVersion
{
    Unknown = 0,
    Federation10 = 1_0,
    Federation20 = 2_0,
    Federation21 = 2_1,
    Federation22 = 2_2,
    Federation23 = 2_3,
    Federation24 = 2_4,
    Federation25 = 2_5,
    Federation26 = 2_6,
    Latest = Federation26,
}

internal static class FederationVersionUrls
{
    public const string Federation20 = "https://specs.apollo.dev/federation/v2.0";
    public const string Federation21 = "https://specs.apollo.dev/federation/v2.1";
    public const string Federation22 = "https://specs.apollo.dev/federation/v2.2";
    public const string Federation23 = "https://specs.apollo.dev/federation/v2.3";
    public const string Federation24 = "https://specs.apollo.dev/federation/v2.4";
    public const string Federation25 = "https://specs.apollo.dev/federation/v2.5";
}