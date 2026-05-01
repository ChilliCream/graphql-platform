using HotChocolate.Fusion.Suites.Typename.A;
using HotChocolate.Fusion.Suites.Typename.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>typename</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs cooperate: subgraph <c>a</c> declares the <c>Product</c>
/// union, the <c>Node</c> interface, the <c>User</c> interface (with
/// <c>@key</c>), and the <c>Admin</c> concrete entity; subgraph <c>b</c>
/// declares <c>type User @key(fields: "id") @interfaceObject</c> to
/// abstractly extend <c>a</c>'s <c>User</c> interface with a <c>name</c>
/// field. The audit cases probe <c>__typename</c> resolution on union and
/// interface roots, plus the <c>@interfaceObject</c> contribution.
/// </summary>
public sealed class TypenameTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    private const string InterfaceObjectGapSkipReason =
        "Composer rejects @interfaceObject as an unsupported directive. "
        + "See framework gap typename in /workspaces/repo/.work/implement/framework-gaps.md "
        + "(Fusion.Composition/ApolloFederation/FederationSchemaAnalyzer.cs:23 / "
        + "RemoveFederationInfrastructure.cs:22). Phase E feature work, intentionally out "
        + "of scope for the test enablement pass.";

    /// <summary>
    /// Reads <c>__typename</c> twice (raw and aliased) from the union root.
    /// Subgraph <c>a</c> resolves the union to an <see cref="Oven"/>.
    /// </summary>
    [Fact(Skip = InterfaceObjectGapSkipReason)]
    public Task Union_Reports_Typename_Twice() => RunAsync(
        query: """
            query {
              union {
                __typename
                typename: __typename
              }
            }
            """,
        expectedData: """
            {
              "union": { "__typename": "Oven", "typename": "Oven" }
            }
            """);

    /// <summary>
    /// Reads <c>id</c>, <c>__typename</c>, and two aliases of
    /// <c>__typename</c> from the interface root. Subgraph <c>a</c>
    /// resolves the interface to a <see cref="Toaster"/>.
    /// </summary>
    [Fact(Skip = InterfaceObjectGapSkipReason)]
    public Task Interface_Reports_Id_And_Typename_Aliases() => RunAsync(
        query: """
            query {
              interface {
                id
                __typename
                typename: __typename
                t: __typename
              }
            }
            """,
        expectedData: """
            {
              "interface": { "id": "2", "__typename": "Toaster", "typename": "Toaster", "t": "Toaster" }
            }
            """);

    /// <summary>
    /// Reads <c>__typename</c> via inline fragments on each member of the
    /// union. Only the matching fragment contributes its alias.
    /// </summary>
    [Fact(Skip = InterfaceObjectGapSkipReason)]
    public Task Union_Reports_Typename_Through_Inline_Fragments() => RunAsync(
        query: """
            query {
              union {
                __typename
                ... on Oven { typename: __typename }
                ... on Toaster { typename: __typename }
              }
            }
            """,
        expectedData: """
            {
              "union": { "__typename": "Oven", "typename": "Oven" }
            }
            """);

    /// <summary>
    /// Reads <c>__typename</c> via inline fragments on the interface root.
    /// </summary>
    [Fact(Skip = InterfaceObjectGapSkipReason)]
    public Task Interface_Reports_Typename_Through_Inline_Fragments() => RunAsync(
        query: """
            query {
              interface {
                __typename
                ... on Oven { typename: __typename }
                ... on Toaster { typename: __typename }
              }
            }
            """,
        expectedData: """
            {
              "interface": { "__typename": "Toaster", "typename": "Toaster" }
            }
            """);

    /// <summary>
    /// Reads only <c>id</c> from the <c>users</c> root field on subgraph
    /// <c>b</c>. The query never escapes the <c>@interfaceObject</c>, so
    /// no entity calls are required.
    /// </summary>
    [Fact(Skip = InterfaceObjectGapSkipReason)]
    public Task Users_Returns_Ids_From_InterfaceObject_Subgraph() => RunAsync(
        query: """
            query {
              users { id }
            }
            """,
        expectedData: """
            {
              "users": [ { "id": "u1" }, { "id": "u2" } ]
            }
            """);

    /// <summary>
    /// Reads <c>__typename</c> from the <c>users</c> root field. Apollo's
    /// reference enriches each user's <c>__typename</c> by routing into
    /// subgraph <c>a</c> (which owns the concrete type) so the response
    /// reads <c>"Admin"</c>.
    /// </summary>
    [Fact(Skip = InterfaceObjectGapSkipReason)]
    public Task Users_Returns_Concrete_Typename_From_InterfaceObject() => RunAsync(
        query: """
            query {
              users { __typename }
            }
            """,
        expectedData: """
            {
              "users": [ { "__typename": "Admin" }, { "__typename": "Admin" } ]
            }
            """);
}
