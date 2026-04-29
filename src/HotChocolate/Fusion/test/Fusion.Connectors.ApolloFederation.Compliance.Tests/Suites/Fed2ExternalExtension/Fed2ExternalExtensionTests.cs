using HotChocolate.Fusion.Suites.Fed2ExternalExtension.A;
using HotChocolate.Fusion.Suites.Fed2ExternalExtension.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>fed2-external-extension</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs share the <c>User</c> entity. Subgraph <c>a</c> uses the
/// Federation v2 <c>extend type User @key(...)</c> form together with
/// <c>@external</c> markers to project only its own field <c>rid</c>;
/// subgraph <c>b</c> owns the rest of the entity. The audit verifies that
/// the gateway can route the external <c>name</c> field to <c>b</c>, and
/// that subgraph <c>a</c>'s <c>providedRandomUser</c> field lets
/// <c>@provides(fields: "name")</c> ship <c>name</c> alongside the
/// reference so no entity call to <c>b</c> is needed.
/// </summary>
public sealed class Fed2ExternalExtensionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// Combined query: <c>randomUser</c> from subgraph <c>a</c> needs the
    /// external <c>name</c> from <c>b</c> via the entity lookup;
    /// <c>userById</c> resolves entirely in <c>b</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer generates a 'userById' lookup field from User @key(\"id\") that collides with subgraph b's user-declared Query.userById root field. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task RandomUser_And_UserById_Resolve_Across_Subgraphs() => RunAsync(
        query: """
            query {
              randomUser {
                id
                name
              }
              userById(id: "u2") {
                id
                name
                nickname
              }
            }
            """,
        expectedData: """
            {
              "randomUser": {
                "id": "u1",
                "name": "u1-name"
              },
              "userById": {
                "id": "u2",
                "name": "u2-name",
                "nickname": "u2-nickname"
              }
            }
            """);

    /// <summary>
    /// <c>randomUser</c> with <c>id</c> and <c>rid</c> only stays inside
    /// subgraph <c>a</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer generates a 'userById' lookup field from User @key(\"id\") that collides with subgraph b's user-declared Query.userById root field. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task RandomUser_Returns_Local_Rid_Without_Entity_Call() => RunAsync(
        query: """
            query {
              randomUser {
                id
                rid
              }
            }
            """,
        expectedData: """
            {
              "randomUser": {
                "id": "u1",
                "rid": "u1-rid"
              }
            }
            """);

    /// <summary>
    /// <c>randomUser</c> with the external <c>name</c> selection forces
    /// the gateway to issue an entity call to subgraph <c>b</c> for
    /// <c>name</c>, then merge with <c>rid</c> from subgraph <c>a</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer generates a 'userById' lookup field from User @key(\"id\") that collides with subgraph b's user-declared Query.userById root field. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task RandomUser_Resolves_External_Name_Via_Entity_Call() => RunAsync(
        query: """
            query {
              randomUser {
                id
                rid
                name
              }
            }
            """,
        expectedData: """
            {
              "randomUser": {
                "id": "u1",
                "rid": "u1-rid",
                "name": "u1-name"
              }
            }
            """);

    /// <summary>
    /// <c>providedRandomUser</c> uses
    /// <c>@provides(fields: "name")</c> so the gateway should accept the
    /// <c>name</c> shipped from subgraph <c>a</c> and skip the entity
    /// call to subgraph <c>b</c>.
    /// </summary>
    [Fact(Skip = "Federation transformer generates a 'userById' lookup field from User @key(\"id\") that collides with subgraph b's user-declared Query.userById root field. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task ProvidedRandomUser_Uses_Local_Name_Via_Provides() => RunAsync(
        query: """
            query {
              providedRandomUser {
                id
                rid
                name
              }
            }
            """,
        expectedData: """
            {
              "providedRandomUser": {
                "id": "u1",
                "rid": "u1-rid",
                "name": "u1-name"
              }
            }
            """);
}
