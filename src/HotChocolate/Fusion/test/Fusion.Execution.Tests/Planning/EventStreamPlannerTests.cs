using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;

namespace HotChocolate.Fusion.Planning;

public sealed class EventStreamPlannerTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_UseStandardDependents_When_EventStreamHasSingleMessageShape()
    {
        // arrange
        var schema = CreateCompositeSchema(EventStreamSchema);

        // act
        var plan = PlanOperation(
            schema,
            """
            subscription {
              bookChanged {
                title
              }
            }
            """);

        // assert
        var root = Assert.IsType<EventStreamExecutionNode>(Assert.Single(plan.RootNodes));
        Assert.Equal(["book.changed"], root.EventStreamSource.Topics);
        Assert.Equal("{ id }", root.Message);
        Assert.True(root.Dependents.Length > 0);
    }

    [Fact]
    public void FormatPlan_Should_WriteTopics_When_EventStreamHasSource()
    {
        // arrange
        var schema = CreateCompositeSchema(EventStreamSchema);
        var plan = PlanOperation(
            schema,
            """
            subscription {
              bookChanged {
                title
              }
            }
            """);

        // act
        var json = new JsonOperationPlanFormatter().Format(plan);

        // assert
        Assert.Contains("\"topics\":[\"book.changed\"]", json, StringComparison.Ordinal);
    }

    private const string EventStreamSchema =
        """
        schema {
          query: Query
          subscription: Subscription
        }

        type Query
          @fusion__type(schema: BOOKS)
          @fusion__type(schema: AUTHORS) {
          bookById(id: ID!): Book
            @fusion__field(schema: BOOKS)
          authorById(id: ID!): Author
            @fusion__field(schema: AUTHORS)
        }

        type Subscription
          @fusion__type(schema: EVENTS) {
          bookChanged: Book
            @fusion__field(schema: EVENTS)
            @fusion__eventStream(
              schema: EVENTS
              topics: ["book.changed"]
              message: "{ id }"
            )
          changed: Node
            @fusion__field(schema: EVENTS)
            @fusion__eventStream(
              schema: EVENTS
              topics: ["book.changed"]
              message: "{ __typename ... on Book { id } }"
            )
        }

        interface Node
          @fusion__type(schema: EVENTS)
          @fusion__type(schema: BOOKS)
          @fusion__type(schema: AUTHORS) {
          id: ID!
            @fusion__field(schema: EVENTS)
            @fusion__field(schema: BOOKS)
            @fusion__field(schema: AUTHORS)
        }

        type Book implements Node
          @fusion__type(schema: EVENTS)
          @fusion__type(schema: BOOKS)
          @fusion__lookup(
            schema: BOOKS
            key: "{ id }"
            field: "bookById(id: ID!): Book"
            map: ["id"]
            internal: false
          ) {
          id: ID!
            @fusion__field(schema: EVENTS)
            @fusion__field(schema: BOOKS)
          title: String!
            @fusion__field(schema: BOOKS)
        }

        type Author implements Node
          @fusion__type(schema: EVENTS)
          @fusion__type(schema: AUTHORS)
          @fusion__lookup(
            schema: AUTHORS
            key: "{ id }"
            field: "authorById(id: ID!): Author"
            map: ["id"]
            internal: false
          ) {
          id: ID!
            @fusion__field(schema: EVENTS)
            @fusion__field(schema: AUTHORS)
          name: String!
            @fusion__field(schema: AUTHORS)
        }

        enum fusion__Schema {
          EVENTS
          BOOKS
          AUTHORS
        }

        scalar fusion__FieldDefinition
        scalar fusion__FieldSelectionMap
        scalar fusion__FieldSelectionSet

        directive @fusion__type(
          schema: fusion__Schema!
        ) repeatable on OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT | SCALAR

        directive @fusion__field(
          schema: fusion__Schema!
          sourceName: String
          sourceType: String
          provides: fusion__FieldSelectionSet
          external: Boolean! = false
        ) repeatable on FIELD_DEFINITION

        directive @fusion__lookup(
          schema: fusion__Schema!
          key: fusion__FieldSelectionSet!
          field: fusion__FieldDefinition!
          map: [fusion__FieldSelectionMap!]!
          internal: Boolean! = false
        ) repeatable on OBJECT | INTERFACE

        directive @fusion__eventStream(
          schema: fusion__Schema!
          topics: [String!]
          broker: String
          message: fusion__FieldSelectionSet!
        ) on FIELD_DEFINITION
        """;
}
