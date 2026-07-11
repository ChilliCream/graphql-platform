using HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.A;
using HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>interface-object-with-requires</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The <c>a</c> subgraph owns the
/// <c>NodeWithName</c> interface and its <c>User</c> implementation, while the
/// <c>b</c> subgraph contributes an <c>@interfaceObject</c> view of
/// <c>NodeWithName</c> whose <c>username</c> field <c>@requires</c> the external
/// <c>name</c> field owned by <c>a</c>.
/// </summary>
public sealed class InterfaceObjectWithRequiresTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    public Task AnotherUsers_SelectsRequiresUsername() => RunAsync(
        query: """
            query {
              anotherUsers {
                id
                name
                username
              }
            }
            """,
        expectedData: """
            {
              "anotherUsers": [
                {
                  "id": "u1",
                  "name": "u1-name",
                  "username": "u1-username"
                },
                {
                  "id": "u2",
                  "name": "u2-name",
                  "username": "u2-username"
                }
              ]
            }
            """);

    [Fact]
    public Task Users_SelectsRequiresUsername() => RunAsync(
        query: """
            query {
              users {
                id
                name
                username
              }
            }
            """,
        expectedData: """
            {
              "users": [
                {
                  "id": "u1",
                  "name": "u1-name",
                  "username": "u1-username"
                },
                {
                  "id": "u2",
                  "name": "u2-name",
                  "username": "u2-username"
                }
              ]
            }
            """);

    [Fact]
    public Task AnotherUsers_SelectsUserAge() => RunAsync(
        query: """
            query {
              anotherUsers {
                ... on User {
                  age
                }
              }
            }
            """,
        expectedData: """
            {
              "anotherUsers": [
                { "age": 11 },
                { "age": 22 }
              ]
            }
            """);

    [Fact]
    public Task Users_SelectsUserAge() => RunAsync(
        query: """
            query {
              users {
                ... on User {
                  age
                }
              }
            }
            """,
        expectedData: """
            {
              "users": [
                { "age": 11 },
                { "age": 22 }
              ]
            }
            """);

    [Fact]
    public Task AnotherUsers_SelectsInlineFragmentAndInterfaceFields() => RunAsync(
        query: """
            query {
              anotherUsers {
                ... on User {
                  age
                  id
                  name
                  username
                }
                id
                name
              }
            }
            """,
        expectedData: """
            {
              "anotherUsers": [
                {
                  "age": 11,
                  "id": "u1",
                  "name": "u1-name",
                  "username": "u1-username"
                },
                {
                  "age": 22,
                  "id": "u2",
                  "name": "u2-name",
                  "username": "u2-username"
                }
              ]
            }
            """);

    [Fact]
    public Task Users_SelectsInlineFragmentAndInterfaceFields() => RunAsync(
        query: """
            query {
              users {
                ... on User {
                  age
                  id
                  name
                  username
                }
                id
                name
              }
            }
            """,
        expectedData: """
            {
              "users": [
                {
                  "age": 11,
                  "id": "u1",
                  "name": "u1-name",
                  "username": "u1-username"
                },
                {
                  "age": 22,
                  "id": "u2",
                  "name": "u2-name",
                  "username": "u2-username"
                }
              ]
            }
            """);

    [Fact]
    public Task Users_SelectsInlineFragmentAndInterfaceFields_Repeated() => RunAsync(
        query: """
            query {
              users {
                ... on User {
                  age
                  id
                  name
                  username
                }
                id
                name
              }
            }
            """,
        expectedData: """
            {
              "users": [
                {
                  "age": 11,
                  "id": "u1",
                  "name": "u1-name",
                  "username": "u1-username"
                },
                {
                  "age": 22,
                  "id": "u2",
                  "name": "u2-name",
                  "username": "u2-username"
                }
              ]
            }
            """);
}
