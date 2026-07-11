using HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;
using HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;
using HotChocolate.Fusion.Suites.SimpleInterfaceObject.C;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>simple-interface-object</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Subgraph <c>a</c> owns the
/// <c>NodeWithName</c> and <c>Account</c> interfaces with their concrete
/// <c>User</c>, <c>Admin</c>, and <c>Regular</c> implementers, while subgraphs
/// <c>b</c> and <c>c</c> extend those interfaces via <c>@interfaceObject</c> to
/// contribute the <c>username</c>, <c>name</c>, and <c>isActive</c> fields.
/// </summary>
public sealed class SimpleInterfaceObjectTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    [Fact]
    public Task AnotherUsers_Resolves_Name_And_Username() => RunAsync(
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
    public Task Users_Resolves_Name_And_Username() => RunAsync(
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
    public Task AnotherUsers_Resolves_Age_On_User_Fragment() => RunAsync(
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
                {
                  "age": 11
                },
                {
                  "age": 22
                }
              ]
            }
            """);

    [Fact]
    public Task Users_Resolves_Age_On_User_Fragment() => RunAsync(
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
                {
                  "age": 11
                },
                {
                  "age": 22
                }
              ]
            }
            """);

    [Fact]
    public Task AnotherUsers_Resolves_Full_User_Fragment_And_Interface_Fields() => RunAsync(
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
    public Task Users_Resolves_Full_User_Fragment_And_Interface_Fields() => RunAsync(
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
    public Task Users_Resolves_Full_User_Fragment_And_Interface_Fields_Repeated() => RunAsync(
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
    public Task Accounts_Resolves_Name_From_InterfaceObject() => RunAsync(
        query: """
            query {
              accounts {
                name
              }
            }
            """,
        expectedData: """
            {
              "accounts": [
                {
                  "name": "Alice"
                },
                {
                  "name": "Bob"
                },
                {
                  "name": "Charlie"
                }
              ]
            }
            """);

    [Fact]
    public Task Accounts_Resolves_Name_On_Admin_Fragment() => RunAsync(
        query: """
            query {
              accounts {
                ... on Admin {
                  name
                }
              }
            }
            """,
        expectedData: """
            {
              "accounts": [
                {
                  "name": "Alice"
                },
                {
                  "name": "Bob"
                },
                {}
              ]
            }
            """);

    [Fact]
    public Task Accounts_Resolves_Name_And_Concrete_Typename() => RunAsync(
        query: """
            query {
              accounts {
                name
                __typename
              }
            }
            """,
        expectedData: """
            {
              "accounts": [
                {
                  "name": "Alice",
                  "__typename": "Admin"
                },
                {
                  "name": "Bob",
                  "__typename": "Admin"
                },
                {
                  "name": "Charlie",
                  "__typename": "Regular"
                }
              ]
            }
            """);

    [Fact]
    public Task Accounts_Resolves_Typename_On_Admin_Fragment() => RunAsync(
        query: """
            query {
              accounts {
                ... on Admin {
                  __typename
                }
              }
            }
            """,
        expectedData: """
            {
              "accounts": [
                {
                  "__typename": "Admin"
                },
                {
                  "__typename": "Admin"
                },
                {}
              ]
            }
            """);

    [Fact]
    public Task Accounts_Resolves_IsActive_From_InterfaceObject() => RunAsync(
        query: """
            query {
              accounts {
                id
                isActive
              }
            }
            """,
        expectedData: """
            {
              "accounts": [
                {
                  "id": "u1",
                  "isActive": false
                },
                {
                  "id": "u2",
                  "isActive": false
                },
                {
                  "id": "u3",
                  "isActive": false
                }
              ]
            }
            """);

    [Fact]
    public Task Accounts_Resolves_IsActive_On_Admin_Fragment() => RunAsync(
        query: """
            query {
              accounts {
                id
                ... on Admin {
                  isActive
                }
              }
            }
            """,
        expectedData: """
            {
              "accounts": [
                {
                  "id": "u1",
                  "isActive": true
                },
                {
                  "id": "u2",
                  "isActive": true
                },
                {
                  "id": "u3"
                }
              ]
            }
            """);
}
