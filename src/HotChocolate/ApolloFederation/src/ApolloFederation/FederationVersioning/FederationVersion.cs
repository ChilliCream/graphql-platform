namespace HotChocolate.ApolloFederation;

/// <summary>
/// Enum defining all supported Apollo Federation v2 versions.
/// </summary>
public enum FederationVersion
{
    Federation20 = 0,
    Federation21 = 1,
    Federation22 = 2,
    Federation23 = 3,
    Federation24 = 4,
    Federation25 = 5,
    Federation26 = 6,
    Latest = Federation25,
    Count,
}
