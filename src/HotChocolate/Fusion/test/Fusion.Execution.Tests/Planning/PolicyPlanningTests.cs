using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Policies.InMemory;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Policies.PolicyTestHelper;

namespace HotChocolate.Fusion.Planning;

public class PolicyPlanningTests : FusionTestBase
{
    private const string Directives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    private const string ProductSchema =
        $$"""
        type Query {
          product: Product
          secret: String @authenticated
        }

        type Product {
          id: ID!
          name: String! @authenticated
          price: Int @requiresScopes(scopes: [["read"]])
        }

        {{Directives}}
        """;

    private const string ProductsSchemaA =
        $$"""
        # name: a
        type Query {
          products: [Product]
        }

        type Product @key(fields: "id") {
          id: ID!
          weight: Int
        }

        {{Directives}}
        """;

    private const string ProductsSchemaB =
        $$"""
        # name: b
        type Query {
          productById(id: ID!): Product @lookup @internal
        }

        type Product @key(fields: "id") {
          id: ID!
          price: Int @authenticated
          note: String @requiresScopes(scopes: [["read"]])
          shippingEstimate(weight: Int @require(field: "weight")): Int @authenticated
        }

        {{Directives}}
        """;

    private const string ProductsSchemaBWithOpenField =
        $$"""
        # name: b
        type Query {
          productById(id: ID!): Product @lookup @internal
        }

        type Product @key(fields: "id") {
          id: ID!
          open: String
          shippingEstimate(weight: Int @require(field: "weight")): Int @authenticated
        }

        {{Directives}}
        """;

    [Fact]
    public void CreatePlan_Should_SkipProtectedFieldsWithSyntheticVariables_When_FieldsCarryPolicies()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);

        // act
        var plan = PlanOperation(schema, "{ product { id name price } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1(
                      $__fusion_policy_1: Boolean!
                      $__fusion_policy_2: Boolean!
                    ) {
                      product {
                        id
                        name @skip(if: $__fusion_policy_1)
                        price @skip(if: $__fusion_policy_2)
                      }
                    }
                  hash: 3b93905e93238450620f4f958670af67b19bd3a1389d018e202969e3d03a81f8
                  shortHash: 3b93905e
                  xxHash: 14642125102203654305
                resultSelectionSet: >-
                  { product }
                forwardedVariables:
                  - __fusion_policy_1
                  - __fusion_policy_2
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.product.name
                - directiveName: requiresScopes
                  scopes:
                    - [read]
                  selection:
                    path: $.product.price
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.product.name
                - name: __fusion_policy_2
                  selections:
                    - path: $.product.price
            """);
    }

    [Fact]
    public void CreatePlan_Should_AllocateOneVariablePerSelection_When_AliasesSelectTheSameProtectedField()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);

        // act
        var plan = PlanOperation(schema, "{ product { first: name second: name } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1(
                      $__fusion_policy_1: Boolean!
                      $__fusion_policy_2: Boolean!
                    ) {
                      product {
                        first: name @skip(if: $__fusion_policy_1)
                        second: name @skip(if: $__fusion_policy_2)
                      }
                    }
                  hash: 5e26ccc562353879b9e6c0092f80c067a328a9d549faa554dd2f5ea921978e09
                  shortHash: 5e26ccc5
                  xxHash: 13814883196681120138
                resultSelectionSet: >-
                  { product }
                forwardedVariables:
                  - __fusion_policy_1
                  - __fusion_policy_2
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.product.first
                - directiveName: authenticated
                  selection:
                    path: $.product.second
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.product.first
                - name: __fusion_policy_2
                  selections:
                    - path: $.product.second
            """);
    }

    [Fact]
    public void CreatePlan_Should_WrapFieldInFragment_When_ProtectedFieldAlreadyHasSkip()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);

        // act
        var plan = PlanOperation(schema, "query($skip: Boolean!) { product { name @skip(if: $skip) } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1($skip: Boolean!, $__fusion_policy_1: Boolean!) {
                      product {
                        ... @skip(if: $__fusion_policy_1) {
                          name @skip(if: $skip)
                        }
                      }
                    }
                  hash: 27c4c839dfbb7afc922cc8f541b572b656499425a1405951a4ebf65715998ee3
                  shortHash: 27c4c839
                  xxHash: 9965206127971575456
                resultSelectionSet: >-
                  { product }
                forwardedVariables:
                  - skip
                  - __fusion_policy_1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.product.name
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.product.name
            """);
    }

    [Fact]
    public void CreatePlan_Should_MoveSharedVariableToNodeCondition_When_AllRootSelectionsAreProtected()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchemaA, ProductsSchemaB);

        // act
        var plan = PlanOperation(schema, "{ products { price } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        id
                      }
                    }
                  hash: f991ee77bf09e4eaf8786949bcc441cd46d16baa24b422a6c1d66a03854fa31c
                  shortHash: f991ee77
                  xxHash: 11674115490493392138
                resultSelectionSet: >-
                  { products }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_1_id: ID!) {
                      productById(id: $__fusion_1_id) {
                        price
                      }
                    }
                  hash: 8e5ace38bb989a160e79c92b2efd199fc9a1ee10a71c0720311ed262232c2674
                  shortHash: 8e5ace38
                  xxHash: 4079249144343109309
                lookupTypeName: Product
                resultSelectionSet: >-
                  { price }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.products
                    selectionMap: >-
                      id
                conditions:
                  - variable: $__fusion_policy_1
                    passingValue: false
                dependencies:
                  - id: 1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.products.price
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.products.price
            """);
    }

    [Fact]
    public void CreatePlan_Should_CreateNodeVariable_When_RootSelectionsHaveDifferentVariables()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchemaA, ProductsSchemaB);

        // act
        var plan = PlanOperation(schema, "{ products { price note } }");

        // assert
        Describe(plan).MatchInlineSnapshot(
            """
            {
              "Variables": [
                {
                  "Name": "__fusion_policy_1",
                  "Selections": [
                    "price"
                  ],
                  "Operands": []
                },
                {
                  "Name": "__fusion_policy_2",
                  "Selections": [
                    "note"
                  ],
                  "Operands": []
                },
                {
                  "Name": "__fusion_policy_3",
                  "Selections": [],
                  "Operands": [
                    "__fusion_policy_1",
                    "__fusion_policy_2"
                  ]
                }
              ]
            }
            """);
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        id
                      }
                    }
                  hash: f991ee77bf09e4eaf8786949bcc441cd46d16baa24b422a6c1d66a03854fa31c
                  shortHash: f991ee77
                  xxHash: 11674115490493392138
                resultSelectionSet: >-
                  { products }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2(
                      $__fusion_policy_1: Boolean!
                      $__fusion_policy_2: Boolean!
                      $__fusion_1_id: ID!
                    ) {
                      productById(id: $__fusion_1_id) {
                        price @skip(if: $__fusion_policy_1)
                        note @skip(if: $__fusion_policy_2)
                      }
                    }
                  hash: 31ca4b899784ede4d75b38b0090f176786ce5c9d52333e1cf599c9ac5018c2ae
                  shortHash: 31ca4b89
                  xxHash: 12604915753937650340
                lookupTypeName: Product
                resultSelectionSet: >-
                  { price note }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.products
                    selectionMap: >-
                      id
                conditions:
                  - variable: $__fusion_policy_3
                    passingValue: false
                conditionAlternatives:
                  -
                    - variable: $__fusion_policy_1
                      passingValue: false
                  -
                    - variable: $__fusion_policy_2
                      passingValue: false
                forwardedVariables:
                  - __fusion_policy_1
                  - __fusion_policy_2
                dependencies:
                  - id: 1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.products.price
                - directiveName: requiresScopes
                  scopes:
                    - [read]
                  selection:
                    path: $.products.note
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.products.price
                - name: __fusion_policy_2
                  selections:
                    - path: $.products.note
                - name: __fusion_policy_3
                  operands:
                    - __fusion_policy_1
                    - __fusion_policy_2
            """);
    }

    [Fact]
    public void CreatePlan_Should_GateRequirementSelection_When_ProtectedFieldIsItsOnlyConsumer()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchemaA, ProductsSchemaB);

        // act
        var plan = PlanOperation(schema, "{ products { shippingEstimate } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1($__fusion_policy_1: Boolean!) {
                      products {
                        id
                        weight @skip(if: $__fusion_policy_1)
                      }
                    }
                  hash: 9f167a7d4a6c8448d4cf2c4a858a158d360006bd22bf37f9d15b915fb6519ef3
                  shortHash: 9f167a7d
                  xxHash: 590867055256475166
                resultSelectionSet: >-
                  { products }
                forwardedVariables:
                  - __fusion_policy_1
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_1_id: ID!, $__fusion_2_weight: Int) {
                      productById(id: $__fusion_1_id) {
                        shippingEstimate(weight: $__fusion_2_weight)
                      }
                    }
                  hash: 691bccebdbce19b1d2854a678752f31dff864c034b1e197b93139c9245c70af9
                  shortHash: 691bcceb
                  xxHash: 8213001851433487594
                lookupTypeName: Product
                resultSelectionSet: >-
                  { shippingEstimate }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.products
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    type: Int
                    path: $.products
                    selectionMap: >-
                      weight
                conditions:
                  - variable: $__fusion_policy_1
                    passingValue: false
                dependencies:
                  - id: 1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.products.shippingEstimate
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.products.shippingEstimate
            """);
    }

    [Fact]
    public void CreatePlan_Should_NotGateRequirementSelection_When_ConsumerNodeHasDifferentlyProtectedSibling()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchemaA, ProductsSchemaB);

        // act
        var plan = PlanOperation(schema, "{ products { shippingEstimate note } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        id
                        weight
                      }
                    }
                  hash: a11b97ae8805f25e588aff83cb0f324abc852acd5163e923da662957687c09e2
                  shortHash: a11b97ae
                  xxHash: 1031150499231380377
                resultSelectionSet: >-
                  { products }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2(
                      $__fusion_policy_1: Boolean!
                      $__fusion_policy_2: Boolean!
                      $__fusion_1_id: ID!
                      $__fusion_2_weight: Int
                    ) {
                      productById(id: $__fusion_1_id) {
                        note @skip(if: $__fusion_policy_1)
                        shippingEstimate(weight: $__fusion_2_weight) @skip(if: $__fusion_policy_2)
                      }
                    }
                  hash: 7133cd433c1c2a54e020f3f1e60f2bc65f5dcce551a0a36da38c4576daba00bc
                  shortHash: 7133cd43
                  xxHash: 6047108285050503958
                lookupTypeName: Product
                resultSelectionSet: >-
                  { note shippingEstimate }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.products
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    type: Int
                    path: $.products
                    selectionMap: >-
                      weight
                conditions:
                  - variable: $__fusion_policy_3
                    passingValue: false
                conditionAlternatives:
                  -
                    - variable: $__fusion_policy_1
                      passingValue: false
                  -
                    - variable: $__fusion_policy_2
                      passingValue: false
                forwardedVariables:
                  - __fusion_policy_1
                  - __fusion_policy_2
                dependencies:
                  - id: 1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.products.shippingEstimate
                - directiveName: requiresScopes
                  scopes:
                    - [read]
                  selection:
                    path: $.products.note
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.products.note
                - name: __fusion_policy_2
                  selections:
                    - path: $.products.shippingEstimate
                - name: __fusion_policy_3
                  operands:
                    - __fusion_policy_1
                    - __fusion_policy_2
            """);
    }

    [Fact]
    public void CreatePlan_Should_NotGateRequirementSelection_When_ConsumerNodeHasUnprotectedSibling()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchemaA, ProductsSchemaBWithOpenField);

        // act
        var plan = PlanOperation(schema, "{ products { shippingEstimate open } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        id
                        weight
                      }
                    }
                  hash: a11b97ae8805f25e588aff83cb0f324abc852acd5163e923da662957687c09e2
                  shortHash: a11b97ae
                  xxHash: 1031150499231380377
                resultSelectionSet: >-
                  { products }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2(
                      $__fusion_policy_1: Boolean!
                      $__fusion_1_id: ID!
                      $__fusion_2_weight: Int
                    ) {
                      productById(id: $__fusion_1_id) {
                        open
                        shippingEstimate(weight: $__fusion_2_weight) @skip(if: $__fusion_policy_1)
                      }
                    }
                  hash: 56678473ed4e11530ac9bcab667600e4f9a0e373ac5bdadfb36cbc3aa2997af5
                  shortHash: "56678473"
                  xxHash: 4421542183129440636
                lookupTypeName: Product
                resultSelectionSet: >-
                  { open shippingEstimate }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.products
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    type: Int
                    path: $.products
                    selectionMap: >-
                      weight
                forwardedVariables:
                  - __fusion_policy_1
                dependencies:
                  - id: 1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.products.shippingEstimate
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.products.shippingEstimate
            """);
    }

    [Fact]
    public void CreatePlan_Should_NotGateRequirementSelection_When_ClientSelectsItToo()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchemaA, ProductsSchemaB);

        // act
        var plan = PlanOperation(schema, "{ products { weight shippingEstimate } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      products {
                        weight
                        id
                      }
                    }
                  hash: d1af8dd83b00915c56b8eea24ae2d15693391b0429820ff6def6ffa802a238b1
                  shortHash: d1af8dd8
                  xxHash: 13604952808818186597
                resultSelectionSet: >-
                  { products }
              - id: 2
                type: Operation
                schema: b
                operation:
                  name: Op_123456789101112_2
                  kind: Query
                  document: |
                    query Op_123456789101112_2($__fusion_1_id: ID!, $__fusion_2_weight: Int) {
                      productById(id: $__fusion_1_id) {
                        shippingEstimate(weight: $__fusion_2_weight)
                      }
                    }
                  hash: 691bccebdbce19b1d2854a678752f31dff864c034b1e197b93139c9245c70af9
                  shortHash: 691bcceb
                  xxHash: 8213001851433487594
                lookupTypeName: Product
                resultSelectionSet: >-
                  { shippingEstimate }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.products
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    type: Int
                    path: $.products
                    selectionMap: >-
                      weight
                conditions:
                  - variable: $__fusion_policy_1
                    passingValue: false
                dependencies:
                  - id: 1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.products.shippingEstimate
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.products.shippingEstimate
            """);
    }

    [Fact]
    public void CreatePlan_Should_ScopeVariableToConcreteType_When_FragmentsSelectTypeConditionedFields()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              search: [SearchResult]
            }

            union SearchResult = Book | Movie

            type Book {
              title: String @authenticated
            }

            type Movie {
              title: String
            }

            {{Directives}}
            """);

        // act
        var plan = PlanOperation(
            schema,
            "{ search { ... on Book { title } ... on Movie { title } } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1($__fusion_policy_1: Boolean!) {
                      search {
                        __typename
                        ... on Book {
                          title @skip(if: $__fusion_policy_1)
                        }
                        ... on Movie {
                          title
                        }
                      }
                    }
                  hash: d51f6c9ddbac3c532047448145a7f36220528d617cf3ca313bbf4483496d6bbc
                  shortHash: d51f6c9d
                  xxHash: 14031667724514837924
                resultSelectionSet: >-
                  { search }
                forwardedVariables:
                  - __fusion_policy_1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.search<Book>.title
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.search<Book>.title
            """);
    }

    [Fact]
    public void CreatePlan_Should_ConditionEachMutationRootField_When_RootFieldsAreProtected()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              open: Int
            }

            type Mutation {
              delete: Boolean @authenticated
              update: Boolean @requiresScopes(scopes: [["write"]])
              create: Boolean
            }

            {{Directives}}
            """);

        // act
        var plan = PlanOperation(schema, "mutation { delete update create }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Mutation
                  document: |
                    mutation Op_123456789101112_1 {
                      delete
                    }
                  hash: 841f582d590553e94cd67cdee6b3ca1cb8412273ea870e6b4841fd67bafbb43b
                  shortHash: 841f582d
                  xxHash: 12489117972473022892
                resultSelectionSet: >-
                  { delete }
                conditions:
                  - variable: $__fusion_policy_1
                    passingValue: false
              - id: 2
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_2
                  kind: Mutation
                  document: |
                    mutation Op_123456789101112_2 {
                      update
                    }
                  hash: e7a9a8e298711ceeed28edfafe18666b16af244e9f0be6fd3be99494eeb093f9
                  shortHash: e7a9a8e2
                  xxHash: 10167359045783126545
                resultSelectionSet: >-
                  { update }
                conditions:
                  - variable: $__fusion_policy_2
                    passingValue: false
              - id: 3
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_3
                  kind: Mutation
                  document: |
                    mutation Op_123456789101112_3 {
                      create
                    }
                  hash: ec5a4d00fdf25c86c385da192e0e97141b93e6a7e3461a355badb5b1c84341d4
                  shortHash: ec5a4d00
                  xxHash: 10075464965401307264
                resultSelectionSet: >-
                  { create }
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.delete
                - directiveName: requiresScopes
                  scopes:
                    - [write]
                  selection:
                    path: $.update
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.delete
                - name: __fusion_policy_2
                  selections:
                    - path: $.update
            """);
    }

    [Fact]
    public void CreatePlan_Should_ConditionEventStream_When_SubscriptionFieldIsProtected()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              open: Int
            }

            type Subscription {
              changed: String @authenticated
            }

            {{Directives}}
            """);

        // act
        var plan = PlanOperation(schema, "subscription { changed }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Subscription
                  document: |
                    subscription Op_123456789101112_1 {
                      changed
                    }
                  hash: 0803ded6ed32e719fd381505d35724152f31da8867c4ca6f42528b192273b062
                  shortHash: 0803ded6
                  xxHash: 15025893984214730618
                resultSelectionSet: >-
                  { changed }
                conditions:
                  - variable: $__fusion_policy_1
                    passingValue: false
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    path: $.changed
              variables:
                - name: __fusion_policy_1
                  selections:
                    - path: $.changed
            """);
    }

    [Fact]
    public void CreatePlan_Should_MarkDeferredPlans_When_DeferredFieldsAreProtected()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            # name: a
            type Query {
              user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
              name: String!
              secret: String @authenticated
            }

            {{Directives}}
            """,
            $$"""
            # name: b
            type Query {
              userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
              id: ID!
              email: String! @authenticated
              nickname: String
            }

            {{Directives}}
            """);

        // act
        var plan = PlanOperation(
            schema,
            "{ user(id: 1) { name ... @defer { email secret nickname } } }");

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      user(id: 1) {
                        name
                        id
                      }
                    }
                  hash: 6e24d3b8bbc1dd31c763f040a0b26b30e78a222e1436ca56218ca868e2b3a362
                  shortHash: 6e24d3b8
                  xxHash: 15526821928012272930
                resultSelectionSet: >-
                  { user }
            deliveryGroups:
              - id: 0
                path: $.user
            incrementalPlans:
              - deliveryGroupIds:
                  - 0
                parentNodeId: 1
                requirements:
                  - name: __fusion_1_id
                    type: ID!
                    path: $.user
                    selectionMap: >-
                      id
                  - name: __fusion_2_id
                    type: ID!
                    path: $.user
                    selectionMap: >-
                      id
                operation:
                  - document: |
                      {
                        user(id: 1) {
                          email
                          secret
                          nickname
                          id @fusion__requirement
                        }
                      }
                    kind: Query
                    id: 123456789101112#defer_0
                    hash: 123456789101112#defer_0
                    shortHash: "123456789101112"
                nodes:
                  - id: 1
                    type: Operation
                    schema: b
                    operation:
                      name: Op_defer_1
                      kind: Query
                      document: |
                        query Op_defer_1($__fusion_policy_1: Boolean!, $__fusion_1_id: ID!) {
                          userById(id: $__fusion_1_id) {
                            email @skip(if: $__fusion_policy_1)
                            nickname
                          }
                        }
                      hash: ebaba192ca259ce2e8f1c0c3205059fd4d765b41abc4fbcbb5b7bbd6949ef852
                      shortHash: ebaba192
                      xxHash: 16479378667722747125
                    lookupTypeName: User
                    resultSelectionSet: >-
                      { email nickname }
                    source: $.userById
                    target: $.user
                    requirements:
                      - name: __fusion_1_id
                        type: ID!
                        path: $.user
                        selectionMap: >-
                          id
                    forwardedVariables:
                      - __fusion_policy_1
                    dependencies:
                      - parentNodeId: 1
                  - id: 2
                    type: Operation
                    schema: a
                    operation:
                      name: Op_defer_2
                      kind: Query
                      document: |
                        query Op_defer_2($__fusion_2_id: ID!) {
                          user(id: $__fusion_2_id) {
                            secret
                          }
                        }
                      hash: a7ae03e8a3367ecbdc658e22d9bb4f68f6be5da8d61cd269886103aadda75913
                      shortHash: a7ae03e8
                      xxHash: 12373734428100261129
                    lookupTypeName: User
                    resultSelectionSet: >-
                      { secret }
                    source: $.user
                    target: $.user
                    requirements:
                      - name: __fusion_2_id
                        type: ID!
                        path: $.user
                        selectionMap: >-
                          id
                    conditions:
                      - variable: $__fusion_policy_2
                        passingValue: false
                    dependencies:
                      - parentNodeId: 1
            policies:
              descriptors:
                - directiveName: authenticated
                  selection:
                    incrementalPlan: 0
                    path: $.user.email
                - directiveName: authenticated
                  selection:
                    incrementalPlan: 0
                    path: $.user.secret
              variables:
                - name: __fusion_policy_1
                  selections:
                    - incrementalPlan: 0
                      path: $.user.email
                - name: __fusion_policy_2
                  selections:
                    - incrementalPlan: 0
                      path: $.user.secret
            """);
    }

    [Fact]
    public void CreatePlan_Should_SkipNamesOfTheOperation_When_OperationDeclaresSyntheticVariableName()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);

        // act
        var plan = PlanOperation(
            schema,
            """
            query($__fusion_policy_1: Boolean = false) {
              product {
                name @include(if: $__fusion_policy_1)
                price
              }
            }
            """);

        // assert
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1(
                      $__fusion_policy_1: Boolean = false
                      $__fusion_policy_2: Boolean!
                      $__fusion_policy_3: Boolean!
                    ) {
                      product {
                        price @skip(if: $__fusion_policy_2)
                        name @include(if: $__fusion_policy_1) @skip(if: $__fusion_policy_3)
                      }
                    }
                  hash: 2509deeefbc3bed483d40695eaddcfbdd2bc49481de1436d1098275b632dffdc
                  shortHash: 2509deee
                  xxHash: 4009338969241049987
                resultSelectionSet: >-
                  { product }
                forwardedVariables:
                  - __fusion_policy_1
                  - __fusion_policy_2
                  - __fusion_policy_3
            policies:
              descriptors:
                - directiveName: requiresScopes
                  scopes:
                    - [read]
                  selection:
                    path: $.product.price
                - directiveName: authenticated
                  selection:
                    path: $.product.name
              variables:
                - name: __fusion_policy_2
                  selections:
                    - path: $.product.price
                - name: __fusion_policy_3
                  selections:
                    - path: $.product.name
            """);
    }

    [Fact]
    public void CreatePlan_Should_CollectOneDescriptorPerDirective_When_FieldCombinesDirectives()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              report: String
                @authenticated
                @requiresScopes(scopes: [["read"], ["admin"]])
                @policy(policies: [["a", "b"], ["c", "a"]])
            }

            {{Directives}}
            """);

        // act
        var plan = PlanOperation(schema, "{ report }");

        // assert
        plan.Policies.Descriptors
            .Select(static d => new
            {
                d.DirectiveName,
                d.PolicyName,
                d.Scopes,
                Selection = d.Selection.ResponseName
            })
            .ToArray()
            .MatchInlineSnapshot(
                """
                [
                  {
                    "DirectiveName": "authenticated",
                    "PolicyName": null,
                    "Scopes": [],
                    "Selection": "report"
                  },
                  {
                    "DirectiveName": "requiresScopes",
                    "PolicyName": null,
                    "Scopes": [
                      [
                        "admin"
                      ],
                      [
                        "read"
                      ]
                    ],
                    "Selection": "report"
                  },
                  {
                    "DirectiveName": "policy",
                    "PolicyName": "a",
                    "Scopes": [],
                    "Selection": "report"
                  },
                  {
                    "DirectiveName": "policy",
                    "PolicyName": "b",
                    "Scopes": [],
                    "Selection": "report"
                  },
                  {
                    "DirectiveName": "policy",
                    "PolicyName": "c",
                    "Scopes": [],
                    "Selection": "report"
                  }
                ]
                """);
    }

    [Fact]
    public void CreatePlan_Should_PinOnePolicyInstance_When_SeveralSelectionsUseTheSamePolicy()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              first: String @policy(policies: [["p"]])
              second: String @policy(policies: [["p"]])
            }

            {{Directives}}
            """);
        var resolver = CreateResolver(policies => policies.Allow("p"));

        // act
        var plan = PlanOperation(schema, "{ first second }", resolver);

        // assert
        var descriptors = plan.Policies.Descriptors;
        Assert.False(plan.Policies.IsEmpty);
        Assert.Equal(2, descriptors.Length);
        Assert.Same(descriptors[0].Policy, descriptors[1].Policy);
        Assert.IsType<InMemoryPolicy>(descriptors[0].Policy);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Throw_When_PolicyOfDescriptorIsNotRegistered()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              field: String @policy(policies: [["missing"]])
            }

            {{Directives}}
            """);
        var plan = PlanOperation(schema, "{ field }");
        var descriptor = Assert.Single(plan.Policies.Descriptors);
        var context = CreateContext(
            Authenticated(),
            new PolicyEvaluationEntry(descriptor, new Dictionary<string, object?>()));

        // act
        var exception = await Assert.ThrowsAsync<PolicyNotResolvedException>(
            async () => await descriptor.Policy.EvaluateAsync(context, TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "No policy provider knows the policy 'missing' of the directive '@policy'.",
            exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_ProduceSamePlanId_When_PolicyAnswersDiffer()
    {
        // arrange
        var schema = ComposeSchema(
            $$"""
            type Query {
              field: String @policy(policies: [["p"]])
            }

            {{Directives}}
            """);
        var allowing = CreateResolver(policies => policies.Allow("p"));
        var denying = CreateResolver(policies => policies.Deny("p"));

        // act
        var allowed = PlanOperation(schema, "{ field }", allowing);
        var denied = PlanOperation(schema, "{ field }", denying);

        // assert
        Assert.Equal(allowed.Id, denied.Id);
        Assert.Equal(
            new YamlOperationPlanFormatter().Format(allowed),
            new YamlOperationPlanFormatter().Format(denied));
    }

    [Fact]
    public void CreatePlan_Should_NotMarkOperation_When_NoSelectionIsProtected()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);

        // act
        var plan = PlanOperation(schema, "{ product { id } }");

        // assert
        Assert.Same(PolicyPlan.Empty, plan.Policies);
        Assert.True(plan.Policies.IsEmpty);
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation:
                  name: Op_123456789101112_1
                  kind: Query
                  document: |
                    query Op_123456789101112_1 {
                      product {
                        id
                      }
                    }
                  hash: 2faef13dd54dcce950f793b35ea4b9e24891095e4405bbf2920509a6f1791dc4
                  shortHash: 2faef13d
                  xxHash: 14157010451816306771
                resultSelectionSet: >-
                  { product }
            """);
    }

    [Fact]
    public void CreatePlan_Should_UseEmptyPolicyPlan_When_SchemaHasNoProtectedFields()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field: String
            }
            """);

        // act
        var plan = PlanOperation(schema, "{ field }");

        // assert
        Assert.Same(PolicyPlan.Empty, plan.Policies);
        Assert.True(plan.Policies.IsEmpty);
    }

    [Fact]
    public void CreatePlan_Should_FlagProtectedSelections_When_FieldsCarryPolicies()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);

        // act
        var plan = PlanOperation(schema, "{ product { id name price } secret }");

        // assert
        var product = plan.Operation.RootSelectionSet.Selections[0];
        new
        {
            Root = plan.Operation.RootSelectionSet.Selections.ToArray()
                .Select(static s => new { s.ResponseName, s.HasPolicies }),
            Product = product.GetSelectionSet()!.Selections.ToArray()
                .Select(static s => new { s.ResponseName, s.HasPolicies })
        }.MatchInlineSnapshot(
            """
            {
              "Root": [
                {
                  "ResponseName": "product",
                  "HasPolicies": false
                },
                {
                  "ResponseName": "secret",
                  "HasPolicies": true
                }
              ],
              "Product": [
                {
                  "ResponseName": "id",
                  "HasPolicies": false
                },
                {
                  "ResponseName": "name",
                  "HasPolicies": true
                },
                {
                  "ResponseName": "price",
                  "HasPolicies": true
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task CreatePlan_Should_UseRegisteredPolicies_When_PlannerIsCreatedByTheGateway()
    {
        // arrange
        var schemaDocument = ComposeSchemaDocument(
            $$"""
            type Query {
              field: String @policy(policies: [["p"]])
            }

            {{Directives}}
            """);
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddInMemoryPolicies(policies => policies.Allow("p"))
            .AddInMemoryConfiguration(schemaDocument)
            .ConfigureSchemaServices(
                (_, sc) => sc.AddSingleton<IAuthenticationSchemeLookup>(
                    new TestAuthenticationSchemeLookup("Bearer")));
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var schema = executor.Schema.Services.GetRequiredService<FusionSchemaDefinition>();
        var planner = executor.Schema.Services.GetRequiredService<OperationPlanner>();
        var operation = new DocumentRewriter(schema)
            .RewriteDocument(Utf8GraphQLParser.Parse("{ field }"), operationName: null)
            .Definitions.OfType<OperationDefinitionNode>()
            .First();

        // act
        var plan = planner.CreatePlan("1", "1", "1", operation, TestContext.Current.CancellationToken);

        // assert
        var descriptor = Assert.Single(plan.Policies.Descriptors);
        Assert.IsType<InMemoryPolicy>(descriptor.Policy);
    }

    private static IPolicyResolver CreateResolver(Action<InMemoryPolicyBuilder> configure)
    {
        var builder = new InMemoryPolicyBuilder();
        configure(builder);

        return new PolicyResolver(
            new BuiltInPolicyProvider(),
            [builder.Build(new InMemoryPolicyRecorder())]);
    }

    private static object Describe(OperationPlan plan)
    {
        var policies = plan.Policies;

        return new
        {
            Variables = policies.Variables.Select(
                static v => new
                {
                    v.Name,
                    Selections = v.Selections.Select(static s => s.ResponseName),
                    Operands = v.Operands.Select(static o => o.Name)
                })
        };
    }

    private static void MatchNodes(OperationPlan plan, [StringSyntax("yaml")] string expected)
    {
        var yaml = new YamlOperationPlanFormatter().Format(plan);
        var actual = yaml[yaml.IndexOf("nodes:", StringComparison.Ordinal)..];
        actual.MatchInlineSnapshot(expected + Environment.NewLine);
    }
}
