using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Policies.InMemory;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Serialization;

public class JsonOperationPlanSerializationTests : FusionTestBase
{
    [Fact]
    public void Parse_Plan()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
                estimatedDelivery(postCode: "12345")
            }
            """);
        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
        foreach (var original in originalPlan.AllNodes.OfType<OperationExecutionNode>())
        {
            var parsed = parsedPlan.AllNodes.OfType<OperationExecutionNode>().Single(t => t.Id == original.Id);
            Assert.Equal(original.Operation.Value.ToArray(), parsed.Operation.Value.ToArray());
            Assert.Equal(original.Operation.Hash, parsed.Operation.Hash);
        }
    }

    private const string PolicyDirectives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Parse_Should_RoundTripPolicyPlan_When_ProtectedFieldsAreDeferred()
    {
        // arrange
        var compositeSchema = ComposeSchema(
            $$"""
            # name: a
            type Query {
              me: String @authenticated
              user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
              name: String
              secret: String @requiresScopes(scopes: [["read"], ["admin", "write"]])
            }

            {{PolicyDirectives}}
            """,
            $$"""
            # name: b
            type Query {
              userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
              email: String! @authenticated
              budget: Int @policy(policies: [["finance"]])
              nickname: String
            }

            {{PolicyDirectives}}
            """);
        var originalPlan = PlanWithPooledFields(
            compositeSchema,
            "{ me user(id: 1) { name secret ... @defer { email budget nickname } } }",
            TestPolicyResolver.Create());
        var (originalJson, parsedPlan) = RoundTrip(compositeSchema, originalPlan);

        // act
        var parsedJson = new JsonOperationPlanFormatter(s_indentedOptions).Format(parsedPlan);

        // assert
        Assert.Equal(originalJson, parsedJson);
        parsedJson.MatchInlineSnapshot(
            """
            {
              "version": "1.3.0",
              "id": "3d4ae8bf9805fddd74ade860c3c6ed54b3421658fcd4e005ee243af2842ae9a9",
              "operation": {
                "kind": "Query",
                "document": "{ me user(id: 1) { name secret id @fusion__requirement } }",
                "id": "123456789101112",
                "hash": "123456789101112",
                "shortHash": "123456789101112"
              },
              "searchSpace": 1,
              "expandedNodes": 1,
              "nodes": [
                {
                  "id": 1,
                  "type": "Operation",
                  "schema": "a",
                  "operation": {
                    "name": "Op_123456789101112_1",
                    "kind": "Query",
                    "document": "query Op_123456789101112_1($__fusion_policy_3: Boolean!, $__fusion_policy_4: Boolean!) { me @skip(if: $__fusion_policy_3) user(id: 1) { name secret @skip(if: $__fusion_policy_4) id } }",
                    "hash": "28bc7fb556ec6b9c333666aa18147b93ebec07045a3dbfc7cc680d881c659480",
                    "shortHash": "28bc7fb5",
                    "xxHash": 4965678863524489945
                  },
                  "resultSelectionSet": "{ me user }",
                  "forwardedVariables": [
                    "__fusion_policy_3",
                    "__fusion_policy_4"
                  ]
                }
              ],
              "deliveryGroups": [
                {
                  "id": 0,
                  "path": "$.user"
                }
              ],
              "incrementalPlans": [
                {
                  "deliveryGroupIds": [
                    0
                  ],
                  "parentNodeId": 1,
                  "requirements": [
                    {
                      "name": "__fusion_1_id",
                      "type": "ID!",
                      "path": "$.user",
                      "selectionMap": "id"
                    }
                  ],
                  "operation": {
                    "kind": "Query",
                    "document": "{ user(id: 1) { email budget nickname id @fusion__requirement } }",
                    "id": "123456789101112#defer_0",
                    "hash": "123456789101112#defer_0",
                    "shortHash": "123456789101112"
                  },
                  "nodes": [
                    {
                      "id": 1,
                      "type": "Operation",
                      "schema": "b",
                      "operation": {
                        "name": "Op_defer_1",
                        "kind": "Query",
                        "document": "query Op_defer_1($__fusion_policy_1: Boolean!, $__fusion_policy_2: Boolean!, $__fusion_1_id: ID!) { userById(id: $__fusion_1_id) { email @skip(if: $__fusion_policy_1) budget @skip(if: $__fusion_policy_2) nickname } }",
                        "hash": "729ffbe7d33e5f56d69e931732f144c0b5f9b54b9bd37688453b91c68814bda1",
                        "shortHash": "729ffbe7",
                        "xxHash": 15130587353675677491
                      },
                      "lookupTypeName": "User",
                      "resultSelectionSet": "{ email budget nickname }",
                      "source": "$.userById",
                      "target": "$.user",
                      "requirements": [
                        {
                          "name": "__fusion_1_id",
                          "type": "ID!",
                          "path": "$.user",
                          "selectionMap": "id"
                        }
                      ],
                      "forwardedVariables": [
                        "__fusion_policy_1",
                        "__fusion_policy_2"
                      ],
                      "dependencies": [
                        {
                          "parentNodeId": 1
                        }
                      ]
                    }
                  ]
                }
              ],
              "policies": {
                "descriptors": [
                  {
                    "directiveName": "authenticated",
                    "selection": {
                      "path": "$.me"
                    }
                  },
                  {
                    "directiveName": "requiresScopes",
                    "scopes": [
                      [
                        "read"
                      ],
                      [
                        "admin",
                        "write"
                      ]
                    ],
                    "selection": {
                      "path": "$.user.secret"
                    }
                  },
                  {
                    "directiveName": "authenticated",
                    "selection": {
                      "incrementalPlan": 0,
                      "path": "$.user.email"
                    }
                  },
                  {
                    "directiveName": "policy",
                    "policyName": "finance",
                    "selection": {
                      "incrementalPlan": 0,
                      "path": "$.user.budget"
                    }
                  }
                ],
                "variables": [
                  {
                    "name": "__fusion_policy_3",
                    "selections": [
                      {
                        "path": "$.me"
                      }
                    ]
                  },
                  {
                    "name": "__fusion_policy_4",
                    "selections": [
                      {
                        "path": "$.user.secret"
                      }
                    ]
                  },
                  {
                    "name": "__fusion_policy_1",
                    "selections": [
                      {
                        "incrementalPlan": 0,
                        "path": "$.user.email"
                      }
                    ]
                  },
                  {
                    "name": "__fusion_policy_2",
                    "selections": [
                      {
                        "incrementalPlan": 0,
                        "path": "$.user.budget"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public void Parse_Should_ResolveProtectedSelection_When_DocumentOrderCompilationAssignsItsIdToAnotherSelection()
    {
        // arrange
        // document-order compilation gives the planned id of the protected selection to another selection
        var compositeSchema = ComposeSchema(
            $$"""
            type Query {
              a: A
              b: B
            }

            type A {
              id: ID!
              c: C
            }

            type C {
              k: String
            }

            type B {
              id: ID!
              d: D
            }

            type D {
              m: String @authenticated
              z: String
            }

            {{PolicyDirectives}}
            """);
        var originalPlan = PlanWithPooledFields(
            compositeSchema,
            "{ a { c { k } } b { d { m z } } }",
            TestPolicyResolver.Create());
        var (_, parsedPlan) = RoundTrip(compositeSchema, originalPlan);
        var originalSelection = Assert.IsType<Selection>(
            Assert.Single(originalPlan.Policies!.Descriptors).Selection);

        // an operation that compiles the selection sets in document order assigns other ids
        var documentOrderOperation = new OperationCompiler(
                compositeSchema,
                new DefaultObjectPoolProvider().CreateFieldMapPool())
            .Compile("id", "hash", "short", originalPlan.Operation.Definition);

        // act
        var selection = Assert.IsType<Selection>(Assert.Single(parsedPlan.Policies!.Descriptors).Selection);

        // assert
        Assert.Same(parsedPlan.Operation, selection.DeclaringSelectionSet.DeclaringOperation);
        new
        {
            PlannedId = originalSelection.Id,
            ParsedId = selection.Id,
            DocumentOrderIdOfAck = GetSelection(documentOrderOperation, "a", "c", "k").Id,
            DocumentOrderIdOfBdm = GetSelection(documentOrderOperation, "b", "d", "m").Id,
            Parsed = selection.ResponseName,
            ParsedDeclaringType = selection.DeclaringSelectionSet.Type.Name
        }.MatchInlineSnapshot(
            """
            {
              "PlannedId": 7,
              "ParsedId": 7,
              "DocumentOrderIdOfAck": 7,
              "DocumentOrderIdOfBdm": 11,
              "Parsed": "m",
              "ParsedDeclaringType": "D"
            }
            """);
    }

    [Fact]
    public void Parse_Should_RoundTripPolicyVariableOperands_When_RootSelectionsHaveDifferentVariables()
    {
        // arrange
        var compositeSchema = ComposeSchema(
            $$"""
            # name: a
            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            {{PolicyDirectives}}
            """,
            $$"""
            # name: b
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Int @authenticated
              note: String @requiresScopes(scopes: [["read"]])
            }

            {{PolicyDirectives}}
            """);
        var originalPlan = PlanWithPooledFields(
            compositeSchema,
            "{ products { price note } }",
            TestPolicyResolver.Create());
        var (originalJson, parsedPlan) = RoundTrip(compositeSchema, originalPlan);

        // act
        var parsedJson = new JsonOperationPlanFormatter(s_indentedOptions).Format(parsedPlan);

        // assert
        Assert.Equal(originalJson, parsedJson);
        var variables = parsedPlan.Policies!.Variables;
        Assert.Equal(variables.Take(2), variables[2].Operands);
        var variablesJson = JsonNode.Parse(parsedJson)!["policies"]!["variables"]!
            .ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        variablesJson.MatchInlineSnapshot(
            """
            [
              {
                "name": "__fusion_policy_1",
                "selections": [
                  {
                    "path": "$.products.price"
                  }
                ]
              },
              {
                "name": "__fusion_policy_2",
                "selections": [
                  {
                    "path": "$.products.note"
                  }
                ]
              },
              {
                "name": "__fusion_policy_3",
                "operands": [
                  "__fusion_policy_1",
                  "__fusion_policy_2"
                ]
              }
            ]
            """);
    }

    [Fact]
    public void Parse_Should_Throw_When_PolicyVariableOperandIsNotDefinedBeforeIt()
    {
        // arrange
        var compositeSchema = ComposeSchema(
            $$"""
            # name: a
            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            {{PolicyDirectives}}
            """,
            $$"""
            # name: b
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Int @authenticated
              note: String @requiresScopes(scopes: [["read"]])
            }

            {{PolicyDirectives}}
            """);
        var originalPlan = PlanWithPooledFields(
            compositeSchema,
            "{ products { price note } }",
            TestPolicyResolver.Create());
        var json = new JsonOperationPlanFormatter(s_indentedOptions).Format(originalPlan);
        var document = JsonNode.Parse(json)!;
        var variables = document["policies"]!["variables"]!.AsArray();
        var combined = variables[2]!;
        variables.RemoveAt(2);
        variables.Insert(0, combined);
        var reorderedJson = document.ToJsonString();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateParser(compositeSchema, TestPolicyResolver.Create())
                .Parse(Encoding.UTF8.GetBytes(reorderedJson)));

        // assert
        Assert.Equal(
            "The policy variable '__fusion_policy_3' combines the variable '__fusion_policy_1', "
            + "which is not defined before it.",
            exception.Message);
    }

    [Fact]
    public void Parse_Should_RoundTripPolicyPlan_When_ProtectedFieldsAreSelectedThroughAbstractTypes()
    {
        // arrange
        var compositeSchema = ComposeSchema(
            $$"""
            type Query {
              node(id: ID!): Node @lookup
              authorById(id: ID!): Author @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String! @authenticated
            }

            type Author implements Node {
              id: ID!
              name: String! @requiresScopes(scopes: [["read"]])
            }

            {{PolicyDirectives}}
            """);
        var originalPlan = PlanWithPooledFields(
            compositeSchema,
            """
            {
              node(id: "abc") {
                id
                ... on Discussion { title }
                ... on Author { name }
              }
            }
            """,
            TestPolicyResolver.Create());
        var (originalJson, parsedPlan) = RoundTrip(compositeSchema, originalPlan);

        // act
        var parsedJson = new JsonOperationPlanFormatter(s_indentedOptions).Format(parsedPlan);

        // assert
        Assert.Equal(originalJson, parsedJson);
        parsedJson.MatchInlineSnapshot(
            """
            {
              "version": "1.3.0",
              "id": "ddeb9a07a1ce2925db3f619131745ad7f71a87d88becc06033d7958550dee877",
              "operation": {
                "kind": "Query",
                "document": "{ node(id: \"abc\") { __typename @fusion__requirement id ... on Discussion { title } ... on Author { name } } }",
                "id": "123456789101112",
                "hash": "123456789101112",
                "shortHash": "123456789101112"
              },
              "searchSpace": 1,
              "expandedNodes": 1,
              "nodes": [
                {
                  "id": 1,
                  "type": "Node",
                  "idValue": "\"abc\"",
                  "responseName": "node",
                  "branches": {
                    "Author": 4,
                    "Discussion": 3
                  },
                  "fallback": 2
                },
                {
                  "id": 2,
                  "type": "Operation",
                  "operation": {
                    "name": "Op_123456789101112_2",
                    "kind": "Query",
                    "document": "query Op_123456789101112_2 { node(id: \"abc\") { __typename id } }",
                    "hash": "27f70b45d55118c12968c57c2daf2e7e0804ec56d63ac91b8d34cac24f0a5576",
                    "shortHash": "27f70b45",
                    "xxHash": 13931885275327754576
                  },
                  "resultSelectionSet": "{ node }",
                  "dependencies": [
                    1
                  ]
                },
                {
                  "id": 3,
                  "type": "Operation",
                  "schema": "a",
                  "operation": {
                    "name": "Op_123456789101112_3",
                    "kind": "Query",
                    "document": "query Op_123456789101112_3($__fusion_policy_1: Boolean!) { node(id: \"abc\") { __typename ... on Discussion { __typename id title @skip(if: $__fusion_policy_1) } } }",
                    "hash": "72b367b855782010f5253100568f1fb25bb26f02a6d3cd2e72bc3a953edf2266",
                    "shortHash": "72b367b8",
                    "xxHash": 17955393752029729173
                  },
                  "lookupTypeName": "Discussion",
                  "resultSelectionSet": "{ node { __typename ... on Discussion { __typename id title } } }",
                  "forwardedVariables": [
                    "__fusion_policy_1"
                  ],
                  "dependencies": [
                    1
                  ]
                },
                {
                  "id": 4,
                  "type": "Operation",
                  "schema": "a",
                  "operation": {
                    "name": "Op_123456789101112_4",
                    "kind": "Query",
                    "document": "query Op_123456789101112_4($__fusion_policy_2: Boolean!) { node: authorById(id: \"abc\") { __typename id name @skip(if: $__fusion_policy_2) } }",
                    "hash": "a225185a064e3b83d9693ffa72bcb786c3638b91c867ffb7f06020fa7853b542",
                    "shortHash": "a225185a",
                    "xxHash": 14427187784509197674
                  },
                  "lookupTypeName": "Author",
                  "resultSelectionSet": "{ node { __typename id name } }",
                  "forwardedVariables": [
                    "__fusion_policy_2"
                  ],
                  "dependencies": [
                    1
                  ]
                }
              ],
              "policies": {
                "descriptors": [
                  {
                    "directiveName": "requiresScopes",
                    "scopes": [
                      [
                        "read"
                      ]
                    ],
                    "selection": {
                      "path": "$.node<Author>.name"
                    }
                  },
                  {
                    "directiveName": "authenticated",
                    "selection": {
                      "path": "$.node<Discussion>.title"
                    }
                  }
                ],
                "variables": [
                  {
                    "name": "__fusion_policy_1",
                    "selections": [
                      {
                        "path": "$.node<Discussion>.title"
                      }
                    ]
                  },
                  {
                    "name": "__fusion_policy_2",
                    "selections": [
                      {
                        "path": "$.node<Author>.name"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public void Parse_Should_ResolvePoliciesThroughResolver_When_PolicyNamesAreParsed()
    {
        // arrange
        var compositeSchema = ComposeSchema(
            $$"""
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              margin: Int @policy(policies: [["finance"]])
            }

            {{PolicyDirectives}}
            """);
        var knownResolver = CreatePolicyResolver(policies => policies.Allow("finance"));
        var originalPlan = PlanWithPooledFields(compositeSchema, "{ product { margin } }", knownResolver);
        var json = new JsonOperationPlanFormatter(s_indentedOptions).Format(originalPlan);

        // act
        var knownPlan = CreateParser(compositeSchema, knownResolver).Parse(Encoding.UTF8.GetBytes(json));
        var unknownPlan = CreateParser(compositeSchema, TestPolicyResolver.Create())
            .Parse(Encoding.UTF8.GetBytes(json));

        // assert
        Assert.IsType<InMemoryPolicy>(Assert.Single(knownPlan.Policies!.Descriptors).Policy);
        Assert.IsType<UnresolvedPolicy>(Assert.Single(unknownPlan.Policies!.Descriptors).Policy);
        Assert.True(unknownPlan.Policies!.HasUnresolvedPolicies);
    }

    [Fact]
    public void Parse_Should_ReturnNoPolicyPlan_When_PlanJsonHasNoPolicies()
    {
        // arrange
        var compositeSchema = ComposeSchema(
            $$"""
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              margin: Int @policy(policies: [["finance"]])
            }

            {{PolicyDirectives}}
            """);
        var originalPlan = PlanWithPooledFields(
            compositeSchema,
            "{ product { margin } }",
            TestPolicyResolver.Create());
        var json = JsonNode.Parse(new JsonOperationPlanFormatter().Format(originalPlan))!.AsObject();
        json.Remove("policies");

        // act
        var parsedPlan = CreateParser(compositeSchema, TestPolicyResolver.Create())
            .Parse(Encoding.UTF8.GetBytes(json.ToJsonString()));

        // assert
        Assert.NotNull(originalPlan.Policies);
        Assert.Null(parsedPlan.Policies);
    }

    // Source schema A owns the union and the nested lookups, source schema B the Asset entity
    // fields. Without request grouping the merged multi-target lookups become batch nodes that
    // hold a single BatchOperationDefinition with fan-in dependencies.
    private const string UnionSiblingAssetSchemaA =
        """
        # name: a
        type Query {
          search: [CandidateResult!]!
          meterTypeById(id: Int!): MeterType @lookup @internal
          categoryByDbId(dbId: Int!): Category @lookup @internal
        }
        union CandidateResult = NotSubmissableResult | ExistingResult | ConfigMatchesResult | CustomResult
        type ExistingResult { asset: Asset }
        type ConfigMatchesResult { asset: Asset }
        type NotSubmissableResult { asset: Asset }
        type CustomResult { asset: Asset }
        type Asset @key(fields: "dbId") { dbId: Int! }
        type MeterType @key(fields: "id") {
          id: Int!
          type: String!
        }
        type Category @key(fields: "dbId") {
          dbId: Int!
          allowedMeterTypes: [MeterType!]!
        }
        """;

    private const string UnionSiblingAssetSchemaB =
        """
        # name: b
        type Query {
          assetByDbId(dbId: Int!): Asset @lookup @internal
        }
        type Asset @key(fields: "dbId") {
          dbId: Int!
          statusMessage: String
          meterType: MeterType
          category: Category
        }
        type MeterType @key(fields: "id") { id: Int! }
        type Category @key(fields: "dbId") { dbId: Int! }
        """;

    [Fact]
    public void Parse_Plan_Preserves_Optional_Dependency_Wiring_For_Single_Merged_Batch_Node()
    {
        // arrange
        // each fan-in dependency of a merged multi-target operation only feeds a subset of
        // its targets, so it must be wired optional on the parsed plan just like on the built plan
        var compositeSchema = ComposeSchema(UnionSiblingAssetSchemaA, UnionSiblingAssetSchemaB);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category { allowedMeterTypes { type } }
                    meterType { type }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType { type }
                    statusMessage
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
              }
            }
            """,
            new OperationPlannerOptions { EnableRequestGrouping = false });

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var originalWiring = DescribeDependencyWiring(originalPlan);
        Assert.Equal(originalWiring, DescribeDependencyWiring(parsedPlan));
        Assert.Distinct(CollectNodeAndDefinitionIds(parsedPlan));
        originalWiring.MatchInlineSnapshot(
            """
            1 Operation required=[] optional=[]
            5 Operation required=[1] optional=[]
            8 Operation required=[1] optional=[]
            14 OperationBatch(Merged) required=[] optional=[1]
            15 OperationBatch(Merged) required=[] optional=[5, 8, 14]
            16 OperationBatch(Merged) required=[] optional=[5, 8, 14]

            """);
    }

    [Fact]
    public void Parse_Plan_Preserves_Optional_Dependency_Wiring_For_Grouped_Batch_Nodes()
    {
        // arrange
        // grouped batch nodes carry their own identifier, so the parser must rebuild the
        // batch node from the batching group identifier and redirect every member identifier
        var compositeSchema = ComposeSchema(UnionSiblingAssetSchemaA, UnionSiblingAssetSchemaB);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            query testQuery {
              search {
                ... on ExistingResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on ConfigMatchesResult {
                  asset {
                    category { allowedMeterTypes { type } }
                    meterType { type }
                  }
                }
                ... on NotSubmissableResult {
                  asset {
                    meterType { type }
                    statusMessage
                    category { allowedMeterTypes { type } }
                  }
                }
                ... on CustomResult {
                  asset {
                    meterType { type }
                    category { allowedMeterTypes { type } }
                  }
                }
              }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var originalWiring = DescribeDependencyWiring(originalPlan);
        Assert.Equal(originalWiring, DescribeDependencyWiring(parsedPlan));
        Assert.Distinct(CollectNodeAndDefinitionIds(parsedPlan));
        originalWiring.MatchInlineSnapshot(
            """
            1 Operation required=[] optional=[]
            14 OperationBatch(3) required=[] optional=[1]
            15 OperationBatch(2) required=[] optional=[14]

            """);
    }

    private static List<int> CollectNodeAndDefinitionIds(OperationPlan plan)
    {
        var ids = new List<int>();

        foreach (var node in plan.AllNodes)
        {
            ids.Add(node.Id);

            switch (node)
            {
                case OperationBatchExecutionNode batchNode:
                    foreach (var operation in batchNode.Operations)
                    {
                        ids.Add(operation.Id);
                    }
                    break;

                case ApolloOperationBatchExecutionNode apolloBatchNode:
                    foreach (var operation in apolloBatchNode.Operations)
                    {
                        ids.Add(operation.Id);
                    }
                    break;
            }
        }

        return ids;
    }

    private static string DescribeDependencyWiring(OperationPlan plan)
    {
        var builder = new StringBuilder();

        foreach (var node in plan.AllNodes.OrderBy(n => n.Id))
        {
            builder.Append(node.Id);
            builder.Append(' ');
            builder.Append(node.Type);

            if (node is OperationBatchExecutionNode batchNode)
            {
                builder.Append(
                    batchNode.Operations is [BatchOperationDefinition]
                        ? "(Merged)"
                        : $"({batchNode.Operations.Length})");
            }

            AppendDependencyIds(builder, " required=", node.Dependencies);
            AppendDependencyIds(builder, " optional=", node.OptionalDependencies);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendDependencyIds(
        StringBuilder builder,
        string label,
        ReadOnlySpan<IOperationPlanNode> dependencies)
    {
        var ids = new List<int>();

        foreach (var dependency in dependencies)
        {
            ids.Add(dependency.Id);
        }

        ids.Sort();
        builder.Append(label);
        builder.Append('[');
        builder.Append(string.Join(", ", ids));
        builder.Append(']');
    }

    // Source schema A defines the Media interface and its implementing types; source schema B is
    // an @interfaceObject stand-in contributing "views". A value produced by B is opaque, and that
    // opacity is derived when the result selection set is (re)built, so it must survive a plan
    // serialize/parse round-trip for a cached plan to execute correctly.
    private const string InterfaceObjectSchemaA =
        """
        # name: a
        type Query {
          mediaById(id: ID!): Media @lookup
        }
        interface Media {
          id: ID!
          title: String!
        }
        type Book implements Media @key(fields: "id") {
          id: ID!
          title: String!
          isbn: String!
        }
        type Movie implements Media @key(fields: "id") {
          id: ID!
          title: String!
          runtime: Int!
        }
        """;

    private const string InterfaceObjectSchemaB =
        """
        # name: b
        type Query {
          trendingMedia: [Media!]!
          mediaByKey(id: ID!): Media @lookup @internal
        }
        type Media @interfaceObject @key(fields: "id") {
          id: ID!
          views: Int!
        }
        """;

    [Fact]
    public void Parse_Plan_Preserves_InterfaceObject_Opacity_On_StandIn_Fetch()
    {
        // arrange
        var compositeSchema = ComposeSchema(InterfaceObjectSchemaA, InterfaceObjectSchemaB);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            query {
              trendingMedia {
                __typename
                id
                views
              }
            }
            """);

        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var planSource = Encoding.UTF8.GetBytes(formatter.Format(originalPlan));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        // The stand-in fetch (schema b) resolves { id views }; its trendingMedia child must carry
        // the opacity marker after deserialization so the executor completes it interface-typed.
        var standInNode = parsedPlan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => t.SchemaName == "b");
        var opaqueChild = standInNode.ResultSelectionSet.TryGetChild("trendingMedia");
        Assert.NotNull(opaqueChild);
        Assert.True(opaqueChild.ProducesOpaqueElements);
    }

    [Fact]
    public void Parse_Plan_Preserves_LazySkipped_InterfaceObject_Plan()
    {
        // arrange
        var compositeSchema = ComposeSchema(InterfaceObjectSchemaA, InterfaceObjectSchemaB);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            query {
              trendingMedia {
                id
              }
            }
            """);

        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var planSource = Encoding.UTF8.GetBytes(formatter.Format(originalPlan));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        // Neither __typename nor any type-conditioned field is selected, so the plan is a single
        // stand-in fetch with no covering lookup, and that shape survives the round-trip while the
        // value still completes interface-typed (opacity preserved).
        var standInNode = Assert.Single(parsedPlan.AllNodes.OfType<OperationExecutionNode>());
        Assert.Equal("b", standInNode.SchemaName);
        var opaqueChild = standInNode.ResultSelectionSet.TryGetChild("trendingMedia");
        Assert.NotNull(opaqueChild);
        Assert.True(opaqueChild.ProducesOpaqueElements);
    }

    [Fact]
    public void Parse_Plan_Uses_SelectionSet_Syntax_When_Present()
    {
        // arrange
        // Inject a custom selection set string into the formatted plan, then
        // parse it back to confirm the parser preserves the syntax.
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    id
                    name
                }
            }
            """);

        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var json = JsonNode.Parse(formatter.Format(originalPlan))!;
        var operationNode = json["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .First(t => t["type"]?.GetValue<string>() is "Operation");
        var operationNodeId = operationNode["id"]!.GetValue<int>();
        operationNode["resultSelectionSet"] = "{ __typename }";
        var planSource = Encoding.UTF8.GetBytes(json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        var parsedOperationNode = parsedPlan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => t.Id == operationNodeId);
        Assert.Equal("{ __typename }", parsedOperationNode.ResultSelectionSet.ToString(indented: false));
    }

    [Fact]
    public void Parse_Should_PreserveSourceResponseNameMapping_When_PlanIsRoundTripped()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    clientName: name
                }
            }
            """);
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var json = JsonNode.Parse(formatter.Format(originalPlan))!;
        var operationNode = json["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .First(t => t["type"]?.GetValue<string>() is "Operation"
                && t["resultSelectionSet"]?.GetValue<string>()
                    .Contains("productBySlug", StringComparison.Ordinal) == true);
        var operationNodeId = operationNode["id"]!.GetValue<int>();
        const string resultSelectionSet =
            "{ productBySlug { fusion__field_1: name "
            + "@fusion__responseName(name: \"clientName\") } }";
        operationNode["resultSelectionSet"] = resultSelectionSet;
        var planSource = Encoding.UTF8.GetBytes(
            json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        var parsedOperationNode = parsedPlan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => t.Id == operationNodeId);
        Assert.Equal(
            resultSelectionSet,
            parsedOperationNode.ResultSelectionSet.ToString(indented: false));
    }

    [Fact]
    public void Parse_Plan_With_Node()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
              node(id: ID!): Node @lookup
              authorById(id: ID!): Author @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }

            type Author implements Node {
              id: ID!
              name: String!
            }
            """);

        var compositeSchema = ComposeSchema(source1);

        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
              a: node(id: "abc") {
                id
                ... on Discussion {
                  title
                }
                ... on Author {
                  name
                }
              }
            }
            """);
        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var parsedPlanFormatted = formatter.Format(parsedPlan);
        parsedPlanFormatted.MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Plan_With_Apollo_Lookup()
    {
        // arrange
        var compositeSchema = ComposeApolloSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    id
                    name
                }
            }
            """);
        var originalNode = Assert.Single(
            originalPlan.AllNodes.OfType<ApolloOperationExecutionNode>());

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var parsedNode = Assert.Single(parsedPlan.AllNodes.OfType<ApolloOperationExecutionNode>());
        AssertOperationSourceTextEqual(originalNode.Operation, parsedNode.Operation);
        AssertApolloLookup(parsedNode.Lookup, "Product");
        FormatRepresentationShapeRoundTrip(
            originalNode.Lookup.RepresentationShape,
            parsedNode.Lookup.RepresentationShape).MatchInlineSnapshot(
            """
            {
              "planner": [
                {
                  "name": "id",
                  "responseName": "id",
                  "requirementIndex": 0,
                  "lhsPath": [],
                  "isList": false,
                  "skipOnNull": true,
                  "elementInputType": null,
                  "parentTypeCondition": null,
                  "typeCondition": null,
                  "requiresTypeName": false,
                  "nodes": "default",
                  "branches": []
                }
              ],
              "parser": [
                {
                  "name": "id",
                  "responseName": "id",
                  "requirementIndex": 0,
                  "lhsPath": [],
                  "isList": false,
                  "skipOnNull": true,
                  "elementInputType": null,
                  "parentTypeCondition": null,
                  "typeCondition": null,
                  "requiresTypeName": false,
                  "nodes": "default",
                  "branches": []
                }
              ]
            }
            """);
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Plan_With_Apollo_Lookup_Batch()
    {
        // arrange
        // Two entity lookups against the same Apollo source schema at the same
        // depth are grouped into a single Apollo batch node.
        var compositeSchema = ComposeApolloSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    id
                    name
                }
                brands {
                    id
                    name
                }
            }
            """);
        var originalNode = Assert.Single(
            originalPlan.AllNodes.OfType<ApolloOperationBatchExecutionNode>());

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        MatchSnapshot(originalPlan);
        var parsedNode = Assert.Single(
            parsedPlan.AllNodes.OfType<ApolloOperationBatchExecutionNode>());
        Assert.Equal(originalNode.Lookups.Length, parsedNode.Lookups.Length);

        for (var i = 0; i < originalNode.Lookups.Length; i++)
        {
            AssertOperationSourceTextEqual(
                originalNode.Lookups[i].Operation,
                parsedNode.Lookups[i].Operation);
        }

        Assert.Collection(
            parsedNode.Lookups.ToArray(),
            lookup => AssertApolloLookup(lookup, "Brand"),
            lookup => AssertApolloLookup(lookup, "Product"));
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Should_RestoreMultiTargetApolloLookup_When_PlanMergesIdenticalLookups()
    {
        // arrange
        // The Product lookups for '$.products' and '$.brands.featured' are identical and sit
        // at the same depth, so they merge into one lookup with two targets.
        var compositeSchema = ComposeApolloSchema(
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Brand @key(fields: "id") {
              id: ID!
              featured: Product
            }

            type Query {
              products: [Product!]
              brands: [Brand!]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product | Brand

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """,
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }

            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    name
                }
                brands {
                    featured {
                        name
                    }
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        MatchSnapshot(originalPlan);
        var parsedNode = Assert.Single(
            parsedPlan.AllNodes.OfType<ApolloOperationBatchExecutionNode>());
        var parsedOperation = Assert.IsType<BatchOperationDefinition>(
            Assert.Single(parsedNode.Operations.ToArray()));
        Assert.Equal(
            ["$.brands.featured", "$.products"],
            parsedOperation.Targets.ToArray().Select(t => t.ToString()));
        AssertApolloLookup(Assert.Single(parsedNode.Lookups.ToArray()), "Product");
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Format_Should_WriteEntitiesOperationMetadata_When_PlanHasApolloLookup()
    {
        // arrange
        var compositeSchema = ComposeApolloSchema();
        var plan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    id
                    name
                }
            }
            """);
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        // act
        var planSource = formatter.Format(plan);

        // assert
        GetNodes(planSource, "ApolloOperation").MatchSnapshot(extension: ".json");
    }

    [Fact]
    public void Format_Should_WriteEntitiesOperationMetadata_When_PlanHasApolloLookupBatch()
    {
        // arrange
        var compositeSchema = ComposeApolloSchema();
        var plan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    id
                    name
                }
                brands {
                    id
                    name
                }
            }
            """);
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        // act
        var planSource = formatter.Format(plan);

        // assert
        GetNodes(planSource, "ApolloOperationBatch").MatchSnapshot(extension: ".json");
    }

    [Fact]
    public void Parse_Should_DeriveAliasedRepresentationPath_When_RequirementIsNested()
    {
        // arrange
        var compositeSchema = ComposeApolloSchema(NestedRequiresSchemaA, NestedRequiresSchemaB);
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                foos {
                    id
                    details: bar {
                        x
                    }
                }
            }
            """);
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var planSource = formatter.Format(originalPlan);
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        // act
        var parsedPlan = parser.Parse(Encoding.UTF8.GetBytes(planSource));

        // assert
        GetNodes(planSource, "ApolloOperation").MatchSnapshot(extension: ".json");
        Assert.Equal(planSource, formatter.Format(parsedPlan));
        var parsedNode = Assert.Single(
            parsedPlan.AllNodes.OfType<ApolloOperationExecutionNode>());
        FormatRepresentationShape(parsedNode.Lookup.RepresentationShape)
            .ToJsonString(new JsonSerializerOptions { WriteIndented = true })
            .MatchInlineSnapshot(
            """
            [
              {
                "name": "id",
                "responseName": "id",
                "requirementIndex": 0,
                "lhsPath": [],
                "isList": false,
                "skipOnNull": true,
                "elementInputType": null,
                "parentTypeCondition": null,
                "typeCondition": null,
                "requiresTypeName": false,
                "nodes": "default",
                "branches": []
              },
              {
                "name": "bar",
                "responseName": "details",
                "requirementIndex": -1,
                "lhsPath": [],
                "isList": false,
                "skipOnNull": true,
                "elementInputType": null,
                "parentTypeCondition": null,
                "typeCondition": null,
                "requiresTypeName": false,
                "nodes": [
                  {
                    "name": "y",
                    "responseName": "y",
                    "requirementIndex": 1,
                    "lhsPath": [],
                    "isList": false,
                    "skipOnNull": false,
                    "elementInputType": null,
                    "parentTypeCondition": null,
                    "typeCondition": null,
                    "requiresTypeName": false,
                    "nodes": "default",
                    "branches": []
                  }
                ],
                "branches": []
              }
            ]
            """);
    }

    [Fact]
    public void Parse_Should_Throw_When_ApolloNodeHasNoSchema()
    {
        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => ParseApolloPlanWithMutatedNode(node => node.Remove("schema")));

        // assert
        Assert.Equal(
            "The schema is required on an Apollo operation of a valid operation plan.",
            exception.Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ApolloNodeHasEmptyEntityType()
    {
        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => ParseApolloPlanWithMutatedNode(node => node["entityType"] = ""));

        // assert
        Assert.Equal(
            "The entityType is required on an Apollo operation of a valid operation plan.",
            exception.Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ApolloNodeHasNoEntityType()
    {
        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => ParseApolloPlanWithMutatedNode(node => node.Remove("entityType")));

        // assert
        Assert.Equal(
            "The entityType is required on an Apollo operation of a valid operation plan.",
            exception.Message);
    }

    [Fact]
    public void Parse_Plan_Preserves_DeliveryGroup_Identity_Across_Plan_And_IncrementalPlans()
    {
        // arrange
        // Two sibling @defer fragments share a field (email) plus a nested @defer
        // adds a parent chain. Round-trip must restore canonical DeliveryGroup instances.
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
            }
            """);

        var originalPlan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                        ... @defer(label: "nested") {
                            address
                        }
                    }
                    ... @defer(label: "location") {
                        email
                    }
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchSnapshot();
        Assert.All(
            parsedPlan.IncrementalPlans,
            p => Assert.All(
                p.DeliveryGroups,
                g => Assert.Same(parsedPlan.DeliveryGroups.Single(d => d.Id == g.Id), g)));
        Assert.All(
            parsedPlan.DeliveryGroups.Where(g => g.Parent is not null),
            g => Assert.Same(parsedPlan.DeliveryGroups.Single(d => d.Id == g.Parent!.Id), g.Parent));
    }

    [Fact]
    public void Parse_Plan_Preserves_NoParentNodeId_When_RootPlanHasNoExecutionNode()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                users: [User!]!
            }

            type User {
                id: ID!
            }
            """);

        var originalPlan = PlanOperation(
            schema,
            """
            {
                ... @defer {
                    users {
                        id
                    }
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchSnapshot();
        Assert.Empty(parsedPlan.AllNodes);
        Assert.Equal(
            IncrementalPlan.NoParentNodeId,
            Assert.Single(parsedPlan.IncrementalPlans).ParentNodeId);
    }

    [Fact]
    public void Parse_Plan_Preserves_ParentDependencies_On_Deferred_IncrementalPlan_Nodes()
    {
        // arrange
        // Same-subgraph hoist injects the key into the parent op so the
        // deferred incremental plan node carries a ParentStepRef on its plan step
        // and a {"parentNodeId": N} entry in its serialized dependencies.
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
            }
            """);

        var originalPlan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                    }
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var originalIncrementalPlanNode = originalPlan.IncrementalPlans
            .Single()
            .AllNodes
            .OfType<OperationExecutionNode>()
            .Single(n => n.ParentDependencies.Length > 0);
        var parsedIncrementalPlanNode = parsedPlan.IncrementalPlans
            .Single()
            .AllNodes
            .OfType<OperationExecutionNode>()
            .Single(n => n.Id == originalIncrementalPlanNode.Id);
        Assert.Equal(
            originalIncrementalPlanNode.ParentDependencies.ToArray(),
            parsedIncrementalPlanNode.ParentDependencies.ToArray());
        Assert.Equal(
            originalIncrementalPlanNode.Dependencies.Length,
            parsedIncrementalPlanNode.Dependencies.Length);
    }

    [Fact]
    public void Parse_Plan_Without_BatchingGroupId_As_StandaloneOperations()
    {
        // arrange
        // A missing batchingGroupId is the current discriminator for a standalone operation.
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    id
                    name
                    estimatedDelivery(postCode: "12345")
                }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        var json = JsonNode.Parse(buffer.WrittenSpan)!;
        var standaloneNodeIds = new List<int>();

        foreach (var node in json["nodes"]!.AsArray())
        {
            if (node?["type"]?.GetValue<string>() is "Operation")
            {
                standaloneNodeIds.Add(node["id"]!.GetValue<int>());
                node.AsObject().Remove("batchingGroupId");
            }
        }
        var standalonePlanSource = Encoding.UTF8.GetBytes(
            json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        // act
        var parsedPlan = parser.Parse(standalonePlanSource);

        // assert
        Assert.Equal(
            standaloneNodeIds,
            parsedPlan.AllNodes.Select(
                node => Assert.IsType<OperationExecutionNode>(node).Id));
    }

    /// <summary>
    /// Returns the nodes of the given type from a formatted plan as an indented JSON array.
    /// </summary>
    private static string GetNodes(string planSource, string nodeType)
    {
        var nodes = JsonNode.Parse(planSource)!["nodes"]!
            .AsArray()
            .Where(t => t!["type"]!.GetValue<string>() == nodeType)
            .Select(t => t!.DeepClone())
            .ToArray();

        return new JsonArray(nodes).ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static void AssertApolloLookup(
        ApolloEntityLookup lookup,
        string entityTypeName)
    {
        Assert.Equal(entityTypeName, lookup.EntityTypeName);
        Assert.False(lookup.RepresentationShape.IsDefault);
        Assert.NotNull(lookup.OperationDocument);
        Assert.Collection(
            lookup.RepresentationShape,
            node =>
            {
                Assert.Equal("id", node.Name);
                Assert.Equal("id", node.ResponseName);
                Assert.Equal(0, node.RequirementIndex);
            });
        Assert.Contains("_entities", Encoding.UTF8.GetString(lookup.Operation.Value.Span));
    }

    private static void AssertOperationSourceTextEqual(
        OperationSourceText expected,
        OperationSourceText actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Hash, actual.Hash);
        Assert.Equal(expected.Value.ToArray(), actual.Value.ToArray());
    }

    private static string FormatRepresentationShapeRoundTrip(
        ImmutableArray<RepresentationShapeNode> plannerShape,
        ImmutableArray<RepresentationShapeNode> parserShape)
        => new JsonObject
        {
            ["planner"] = FormatRepresentationShape(plannerShape),
            ["parser"] = FormatRepresentationShape(parserShape)
        }.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    private static JsonNode FormatRepresentationShape(
        ImmutableArray<RepresentationShapeNode> shape)
    {
        if (shape.IsDefault)
        {
            return JsonValue.Create("default");
        }

        var nodes = new JsonArray();

        foreach (var node in shape)
        {
            var lhsPath = new JsonArray();

            foreach (var segment in node.LhsPath)
            {
                lhsPath.Add(segment);
            }

            var branches = new JsonArray();

            foreach (var branch in node.Branches)
            {
                branches.Add(new JsonObject
                {
                    ["typeCondition"] = branch.TypeCondition,
                    ["nodes"] = FormatRepresentationShape(branch.Nodes)
                });
            }

            nodes.Add(new JsonObject
            {
                ["name"] = node.Name,
                ["responseName"] = node.ResponseName,
                ["requirementIndex"] = node.RequirementIndex,
                ["lhsPath"] = lhsPath,
                ["isList"] = node.IsList,
                ["skipOnNull"] = node.SkipOnNull,
                ["elementInputType"] = node.ElementInputType?.ToString(),
                ["parentTypeCondition"] = node.ParentTypeCondition,
                ["typeCondition"] = node.TypeCondition,
                ["requiresTypeName"] = node.RequiresTypeName,
                ["nodes"] = FormatRepresentationShape(node.Nodes),
                ["branches"] = branches
            });
        }

        return nodes;
    }

    /// <summary>
    /// Formats a plan with a single Apollo lookup, applies the mutation to its
    /// Apollo node and parses the mutated plan.
    /// </summary>
    private static OperationPlan ParseApolloPlanWithMutatedNode(
        Action<JsonObject> mutateApolloNode)
    {
        var compositeSchema = ComposeApolloSchema();
        var plan = PlanOperation(
            compositeSchema,
            """
            {
                products {
                    id
                    name
                }
            }
            """);
        var formatter = new JsonOperationPlanFormatter();
        var planSource = JsonNode.Parse(formatter.Format(plan))!;
        var apolloNode = planSource["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .Single(t => t["type"]!.GetValue<string>() == "ApolloOperation");

        mutateApolloNode(apolloNode);

        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler, TestPolicyResolver.Create());

        return parser.Parse(Encoding.UTF8.GetBytes(planSource.ToJsonString()));
    }

    // Source schema "a" owns 'Bar.y' and source schema "b" requires it for
    // 'Bar.x', so the lookup on "b" binds its requirement on the nested 'bar'
    // field. The client aliases 'bar', which makes the binding path carry a
    // response name that differs from the field name.
    private const string NestedRequiresSchemaA =
        """
        schema @link(
          url: "https://specs.apollo.dev/federation/v2.6"
          import: ["@key", "@shareable"]) {
          query: Query
        }

        type Foo @key(fields: "id") {
          id: ID!
          bar: Bar @shareable
        }

        type Bar {
          y: String @shareable
        }

        type Query {
          foos: [Foo!]
          _service: _Service!
          _entities(representations: [_Any!]!): [_Entity]!
        }

        type _Service { sdl: String! }

        union _Entity = Foo

        scalar FieldSet
        scalar _Any

        directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
        directive @link(url: String! import: [String!]) repeatable on SCHEMA
        directive @shareable on FIELD_DEFINITION | OBJECT
        """;

    private const string NestedRequiresSchemaB =
        """
        schema @link(
          url: "https://specs.apollo.dev/federation/v2.6"
          import: ["@external", "@key", "@requires", "@shareable"]) {
          query: Query
        }

        type Foo @key(fields: "id") {
          id: ID! @external
          bar: Bar @shareable
        }

        type Bar {
          y: String @external
          x: Int @requires(fields: "y")
        }

        type Query {
          _service: _Service!
          _entities(representations: [_Any!]!): [_Entity]!
        }

        type _Service { sdl: String! }

        union _Entity = Foo

        scalar FieldSet
        scalar _Any

        directive @external on FIELD_DEFINITION | OBJECT
        directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
        directive @link(url: String! import: [String!]) repeatable on SCHEMA
        directive @requires(fields: FieldSet!) on FIELD_DEFINITION
        directive @shareable on FIELD_DEFINITION | OBJECT
        """;

    /// <summary>
    /// Composes a schema from two Apollo Federation subgraphs. Source schema
    /// "a" exposes the entity root fields and source schema "b" owns the
    /// entity name fields, so selecting a name plans an entity lookup that is
    /// routed to an Apollo execution node.
    /// </summary>
    private static FusionSchemaDefinition ComposeApolloSchema()
    {
        const string sourceSchemaA =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }

            type Query {
              products: [Product!]
              brands: [Brand!]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product | Brand

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        const string sourceSchemaB =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }

            type Brand @key(fields: "id") {
              id: ID!
              name: String
            }

            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product | Brand

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        return ComposeApolloSchema(sourceSchemaA, sourceSchemaB);
    }

    private static readonly JsonWriterOptions s_indentedOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // The shared test planner leaks fields between compilations through its field map pool.
    private static OperationPlan PlanWithPooledFields(
        FusionSchemaDefinition schema,
        string operationText,
        IPolicyResolver policyResolver)
    {
        var document = Utf8GraphQLParser.Parse(operationText);
        var rewritten = new DocumentRewriter(schema).RewriteDocument(document, operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();
        var compiler = new OperationCompiler(schema, new DefaultObjectPoolProvider().CreateFieldMapPool());
        var planner = new OperationPlanner(schema, compiler, OperationPlannerOptions.Default, policyResolver);
        const string id = "123456789101112";
        return planner.CreatePlan(id, id, id, operation, TestContext.Current.CancellationToken);
    }

    private static Selection GetSelection(Operation operation, params string[] responseNames)
    {
        var selectionSet = operation.RootSelectionSet;
        var selection = default(Selection);

        foreach (var responseName in responseNames)
        {
            if (selectionSet is null || !selectionSet.TryGetSelection(responseName, out selection))
            {
                throw new InvalidOperationException($"Selection {responseName} not found.");
            }

            selectionSet = selection.GetSelectionSet();
        }

        return selection!;
    }

    private static (string Json, OperationPlan ParsedPlan) RoundTrip(
        FusionSchemaDefinition schema,
        OperationPlan plan)
    {
        var json = new JsonOperationPlanFormatter(s_indentedOptions).Format(plan);
        var parsedPlan = CreateParser(schema, TestPolicyResolver.Create()).Parse(Encoding.UTF8.GetBytes(json));
        return (json, parsedPlan);
    }

    private static JsonOperationPlanParser CreateParser(
        FusionSchemaDefinition schema,
        IPolicyResolver policyResolver)
        => new(
            new OperationCompiler(schema, new DefaultObjectPoolProvider().CreateFieldMapPool()),
            policyResolver);

    private static IPolicyResolver CreatePolicyResolver(Action<InMemoryPolicyBuilder> configure)
    {
        var builder = new InMemoryPolicyBuilder();
        configure(builder);

        return new PolicyResolver(
            new BuiltInPolicyProvider(),
            [builder.Build(new InMemoryPolicyRecorder())]);
    }

    private static FusionSchemaDefinition ComposeApolloSchema(
        string sourceSchemaA,
        string sourceSchemaB)
    {
        var sourceTexts = new[]
        {
            new SourceSchemaText("a", sourceSchemaA),
            new SourceSchemaText("b", sourceSchemaB)
        };

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();

        foreach (var sourceText in sourceTexts)
        {
            composerOptions.SourceSchemas[sourceText.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions
                {
                    InferKeysFromLookups = false
                }
            };
        }

        var composer = new SchemaComposer(sourceTexts, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}
