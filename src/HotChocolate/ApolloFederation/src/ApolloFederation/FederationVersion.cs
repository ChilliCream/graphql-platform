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
    // Federation26 = 2_6,
    Latest = Federation25,
}