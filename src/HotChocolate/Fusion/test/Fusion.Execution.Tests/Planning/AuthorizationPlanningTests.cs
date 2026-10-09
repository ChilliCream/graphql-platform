using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Authorization;
using HotChocolate.Fusion.Authorization.InMemory;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Authorization.PolicyTestHelper;

namespace HotChocolate.Fusion.Planning;

public class AuthorizationPlanningTests : FusionTestBase
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
    public void CreatePlan_Should_SkipProtectedFieldsWithSyntheticVariables_When_FieldsCarryAuthorization()
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
                operation: |
                  query Op_123456789101112_1(
                    $__fusion_auth_1: Boolean!
                    $__fusion_auth_2: Boolean!
                  ) {
                    product {
                      id
                      name @skip(if: $__fusion_auth_1)
                      price @skip(if: $__fusion_auth_2)
                    }
                  }
                forwardedVariables:
                  - __fusion_auth_1
                  - __fusion_auth_2
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
                operation: |
                  query Op_123456789101112_1(
                    $__fusion_auth_1: Boolean!
                    $__fusion_auth_2: Boolean!
                  ) {
                    product {
                      first: name @skip(if: $__fusion_auth_1)
                      second: name @skip(if: $__fusion_auth_2)
                    }
                  }
                forwardedVariables:
                  - __fusion_auth_1
                  - __fusion_auth_2
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
                operation: |
                  query Op_123456789101112_1($skip: Boolean!, $__fusion_auth_1: Boolean!) {
                    product {
                      ... @skip(if: $__fusion_auth_1) {
                        name @skip(if: $skip)
                      }
                    }
                  }
                forwardedVariables:
                  - skip
                  - __fusion_auth_1
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
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!) {
                    productById(id: $__fusion_1_id) {
                      price
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                conditions:
                  - variable: $__fusion_auth_1
                    passingValue: false
                dependencies:
                  - id: 1
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
                  "Name": "__fusion_auth_1",
                  "Selections": [
                    "price"
                  ],
                  "Operands": []
                },
                {
                  "Name": "__fusion_auth_2",
                  "Selections": [
                    "note"
                  ],
                  "Operands": []
                },
                {
                  "Name": "__fusion_auth_3",
                  "Selections": [],
                  "Operands": [
                    "__fusion_auth_1",
                    "__fusion_auth_2"
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
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2(
                    $__fusion_auth_1: Boolean!
                    $__fusion_auth_2: Boolean!
                    $__fusion_1_id: ID!
                  ) {
                    productById(id: $__fusion_1_id) {
                      price @skip(if: $__fusion_auth_1)
                      note @skip(if: $__fusion_auth_2)
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                conditions:
                  - variable: $__fusion_auth_3
                    passingValue: false
                forwardedVariables:
                  - __fusion_auth_1
                  - __fusion_auth_2
                dependencies:
                  - id: 1
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
                operation: |
                  query Op_123456789101112_1($__fusion_auth_1: Boolean!) {
                    products {
                      id
                      weight @skip(if: $__fusion_auth_1)
                    }
                  }
                forwardedVariables:
                  - __fusion_auth_1
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!, $__fusion_2_weight: Int) {
                    productById(id: $__fusion_1_id) {
                      shippingEstimate(weight: $__fusion_2_weight)
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    selectionMap: >-
                      weight
                conditions:
                  - variable: $__fusion_auth_1
                    passingValue: false
                dependencies:
                  - id: 1
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
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                      weight
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2(
                    $__fusion_auth_1: Boolean!
                    $__fusion_auth_2: Boolean!
                    $__fusion_1_id: ID!
                    $__fusion_2_weight: Int
                  ) {
                    productById(id: $__fusion_1_id) {
                      note @skip(if: $__fusion_auth_1)
                      shippingEstimate(weight: $__fusion_2_weight) @skip(if: $__fusion_auth_2)
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    selectionMap: >-
                      weight
                conditions:
                  - variable: $__fusion_auth_3
                    passingValue: false
                forwardedVariables:
                  - __fusion_auth_1
                  - __fusion_auth_2
                dependencies:
                  - id: 1
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
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      id
                      weight
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2(
                    $__fusion_auth_1: Boolean!
                    $__fusion_1_id: ID!
                    $__fusion_2_weight: Int
                  ) {
                    productById(id: $__fusion_1_id) {
                      open
                      shippingEstimate(weight: $__fusion_2_weight) @skip(if: $__fusion_auth_1)
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    selectionMap: >-
                      weight
                forwardedVariables:
                  - __fusion_auth_1
                dependencies:
                  - id: 1
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
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      weight
                      id
                    }
                  }
              - id: 2
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!, $__fusion_2_weight: Int) {
                    productById(id: $__fusion_1_id) {
                      shippingEstimate(weight: $__fusion_2_weight)
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_weight
                    selectionMap: >-
                      weight
                conditions:
                  - variable: $__fusion_auth_1
                    passingValue: false
                dependencies:
                  - id: 1
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
                operation: |
                  query Op_123456789101112_1($__fusion_auth_1: Boolean!) {
                    search {
                      __typename
                      ... on Book {
                        title @skip(if: $__fusion_auth_1)
                      }
                      ... on Movie {
                        title
                      }
                    }
                  }
                forwardedVariables:
                  - __fusion_auth_1
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
                operation: |
                  mutation Op_123456789101112_1 {
                    delete
                  }
                conditions:
                  - variable: $__fusion_auth_1
                    passingValue: false
              - id: 2
                type: Operation
                schema: a
                operation: |
                  mutation Op_123456789101112_2 {
                    update
                  }
                conditions:
                  - variable: $__fusion_auth_2
                    passingValue: false
              - id: 3
                type: Operation
                schema: a
                operation: |
                  mutation Op_123456789101112_3 {
                    create
                  }
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
                operation: |
                  subscription Op_123456789101112_1 {
                    changed
                  }
                conditions:
                  - variable: $__fusion_auth_1
                    passingValue: false
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
                operation: |
                  query Op_123456789101112_1 {
                    user(id: 1) {
                      name
                      id
                    }
                  }
            deliveryGroups:
              - id: 0
                path: $.user
            incrementalPlans:
              - deliveryGroupIds:
                  - 0
                parentNodeId: 1
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_id
                    selectionMap: >-
                      id
                nodes:
                  - id: 1
                    type: Operation
                    schema: b
                    operation: |
                      query Op_defer_1($__fusion_auth_1: Boolean!, $__fusion_1_id: ID!) {
                        userById(id: $__fusion_1_id) {
                          email @skip(if: $__fusion_auth_1)
                          nickname
                        }
                      }
                    source: $.userById
                    target: $.user
                    requirements:
                      - name: __fusion_1_id
                        selectionMap: >-
                          id
                    forwardedVariables:
                      - __fusion_auth_1
                    dependencies:
                      - parentNodeId: 1
                  - id: 2
                    type: Operation
                    schema: a
                    operation: |
                      query Op_defer_2($__fusion_2_id: ID!) {
                        user(id: $__fusion_2_id) {
                          secret
                        }
                      }
                    source: $.user
                    target: $.user
                    requirements:
                      - name: __fusion_2_id
                        selectionMap: >-
                          id
                    conditions:
                      - variable: $__fusion_auth_2
                        passingValue: false
                    dependencies:
                      - parentNodeId: 1
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
            query($__fusion_auth_1: Boolean = false) {
              product {
                name @include(if: $__fusion_auth_1)
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
                operation: |
                  query Op_123456789101112_1(
                    $__fusion_auth_1: Boolean = false
                    $__fusion_auth_2: Boolean!
                    $__fusion_auth_3: Boolean!
                  ) {
                    product {
                      price @skip(if: $__fusion_auth_2)
                      name @include(if: $__fusion_auth_1) @skip(if: $__fusion_auth_3)
                    }
                  }
                forwardedVariables:
                  - __fusion_auth_1
                  - __fusion_auth_2
                  - __fusion_auth_3
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
        plan.Operation.Authorization!.Descriptors
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
        var descriptors = plan.Operation.Authorization!.Descriptors;
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
        var descriptor = Assert.Single(plan.Operation.Authorization!.Descriptors);
        var context = CreateContext(
            Authenticated(),
            new PolicyEvaluationEntry(descriptor, new Dictionary<string, object?>()));

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
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
        Assert.Null(plan.Operation.Authorization);
        MatchNodes(
            plan,
            """
            nodes:
              - id: 1
                type: Operation
                schema: a
                operation: |
                  query Op_123456789101112_1 {
                    product {
                      id
                    }
                  }
            """);
    }

    [Fact]
    public void SetAuthorization_Should_Throw_When_PlannerAlreadySetAuthorization()
    {
        // arrange
        var schema = ComposeSchema(ProductSchema);
        var plan = PlanOperation(schema, "{ product { id } }");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => plan.Operation.SetAuthorization(null));

        // assert
        Assert.Equal(
            "The authorization of the operation was already set and cannot be set again.",
            exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_FlagProtectedSelections_When_FieldsCarryAuthorization()
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
                .Select(static s => new { s.ResponseName, s.HasAuthorization }),
            Product = product.GetSelectionSet()!.Selections.ToArray()
                .Select(static s => new { s.ResponseName, s.HasAuthorization })
        }.MatchInlineSnapshot(
            """
            {
              "Root": [
                {
                  "ResponseName": "product",
                  "HasAuthorization": false
                },
                {
                  "ResponseName": "secret",
                  "HasAuthorization": true
                }
              ],
              "Product": [
                {
                  "ResponseName": "id",
                  "HasAuthorization": false
                },
                {
                  "ResponseName": "name",
                  "HasAuthorization": true
                },
                {
                  "ResponseName": "price",
                  "HasAuthorization": true
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
        var descriptor = Assert.Single(plan.Operation.Authorization!.Descriptors);
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
        var authorization = plan.Operation.Authorization!;

        return new
        {
            Variables = authorization.Variables.Select(
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
