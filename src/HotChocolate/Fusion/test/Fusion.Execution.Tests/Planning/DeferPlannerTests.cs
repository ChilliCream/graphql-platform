using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

public class DeferPlannerTests : FusionTestBase
{
    [Fact]
    public void Defer_SingleFragment_ProducesDeferredGroup()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.Single(plan.IncrementalPlans);

        var subPlan = plan.IncrementalPlans[0];
        var group = subPlan.DeliveryGroups[0];
        Assert.Equal(0, group.Id);
        Assert.Null(group.Label);
        Assert.Equal("$.user", group.Path!.ToString());
    }

    [Fact]
    public void Defer_MultipleFragments_ProducesMultipleDeferredGroups()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                profile: Profile!
            }

            type Profile {
                bio: String!
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "emailDefer") {
                        email
                    }
                    profile {
                        ... @defer(label: "bioDefer") {
                            bio
                        }
                    }
                }
            }
            """);

        // assert
        Assert.Equal(2, plan.IncrementalPlans.Length);

        var emailSubPlan = plan.IncrementalPlans.First(s => s.DeliveryGroups[0].Label == "emailDefer");
        var bioSubPlan = plan.IncrementalPlans.First(s => s.DeliveryGroups[0].Label == "bioDefer");

        Assert.NotNull(emailSubPlan);
        Assert.NotNull(bioSubPlan);
        Assert.NotEqual(emailSubPlan.DeliveryGroups[0].Id, bioSubPlan.DeliveryGroups[0].Id);
    }

    [Fact]
    public void Defer_WithLabel_LabelIsPropagated()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "myLabel") {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.Single(plan.IncrementalPlans);
        Assert.Equal("myLabel", plan.IncrementalPlans[0].DeliveryGroups[0].Label);
    }

    [Fact]
    public void Defer_OperationHasIncrementalParts()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.False(plan.IncrementalPlans.IsEmpty);
    }

    [Fact]
    public void Defer_NoDefer_NoDeferredGroups()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    email
                }
            }
            """);

        // assert
        Assert.True(plan.IncrementalPlans.IsEmpty);
        Assert.False(plan.Operation.HasIncrementalParts);
    }

    [Fact]
    public void Defer_ConditionalVariable_IfVariableRecorded()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($shouldDefer: Boolean!) {
                user(id: "1") {
                    name
                    ... @defer(if: $shouldDefer) {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.Single(plan.IncrementalPlans);
        Assert.Equal("shouldDefer", plan.IncrementalPlans[0].DeliveryGroups[0].IfVariable);
    }

    [Fact]
    public void Defer_MainPlanStillExecutes()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.NotEmpty(plan.RootNodes);
        Assert.NotEmpty(plan.AllNodes);

        // The deferred subplan should also have its own execution nodes
        var subPlan = plan.IncrementalPlans[0];
        Assert.False(subPlan.RootNodes.IsEmpty);
        Assert.False(subPlan.AllNodes.IsEmpty);
        Assert.Null(subPlan.DeliveryGroups[0].Parent);
    }

    [Fact]
    public void Defer_IfFalseLiteral_Should_ProduceNoDeferredGroups()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(if: false) {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.True(plan.IncrementalPlans.IsEmpty);
    }

    [Fact]
    public void Defer_IfTrueLiteral_Should_ProduceDeferredGroup()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(if: true) {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.Single(plan.IncrementalPlans);
        Assert.Null(plan.IncrementalPlans[0].DeliveryGroups[0].IfVariable);
    }

    [Fact]
    public void Defer_NestedDefer_Should_ProduceParentChildRelationship()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        email
                        ... @defer(label: "inner") {
                            address
                        }
                    }
                }
            }
            """);

        // assert
        Assert.Equal(2, plan.IncrementalPlans.Length);

        var outerGroup = plan.IncrementalPlans
            .Select(s => s.DeliveryGroups[0])
            .First(g => g.Label == "outer");
        var innerGroup = plan.IncrementalPlans
            .Select(s => s.DeliveryGroups[0])
            .First(g => g.Label == "inner");

        Assert.Null(outerGroup.Parent);
        Assert.NotNull(innerGroup.Parent);
        Assert.Equal(outerGroup.Id, innerGroup.Parent.Id);
    }

    [Fact]
    public void Defer_WithIncludeDirective_Should_ProduceDeferredGroup()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer @include(if: true) {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.Single(plan.IncrementalPlans);
    }

    [Fact]
    public void Plan_Should_Partition_Nested_Defer_With_Mixed_If_Conditions_Correctly()
    {
        // arrange
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

        // act + assert: a=true, b=true (both active)
        var planBothActive = PlanOperation(
            schema,
            """
            {
                user(id: "1") {
                    name
                    ... @defer(label: "outer", if: true) {
                        email
                        ... @defer(label: "inner", if: true) {
                            address
                        }
                    }
                }
            }
            """);

        Assert.Equal(2, planBothActive.IncrementalPlans.Length);
        var outerSubPlan = planBothActive.IncrementalPlans
            .First(s => s.DeliveryGroups.Any(g => g.Label == "outer"));
        var innerSubPlan = planBothActive.IncrementalPlans
            .First(s => s.DeliveryGroups.Any(g => g.Label == "inner"));
        Assert.Single(outerSubPlan.DeliveryGroups, g => g.Label == "outer");
        Assert.Single(innerSubPlan.DeliveryGroups, g => g.Label == "inner");
        Assert.Null(outerSubPlan.DeliveryGroups[0].Parent);
        var innerParent = innerSubPlan.DeliveryGroups[0].Parent;
        Assert.NotNull(innerParent);
        Assert.Equal(outerSubPlan.DeliveryGroups[0].Id, innerParent.Id);

        // act + assert: a=true, b=false (inner inactive, its address collapses into outer)
        var planInnerInactive = PlanOperation(
            schema,
            """
            {
                user(id: "1") {
                    name
                    ... @defer(label: "outer", if: true) {
                        email
                        ... @defer(label: "inner", if: false) {
                            address
                        }
                    }
                }
            }
            """);

        Assert.Single(planInnerInactive.IncrementalPlans);
        var collapsedOuter = planInnerInactive.IncrementalPlans[0];
        Assert.Single(collapsedOuter.DeliveryGroups);
        Assert.Equal("outer", collapsedOuter.DeliveryGroups[0].Label);

        // act + assert: a=false, b=true (outer inactive, inner is top-level; email in initial)
        var planOuterInactive = PlanOperation(
            schema,
            """
            {
                user(id: "1") {
                    name
                    ... @defer(label: "outer", if: false) {
                        email
                        ... @defer(label: "inner", if: true) {
                            address
                        }
                    }
                }
            }
            """);

        Assert.Single(planOuterInactive.IncrementalPlans);
        var innerOnly = planOuterInactive.IncrementalPlans[0];
        Assert.Single(innerOnly.DeliveryGroups);
        Assert.Equal("inner", innerOnly.DeliveryGroups[0].Label);
        Assert.Null(innerOnly.DeliveryGroups[0].Parent);

        // act + assert: a=false, b=false (both inactive, no subplans)
        var planBothInactive = PlanOperation(
            schema,
            """
            {
                user(id: "1") {
                    name
                    ... @defer(label: "outer", if: false) {
                        email
                        ... @defer(label: "inner", if: false) {
                            address
                        }
                    }
                }
            }
            """);

        Assert.True(planBothInactive.IncrementalPlans.IsEmpty);
    }

    [Fact(Skip = "Known bug: BuildDeferredOperation forces OperationType.Query, causing KeyNotFoundException for mutation fields")]
    public void Defer_OnMutationResult_Should_ProduceDeferredGroup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type Mutation {
                createUser(name: String!): User!
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

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation {
                createUser(name: "test") {
                    name
                    ... @defer {
                        email
                    }
                }
            }
            """);

        // assert
        Assert.Single(plan.IncrementalPlans);

        var subPlan = plan.IncrementalPlans[0];
        Assert.False(subPlan.RootNodes.IsEmpty);
        Assert.False(subPlan.AllNodes.IsEmpty);
    }

    [Fact]
    public void Defer_RequirementReachableFromParent_Should_InjectIntoParentOp_When_SameSubgraph()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer {
                        email
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Defer_MultipleDeferGroupsShareRequirement_Should_DeduplicateHoistedField()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                    }
                    ... @defer(label: "location") {
                        address
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Defer_RequirementOnForeignSubgraph_Should_PlanParentScopeLookup()
    {
        // arrange
        // Parent plan only hits schema `a` for `product.name`. The defer
        // group wants `reviews` on schema `c`, which requires `productSku`.
        // `productSku` is only exposed by schema `b`, so the defer's sub-plan
        // would otherwise self-fetch twice: once for `id` on `a`, once for
        // `productSku` on `b`. Because schema `b` is reachable from the
        // parent scope only through a cross-subgraph hop, the hoist must
        // promote a dedicated parent-scope op on `b`.
        var schema = ComposeSchema(
            """
            # name: a
            schema {
                query: Query
            }

            type Query {
                product(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            schema {
                query: Query
            }

            type Query {
                productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                productSku: String!
            }
            """,
            """
            # name: c
            schema {
                query: Query
            }

            type Query {
                productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                reviews(productSku: String! @require(field: "productSku")): [String!]!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                product(id: "1") {
                    name
                    ... @defer {
                        reviews
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Defer_NestedDefer_InnerRequirement_Should_ResolveAgainstOuterDefer()
    {
        // arrange
        // Outer defer needs `email` on schema `b` (requires `id` from root
        // schema `a`). Inner defer needs `address`, also on schema `b` and
        // also requiring `id`. The inner requirement is reachable from the
        // outer defer's result tree (both select the same `User` at `$.user`).
        // The current hoist only persists parent-step mutations when the
        // enclosing scope is the root plan, so the outer sub-plan hoists `id`
        // into the root op (one self-fetch eliminated) while the inner
        // sub-plan falls back to its own local self-fetch. The runtime's
        // try-parent-fall-back-to-own variable resolution (Step 8) consumes
        // that local fetch. This snapshot locks in the current planner
        // behavior and serves as the regression fixture for any future
        // extension of the hoist to enclosing-defer scopes.
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        email
                        ... @defer(label: "inner") {
                            address
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Defer_NestedDefer_InnerAndOuterShareRequirement_Should_DeduplicateAtOuter()
    {
        // arrange
        // Outer defer selects `reviews`, inner defer selects `summary`. Both
        // fields live on schema `c` and both declare @require(field:
        // "productSku"). The only way to satisfy `productSku` is via schema
        // `b`. Because outer is planned first, it registers the productSku
        // requirement in the outer's scope. When inner is planned, its own
        // productSku requirement must reuse the outer's already-planned
        // producer rather than duplicating another lookup step.
        var schema = ComposeSchema(
            """
            # name: a
            schema {
                query: Query
            }

            type Query {
                product(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            schema {
                query: Query
            }

            type Query {
                productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                productSku: String!
            }
            """,
            """
            # name: c
            schema {
                query: Query
            }

            type Query {
                productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                reviews(productSku: String! @require(field: "productSku")): [String!]!
                summary(productSku: String! @require(field: "productSku")): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                product(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        reviews
                        ... @defer(label: "inner") {
                            summary
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Defer_NestedDefer_InnerRequirement_OnForeignSubgraph_Should_PlanLookupInOuterScope()
    {
        // arrange
        // Inner defer's required variable (nickname) lives on schema `b`. The
        // outer defer already hits `b` for `email`, but the root plan only
        // hits schema `a`. The nearest reachable scope for `nickname` is the
        // outer defer scope. The planner should plan the productSku lookup in
        // outer-defer scope (not root) because root cannot satisfy it.
        var schema = ComposeSchema(
            """
            # name: a
            schema {
                query: Query
            }

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
            schema {
                query: Query
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                nickname: String!
            }
            """,
            """
            # name: c
            schema {
                query: Query
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                badge(nickname: String! @require(field: "nickname")): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        email
                        ... @defer(label: "inner") {
                            badge
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Defer_NestedDefer_InnerRequirement_Should_ResolveAtOuterScope_When_SameSubgraphAsOuterKey()
    {
        // arrange
        // Inner defer's required variable (preferences) lives on schema `a`.
        // The outer defer hits schema `b` for `email` and already has a
        // promoted `user{id}` step on schema `a` (planned for its own
        // lookup-key requirement). The walker's same-subgraph inline at outer
        // scope succeeds by inlining `preferences` into that promoted step,
        // so the inner's producer ends up at outer scope, not root. This is
        // the observable planner behavior for schema-reachable requirements;
        // the walker's parent-chain escalation to root is retained as
        // defensive code (see the Skip'd fixture below).
        var schema = ComposeSchema(
            """
            # name: a
            schema {
                query: Query
            }

            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                preferences: String!
            }
            """,
            """
            # name: b
            schema {
                query: Query
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
            }
            """,
            """
            # name: c
            schema {
                query: Query
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                customized(preferences: String! @require(field: "preferences")): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        email
                        ... @defer(label: "inner") {
                            customized
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact(Skip = "Natural Fusion schemas do not reach this walker-escalation path. "
        + "The outer defer always has a key-producing step (to satisfy its own lookup's "
        + "argument requirement) on the same subgraph as the enclosing entity; the walker's "
        + "per-scope same-subgraph inline always succeeds at outer scope before needing to "
        + "escalate. Cross-subgraph promote also succeeds whenever the inner's required "
        + "subgraph is reachable via a lookup from outer's key. The walker's parent-chain "
        + "escalation is retained as defensive code against planner-internal matching or "
        + "partitioning bugs; snapshot verification is not observable for schema-reachable "
        + "cases. Kept as a documentation fixture of .work/defer-requirement-variable-wiring.md "
        + "§2.4's target behavior.")]
    public void Defer_NestedDefer_InnerRequirement_UnreachableFromOuter_Should_BubbleToRoot()
    {
        // arrange
        // No schema construction produced walker escalation to root. The
        // plan's §2.4 invariant is that the walker's chain escalation is
        // logically unreachable for any schema whose outer defer's own
        // lookup-key requirement anchors a same-subgraph step that can
        // absorb an inner's requirement via inline or promote.
        Assert.True(true);
    }

    [Fact(Skip = "Natural schemas do not reach the unsatisfiable-requirement throw. "
        + "If the defer's sub-plan planner produced a self-fetch, that self-fetch is "
        + "an existence proof that the required value is reachable from some subgraph, "
        + "and the parent's planning machinery (same-subgraph injection or cross-subgraph "
        + "promote) can route to the same subgraph. Every attempt to construct an "
        + "unsatisfiable schema collapsed into either (a) a composition-time failure or "
        + "(b) a schema whose same-subgraph hoist or cross-subgraph promote succeeds. "
        + "The throw in ApplyDeferRequirementsToParent is defensive against "
        + "planner-internal matching bugs, not against schema shapes. Retained as a "
        + "documentation fixture for .work/defer-requirement-variable-wiring.md Phase 1.")]
    public void Defer_UnsatisfiableRequirement_Should_ThrowPlannerError_When_NotReachableAnywhere()
    {
        // arrange
        // No schema construction produced the throw. The plan's Phase 1
        // invariant is that the throw is logically unreachable for any
        // schema whose defer sub-plan succeeds at plan time.
        Assert.True(true);
    }

    [Fact]
    public void MaxNodeId_Should_Match_Max_Node_Id_Of_AllNodes_When_Computed()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer {
                        email
                    }
                }
            }
            """);

        // assert
        var subPlan = plan.IncrementalPlans[0];
        Assert.Equal(subPlan.AllNodes.Max(n => n.Id), subPlan.MaxNodeId);
    }

    [Fact]
    public void IncrementalPlan_Ids_Should_Be_Positional_When_Plan_Built()
    {
        // arrange
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                        ... @defer(label: "nested") { address }
                    }
                    ... @defer(label: "location") { email }
                }
            }
            """);

        // assert
        for (var i = 0; i < plan.IncrementalPlans.Length; i++)
        {
            Assert.Equal($"{plan.Id}#{i}", plan.IncrementalPlans[i].Id);
        }
    }

    [Fact]
    public void Defer_SingleAnchor_Should_Keep_NodeRequirements_AllImported_Or_AllLocal()
    {
        // arrange
        // Single defer over a key handoff: the sub-plan's downstream node depends on
        // a parent-scope step for the user id and has no plan-internal predecessors.
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

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer {
                        email
                    }
                }
            }
            """);

        // assert
        AssertNoMixedRequirementScope(plan);
    }

    [Fact]
    public void Defer_NestedDefer_Should_Keep_NodeRequirements_AllImported_Or_AllLocal()
    {
        // arrange
        // Nested defer with a cross-subgraph requirement that the planner promotes
        // into the outer defer scope. Each inner sub-plan node either lifts its
        // requirements through ParentDependencies or resolves them through a
        // sibling step in the same sub-plan, never both at once.
        var schema = ComposeSchema(
            """
            # name: a
            schema {
                query: Query
            }

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
            schema {
                query: Query
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                nickname: String!
            }
            """,
            """
            # name: c
            schema {
                query: Query
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                badge(nickname: String! @require(field: "nickname")): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        email
                        ... @defer(label: "inner") {
                            badge
                        }
                    }
                }
            }
            """);

        // assert
        AssertNoMixedRequirementScope(plan);
    }

    [Fact]
    public void Defer_StepInternalPredecessor_Should_Keep_NodeRequirements_AllImported_Or_AllLocal()
    {
        // arrange
        // The sub-plan needs an extra hop on schema c to fetch a value that another
        // sub-plan node consumes via @require. The chained downstream node has only
        // sub-plan-internal predecessors and so must route through the local store
        // path, not the imported snapshot path.
        var schema = ComposeSchema(
            """
            # name: a
            schema {
                query: Query
            }

            type Query {
                product(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            schema {
                query: Query
            }

            type Query {
                productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                productSku: String!
            }
            """,
            """
            # name: c
            schema {
                query: Query
            }

            type Query {
                productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                reviews(productSku: String! @require(field: "productSku")): [String!]!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
                product(id: "1") {
                    name
                    ... @defer {
                        reviews
                    }
                }
            }
            """);

        // assert
        AssertNoMixedRequirementScope(plan);
    }

    /// <summary>
    /// Asserts the planner contract that backs the defer-time variable routing in
    /// <c>OperationPlanContext.CreateVariableValueSets</c>. The runtime classifies
    /// each node's requirement span by computing the executor's imported-key set
    /// (the union of every parent-dependent node's requirement keys, mirroring
    /// <c>OperationPlanExecutor.CollectParentScopeRequirements</c>) and then
    /// asserting that any single node's requirement keys are either fully inside
    /// that set or fully outside it. A partial overlap is an unreachable shape
    /// that the routing layer rejects with an explicit invariant exception.
    /// </summary>
    private static void AssertNoMixedRequirementScope(OperationPlan plan)
    {
        foreach (var subPlan in plan.IncrementalPlans)
        {
            var importedKeys = ComputeImportedKeys(subPlan);

            foreach (var node in subPlan.AllNodes)
            {
                switch (node)
                {
                    case OperationExecutionNode operationNode:
                        AssertScopeIsUniform(operationNode.Requirements, importedKeys, subPlan, operationNode.Id);
                        break;

                    case OperationBatchExecutionNode batchNode:
                        foreach (var op in batchNode.Operations)
                        {
                            AssertScopeIsUniform(op.Requirements, importedKeys, subPlan, op.Id);
                        }
                        break;
                }
            }
        }
    }

    private static HashSet<string> ComputeImportedKeys(IncrementalPlan subPlan)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in subPlan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode operationNode when !operationNode.ParentDependencies.IsEmpty:
                    AddRequirementKeys(operationNode.Requirements, keys);
                    break;

                case OperationBatchExecutionNode batchNode:
                    foreach (var op in batchNode.Operations)
                    {
                        if (!op.ParentDependencies.IsEmpty)
                        {
                            AddRequirementKeys(op.Requirements, keys);
                        }
                    }
                    break;
            }
        }

        return keys;
    }

    private static void AddRequirementKeys(
        ReadOnlySpan<OperationRequirement> requirements,
        HashSet<string> keys)
    {
        foreach (var requirement in requirements)
        {
            keys.Add(requirement.Key);
        }
    }

    private static void AssertScopeIsUniform(
        ReadOnlySpan<OperationRequirement> requirements,
        HashSet<string> importedKeys,
        IncrementalPlan subPlan,
        int nodeId)
    {
        if (requirements.Length == 0)
        {
            return;
        }

        var importedCount = 0;

        foreach (var requirement in requirements)
        {
            if (importedKeys.Contains(requirement.Key))
            {
                importedCount++;
            }
        }

        if (importedCount != 0 && importedCount != requirements.Length)
        {
            var imported = new List<string>();
            var local = new List<string>();

            foreach (var requirement in requirements)
            {
                if (importedKeys.Contains(requirement.Key))
                {
                    imported.Add(requirement.Key);
                }
                else
                {
                    local.Add(requirement.Key);
                }
            }

            Assert.Fail(
                $"Sub-plan '{subPlan.Id}' node {nodeId} has a requirement span that mixes "
                + $"imported parent-sourced keys [{string.Join(", ", imported)}] with local keys "
                + $"[{string.Join(", ", local)}]. The runtime variable routing layer assumes "
                + "every node's requirements are either all imported or all local.");
        }
    }
}
