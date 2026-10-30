using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityShareableAbstractTypeTests
{
    [Theory]
    [InlineData(ShareableFieldRuntimeTypeRouting.SourceLocal)]
    [InlineData(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)]
    public void Validate_Should_Succeed_When_ShareableUnionFieldHasOverlappingMembersPerSchema(
        ShareableFieldRuntimeTypeRouting routing)
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                union Activity = Comment | Reaction

                type Comment {
                    body: String @shareable
                }

                type Reaction {
                    emoji: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                union Activity = Comment | Repost

                type Comment {
                    body: String @shareable
                }

                type Repost {
                    channel: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(routing, sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        result.Value.ToString().MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              activity: [Activity] @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Comment @fusion__type(schema: A) @fusion__type(schema: B) {
              body: String @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Reaction @fusion__type(schema: A) {
              emoji: String @fusion__field(schema: A)
            }

            type Repost @fusion__type(schema: B) {
              channel: String @fusion__field(schema: B)
            }

            union Activity
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__unionMember(schema: A, member: "Comment")
              @fusion__unionMember(schema: B, member: "Comment")
              @fusion__unionMember(schema: A, member: "Reaction")
              @fusion__unionMember(schema: B, member: "Repost") =
              | Comment
              | Reaction
              | Repost
            """);
    }

    [Theory]
    [InlineData(ShareableFieldRuntimeTypeRouting.SourceLocal)]
    [InlineData(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)]
    public void Validate_Should_Succeed_When_ShareableUnionFieldHasDisjointMembersPerSchema(
        ShareableFieldRuntimeTypeRouting routing)
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                union Activity = Reaction

                type Reaction {
                    emoji: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                union Activity = Repost

                type Repost {
                    channel: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(routing, sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        result.Value.ToString().MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              activity: [Activity] @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Reaction @fusion__type(schema: A) {
              emoji: String @fusion__field(schema: A)
            }

            type Repost @fusion__type(schema: B) {
              channel: String @fusion__field(schema: B)
            }

            union Activity
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__unionMember(schema: A, member: "Reaction")
              @fusion__unionMember(schema: B, member: "Repost") = Reaction | Repost
            """);
    }

    [Theory]
    [InlineData(ShareableFieldRuntimeTypeRouting.SourceLocal)]
    [InlineData(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)]
    public void Validate_Should_Succeed_When_ShareableInterfaceFieldHasOverlappingImplementationsPerSchema(
        ShareableFieldRuntimeTypeRouting routing)
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                interface Activity {
                    occurredAt: String
                }

                type Comment implements Activity {
                    occurredAt: String @shareable
                    body: String @shareable
                }

                type Reaction implements Activity {
                    occurredAt: String
                    emoji: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                interface Activity {
                    occurredAt: String
                }

                type Comment implements Activity {
                    occurredAt: String @shareable
                    body: String @shareable
                }

                type Repost implements Activity {
                    occurredAt: String
                    channel: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(routing, sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        result.Value.ToString().MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              activity: [Activity] @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Comment implements Activity
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__implements(schema: A, interface: "Activity")
              @fusion__implements(schema: B, interface: "Activity") {
              body: String @fusion__field(schema: A) @fusion__field(schema: B)
              occurredAt: String @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Reaction implements Activity
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Activity") {
              emoji: String @fusion__field(schema: A)
              occurredAt: String @fusion__field(schema: A)
            }

            type Repost implements Activity
              @fusion__type(schema: B)
              @fusion__implements(schema: B, interface: "Activity") {
              channel: String @fusion__field(schema: B)
              occurredAt: String @fusion__field(schema: B)
            }

            interface Activity @fusion__type(schema: A) @fusion__type(schema: B) {
              occurredAt: String @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """);
    }

    [Theory]
    [InlineData(ShareableFieldRuntimeTypeRouting.SourceLocal)]
    [InlineData(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)]
    public void Validate_Should_Succeed_When_ShareableInterfaceFieldHasDisjointImplementationsPerSchema(
        ShareableFieldRuntimeTypeRouting routing)
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                interface Activity {
                    occurredAt: String
                }

                type Reaction implements Activity {
                    occurredAt: String
                    emoji: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    activity: [Activity] @shareable
                }

                interface Activity {
                    occurredAt: String
                }

                type Repost implements Activity {
                    occurredAt: String
                    channel: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(routing, sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        result.Value.ToString().MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              activity: [Activity] @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Reaction implements Activity
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Activity") {
              emoji: String @fusion__field(schema: A)
              occurredAt: String @fusion__field(schema: A)
            }

            type Repost implements Activity
              @fusion__type(schema: B)
              @fusion__implements(schema: B, interface: "Activity") {
              channel: String @fusion__field(schema: B)
              occurredAt: String @fusion__field(schema: B)
            }

            interface Activity @fusion__type(schema: A) @fusion__type(schema: B) {
              occurredAt: String @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """);
    }
}
