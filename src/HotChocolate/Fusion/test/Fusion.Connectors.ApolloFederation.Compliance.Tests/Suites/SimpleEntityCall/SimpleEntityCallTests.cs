using HotChocolate.Fusion.Suites.SimpleEntityCall.Email;
using HotChocolate.Fusion.Suites.SimpleEntityCall.Nickname;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>simple-entity-call</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The gateway composes an
/// <c>email</c> subgraph (which owns <c>User.id</c> and <c>User.email</c>) and a
/// <c>nickname</c> subgraph (which extends <c>User</c> by <c>email</c> and owns
/// <c>User.nickname</c>). A single query touches both subgraphs via an entity call.
/// </summary>
public sealed class SimpleEntityCallTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (EmailSubgraph.Name, EmailSubgraph.BuildAsync),
            (NicknameSubgraph.Name, NicknameSubgraph.BuildAsync));

    [Fact]
    public Task User_Nickname() => RunAsync(
        query: """
            {
              user { id nickname }
            }
            """,
        expectedData: """
            {
              "user": { "id": "1", "nickname": "user1" }
            }
            """);
}
