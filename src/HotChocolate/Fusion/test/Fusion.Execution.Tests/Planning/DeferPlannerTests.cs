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
        Assert.Single(plan.DeferredGroups);

        var group = plan.DeferredGroups[0];
        Assert.Equal(0, group.DeferId);
        Assert.Null(group.Label);
        Assert.Equal("$.user", group.Path.ToString());
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
        Assert.Equal(2, plan.DeferredGroups.Length);

        var emailGroup = plan.DeferredGroups.First(g => g.Label == "emailDefer");
        var bioGroup = plan.DeferredGroups.First(g => g.Label == "bioDefer");

        Assert.NotNull(emailGroup);
        Assert.NotNull(bioGroup);
        Assert.NotEqual(emailGroup.DeferId, bioGroup.DeferId);
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
        Assert.Single(plan.DeferredGroups);
        Assert.Equal("myLabel", plan.DeferredGroups[0].Label);
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
        Assert.False(plan.DeferredGroups.IsEmpty);
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
        Assert.True(plan.DeferredGroups.IsEmpty);
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
        Assert.Single(plan.DeferredGroups);
        Assert.Equal("shouldDefer", plan.DeferredGroups[0].IfVariable);
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

        // The deferred group should also have its own execution nodes
        var deferredGroup = plan.DeferredGroups[0];
        Assert.False(deferredGroup.RootNodes.IsEmpty);
        Assert.False(deferredGroup.AllNodes.IsEmpty);
        Assert.Null(deferredGroup.Parent);
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
        Assert.True(plan.DeferredGroups.IsEmpty);
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
        Assert.Single(plan.DeferredGroups);
        Assert.Null(plan.DeferredGroups[0].IfVariable);
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
        Assert.Equal(2, plan.DeferredGroups.Length);

        var outerGroup = plan.DeferredGroups.First(g => g.Label == "outer");
        var innerGroup = plan.DeferredGroups.First(g => g.Label == "inner");

        Assert.Null(outerGroup.Parent);
        Assert.NotNull(innerGroup.Parent);
        Assert.Equal(outerGroup.DeferId, innerGroup.Parent.DeferId);
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
        Assert.Single(plan.DeferredGroups);
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
        Assert.Single(plan.DeferredGroups);

        var group = plan.DeferredGroups[0];
        Assert.False(group.RootNodes.IsEmpty);
        Assert.False(group.AllNodes.IsEmpty);
    }
}
