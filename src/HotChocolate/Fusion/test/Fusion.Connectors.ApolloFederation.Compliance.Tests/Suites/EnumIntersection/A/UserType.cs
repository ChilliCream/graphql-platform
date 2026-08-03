namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// The <c>UserType</c> enum as projected by the <c>a</c> subgraph. The runtime
/// enum carries <c>ANONYMOUS</c> (the audit data source stores it for
/// <c>u2</c>), but subgraph <c>a</c> only exposes <c>REGULAR</c> in its GraphQL
/// schema, so serializing <c>ANONYMOUS</c> is expected to fail at the subgraph.
/// </summary>
public enum UserTypeEnum
{
    REGULAR,
    ANONYMOUS
}
