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
        Assert.Single(plan.DeferredSubPlans);

        var subPlan = plan.DeferredSubPlans[0];
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
        Assert.Equal(2, plan.DeferredSubPlans.Length);

        var emailSubPlan = plan.DeferredSubPlans.First(s => s.DeliveryGroups[0].Label == "emailDefer");
        var bioSubPlan = plan.DeferredSubPlans.First(s => s.DeliveryGroups[0].Label == "bioDefer");

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
        Assert.Single(plan.DeferredSubPlans);
        Assert.Equal("myLabel", plan.DeferredSubPlans[0].DeliveryGroups[0].Label);
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
        Assert.False(plan.DeferredSubPlans.IsEmpty);
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
        Assert.True(plan.DeferredSubPlans.IsEmpty);
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
        Assert.Single(plan.DeferredSubPlans);
        Assert.Equal("shouldDefer", plan.DeferredSubPlans[0].DeliveryGroups[0].IfVariable);
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
        var subPlan = plan.DeferredSubPlans[0];
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
        Assert.True(plan.DeferredSubPlans.IsEmpty);
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
        Assert.Single(plan.DeferredSubPlans);
        Assert.Null(plan.DeferredSubPlans[0].DeliveryGroups[0].IfVariable);
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
        Assert.Equal(2, plan.DeferredSubPlans.Length);

        var outerGroup = plan.DeferredSubPlans
            .Select(s => s.DeliveryGroups[0])
            .First(g => g.Label == "outer");
        var innerGroup = plan.DeferredSubPlans
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
        Assert.Single(plan.DeferredSubPlans);
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

        Assert.Equal(2, planBothActive.DeferredSubPlans.Length);
        var outerSubPlan = planBothActive.DeferredSubPlans
            .First(s => s.DeliveryGroups.Any(g => g.Label == "outer"));
        var innerSubPlan = planBothActive.DeferredSubPlans
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

        Assert.Single(planInnerInactive.DeferredSubPlans);
        var collapsedOuter = planInnerInactive.DeferredSubPlans[0];
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

        Assert.Single(planOuterInactive.DeferredSubPlans);
        var innerOnly = planOuterInactive.DeferredSubPlans[0];
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

        Assert.True(planBothInactive.DeferredSubPlans.IsEmpty);
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
        Assert.Single(plan.DeferredSubPlans);

        var subPlan = plan.DeferredSubPlans[0];
        Assert.False(subPlan.RootNodes.IsEmpty);
        Assert.False(subPlan.AllNodes.IsEmpty);
    }
}
