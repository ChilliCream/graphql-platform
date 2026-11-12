using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionPolicyDirectiveTests : FusionTestBase
{
    private const string Directives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    private const string SchemaA =
        $$"""
        type Query @authenticated {
          secured: Int @shareable @requiresScopes(scopes: [["read"], ["admin"]])
          open: Int
          iface: FooInterface
          kind: FooEnum
          custom: FooScalar
        }

        interface FooInterface @policy(policies: [["Admin"]]) {
          id: Int
        }

        enum FooEnum @authenticated {
          ONE
        }

        scalar FooScalar @requiresScopes(scopes: [["scalar"]])

        {{Directives}}
        """;

    private const string SchemaB =
        $$"""
        type Query {
          secured: Int @shareable @policy(policies: [["Editor"], ["Admin", "Auditor"]])
        }

        {{Directives}}
        """;

    [Fact]
    public void Create_Should_ParsePolicy_When_MembersCarryFusionPolicy()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        var members = new (string Coordinate, PolicyDirective? Policy)[]
        {
            ("Query", query.Policy),
            ("Query.secured", query.Fields["secured"].Policy),
            ("Query.open", query.Fields["open"].Policy),
            ("FooInterface", schema.Types.GetType<FusionInterfaceTypeDefinition>("FooInterface").Policy),
            ("FooEnum", schema.Types.GetType<FusionEnumTypeDefinition>("FooEnum").Policy),
            ("FooScalar", schema.Types.GetType<FusionScalarTypeDefinition>("FooScalar").Policy)
        };

        // assert
        members
            .Select(static m => new
            {
                m.Coordinate,
                Policy = m.Policy is null
                    ? null
                    : new
                    {
                        m.Policy.Authenticated,
                        m.Policy.Scopes,
                        m.Policy.Policies
                    }
            })
            .ToArray()
            .MatchInlineSnapshot(
            """
            [
              {
                "Coordinate": "Query",
                "Policy": {
                  "Authenticated": true,
                  "Scopes": [],
                  "Policies": []
                }
              },
              {
                "Coordinate": "Query.secured",
                "Policy": {
                  "Authenticated": true,
                  "Scopes": [
                    [
                      "admin"
                    ],
                    [
                      "read"
                    ]
                  ],
                  "Policies": [
                    [
                      "Editor"
                    ],
                    [
                      "Admin",
                      "Auditor"
                    ]
                  ]
                }
              },
              {
                "Coordinate": "Query.open",
                "Policy": {
                  "Authenticated": true,
                  "Scopes": [],
                  "Policies": []
                }
              },
              {
                "Coordinate": "FooInterface",
                "Policy": {
                  "Authenticated": false,
                  "Scopes": [],
                  "Policies": [
                    [
                      "Admin"
                    ]
                  ]
                }
              },
              {
                "Coordinate": "FooEnum",
                "Policy": {
                  "Authenticated": true,
                  "Scopes": [],
                  "Policies": []
                }
              },
              {
                "Coordinate": "FooScalar",
                "Policy": {
                  "Authenticated": false,
                  "Scopes": [
                    [
                      "scalar"
                    ]
                  ],
                  "Policies": []
                }
              }
            ]
            """);
    }

    [Fact]
    public void Create_Should_SetHasPolicy_When_FieldCarriesOrInheritsFusionPolicy()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        var iface = schema.Types.GetType<FusionInterfaceTypeDefinition>("FooInterface");

        // assert
        new
        {
            Secured = query.Fields["secured"].HasPolicy,
            Open = query.Fields["open"].HasPolicy,
            InterfaceField = iface.Fields["id"].HasPolicy
        }.MatchInlineSnapshot(
            """
            {
              "Secured": true,
              "Open": true,
              "InterfaceField": true
            }
            """);
    }

    [Fact]
    public void Create_Should_SetHasPolicy_When_InterfaceFieldCarriesFusionPolicy()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              node: Node
            }

            interface Node {
              id: Int @authenticated
              name: String
            }

            type Item implements Node {
              id: Int @authenticated
              name: String
            }

            {{Directives}}
            """);

        // act
        var node = schema.Types.GetType<FusionInterfaceTypeDefinition>("Node");

        // assert
        new
        {
            Id = node.Fields["id"].HasPolicy,
            Name = node.Fields["name"].HasPolicy
        }.MatchInlineSnapshot(
            """
            {
              "Id": true,
              "Name": false
            }
            """);
    }

    [Fact]
    public void Create_Should_CollectPolicyUsage_When_SchemaUsesPolicy()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var usage = schema.Features.Get<FusionPolicyUsage>()!;

        // assert
        new
        {
            usage.IsUsed,
            usage.UsesAuthenticated,
            usage.UsesScopes,
            usage.UsesPolicies,
            usage.PolicyNames
        }.MatchInlineSnapshot(
            """
            {
              "IsUsed": true,
              "UsesAuthenticated": true,
              "UsesScopes": true,
              "UsesPolicies": true,
              "PolicyNames": [
                "Admin",
                "Auditor",
                "Editor"
              ]
            }
            """);
    }

    [Fact]
    public void Create_Should_ReportNoPolicyUsage_When_SchemaHasNoPolicy()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field: Int
            }
            """);

        // act
        var usage = schema.Features.Get<FusionPolicyUsage>()!;

        // assert
        new
        {
            usage.IsUsed,
            usage.UsesAuthenticated,
            usage.UsesScopes,
            usage.UsesPolicies,
            usage.PolicyNames
        }.MatchInlineSnapshot(
            """
            {
              "IsUsed": false,
              "UsesAuthenticated": false,
              "UsesScopes": false,
              "UsesPolicies": false,
              "PolicyNames": []
            }
            """);
    }

    [Fact]
    public void Create_Should_HideFusionPolicy_When_LoadingSchema()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");

        // assert
        new
        {
            DefinitionLoaded = schema.DirectiveDefinitions.ContainsName("fusion__policy"),
            TypeDirectives = query.Directives.WithInternals.AsEnumerable().Select(static d => d.Name).ToArray(),
            FieldDirectives = query.Fields["secured"].Directives.WithInternals
                .AsEnumerable()
                .Select(static d => d.Name)
                .ToArray()
        }.MatchInlineSnapshot(
            """
            {
              "DefinitionLoaded": false,
              "TypeDirectives": [],
              "FieldDirectives": []
            }
            """);
    }

    [Fact]
    public void Parse_Should_Throw_When_ScopesIsNotAListOfLists()
    {
        // arrange
        var directive = Utf8GraphQLParser.Syntax
            .ParseObjectTypeDefinition("type Foo @fusion__policy(scopes: [\"read\"]) { a: Int }")
            .Directives[0];

        // act
        var exception = Record.Exception(() => PolicyDirectiveParser.Parse(directive));

        // assert
        Assert.IsType<DirectiveParserException>(exception);
        Assert.Equal(
            "The `scopes` argument of @fusion__policy must be a list of string lists.",
            exception.Message);
    }
}
