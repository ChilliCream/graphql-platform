using HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;
using HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>corrupted-supergraph-node-id</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two subgraphs (<c>a</c>, <c>b</c>)
/// each expose <c>node(id: ID!): Node @shareable</c> but intentionally return
/// corrupted (wrong) IDs for the entity types they do not own. The tests verify
/// that the gateway handles these corrupted IDs correctly.
/// </summary>
public sealed class CorruptedSupergraphNodeIdTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
            (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync));

    [Fact]
    public Task Node_Account_Id_Only() => RunAsync(
        query: """
            {
              node(id: "a1") {
                id
              }
            }
            """,
        expectsErrors: true);

    [Fact(Skip = "Gateway composition drops shareable query fields returning interface types.")]
    public Task Node_Account_And_Chat_Typename() => RunAsync(
        query: """
            {
              account: node(id: "a1") {
                __typename
              }
              chat: node(id: "c1") {
                __typename
              }
            }
            """,
        expectedData: """
            {
              "account": {
                "__typename": "Account"
              },
              "chat": {
                "__typename": "Chat"
              }
            }
            """);

    [Fact(Skip = "Gateway composition drops shareable query fields returning interface types.")]
    public Task Node_Account_And_Chat_Full_Fragments() => RunAsync(
        query: """
            {
              account: node(id: "a1") {
                ... on Account {
                  id
                  username
                }
              }
              chat: node(id: "c1") {
                ... on Chat {
                  id
                  text
                }
              }
            }
            """,
        expectedData: """
            {
              "account": {
                "id": "a1",
                "username": "a1-username"
              },
              "chat": {
                "id": "c1",
                "text": "c1-text"
              }
            }
            """);

    [Fact(Skip = "Gateway composition drops shareable query fields returning interface types.")]
    public Task Node_Account_As_Chat_And_Chat_As_Account() => RunAsync(
        query: """
            {
              account: node(id: "a1") {
                ... on Chat {
                  id
                }
              }
              chat: node(id: "c1") {
                ... on Account {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "account": {},
              "chat": {}
            }
            """);

    [Fact(Skip = "Gateway composition drops shareable query fields returning interface types.")]
    public Task Node_Mismatched_Fragments_With_Typename() => RunAsync(
        query: """
            {
              account: node(id: "a1") {
                __typename
                ... on Chat {
                  id
                }
              }
              chat: node(id: "c1") {
                __typename
                ... on Account {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "account": {
                "__typename": "Account"
              },
              "chat": {
                "__typename": "Chat"
              }
            }
            """);

    [Fact]
    public Task Node_Account_Id_With_Mismatched_Chat_Fragment() => RunAsync(
        query: """
            {
              account: node(id: "a1") {
                id
                ... on Chat {
                  id
                }
              }
              chat: node(id: "c1") {
                __typename
                ... on Account {
                  id
                }
              }
            }
            """,
        expectsErrors: true);

    [Fact]
    public Task Chat_By_Id() => RunAsync(
        query: """
            {
              chat(id: "c1") {
                id
              }
            }
            """,
        expectedData: """
            {
              "chat": {
                "id": "c1"
              }
            }
            """);

    [Fact]
    public Task Account_By_Id() => RunAsync(
        query: """
            {
              account(id: "a1") {
                id
              }
            }
            """,
        expectedData: """
            {
              "account": {
                "id": "a1"
              }
            }
            """);

    [Fact]
    public Task Chat_With_Account() => RunAsync(
        query: """
            {
              chat(id: "c1") {
                id
                text
                account {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "chat": {
                "id": "c1",
                "text": "c1-text",
                "account": {
                  "id": "a1"
                }
              }
            }
            """);

    [Fact]
    public Task Account_With_Chats() => RunAsync(
        query: """
            {
              account(id: "a1") {
                id
                username
                chats {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "account": {
                "id": "a1",
                "username": "a1-username",
                "chats": [
                  {
                    "id": "c1"
                  }
                ]
              }
            }
            """);
}
