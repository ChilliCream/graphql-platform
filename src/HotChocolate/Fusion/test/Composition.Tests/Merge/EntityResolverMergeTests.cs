using CookieCrumble;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class EntityResolverMergeTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Extract_Entity_Resolvers_With_Annotations()
        => await Succeed(
                """
                type Query {
                  entity(id: ID! @is(field: "id")): Entity!
                }

                type Entity {
                  id: ID!
                  field1: String!
                }
                """,
                """
                type Query {
                  entity(id: ID! @is(field: "id")): Entity!
                }

                type Entity {
                  id: ID!
                  field2: String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Entity
                  @variable(name: "Entity_id", select: "id", subgraph: "A")
                  @variable(name: "Entity_id", select: "id", subgraph: "B")
                  @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "A")
                  @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                  field2: String!
                    @source(subgraph: "B")
                  id: ID!
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }

                type Query {
                  entity(id: ID!): Entity!
                    @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "A")
                    @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
                }
                """);

    [Fact]
    public async Task Extract_Entity_Resolvers_With_Annotations_Batch()
        => await Succeed(
                """
                type Query {
                  entities(id: [ID!]! @is(field: "id")): [Entity!]
                }

                type Entity {
                  id: ID!
                  field1: String!
                }
                """,
                """
                type Query {
                  entity(id: ID! @is(field: "id")): Entity!
                }

                type Entity {
                  id: ID!
                  field2: String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Entity
                  @variable(name: "Entity_id", select: "id", subgraph: "A")
                  @variable(name: "Entity_id", select: "id", subgraph: "B")
                  @resolver(operation: "query($Entity_id: [ID!]!) { entities(id: $Entity_id) }", kind: BATCH, subgraph: "A")
                  @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                  field2: String!
                    @source(subgraph: "B")
                  id: ID!
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                
                type Query {
                  entities(id: [ID!]!): [Entity!]
                    @resolver(operation: "query($id: [ID!]!) { entities(id: $id) }", kind: FETCH, subgraph: "A")
                  entity(id: ID!): Entity!
                    @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
                }
                """);
    
    [Fact]
    public async Task Extract_Entity_Resolvers_With_Annotations_Composite_Id()
      => await Succeed(
          """
          type Query {
            entity2(id: ID! @is(field: "id") id2: String @is(field: "id2")): Entity!
          }

          type Entity {
            id: ID!
            field1: String!
          }
          """,
          """
          type Query {
            entity(id: ID! @is(field: "id")): Entity!
          }

          type Entity {
            id: ID!
            id2: String
            field2: String!
          }
          """)
        .MatchInlineSnapshotAsync(
          """
          type Entity
            @variable(name: "Entity_id", select: "id", subgraph: "A")
            @variable(name: "Entity_id", select: "id", subgraph: "B")
            @variable(name: "Entity_id2", select: "id2", subgraph: "B")
            @resolver(operation: "query($Entity_id: ID!, $Entity_id2: String) { entity2(id: $Entity_id, id2: $Entity_id2) }", kind: FETCH, subgraph: "A")
            @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
            field1: String!
              @source(subgraph: "A")
            field2: String!
              @source(subgraph: "B")
            id: ID!
              @source(subgraph: "A")
              @source(subgraph: "B")
            id2: String
              @source(subgraph: "B")
          }
          
          type Query {
            entity(id: ID!): Entity!
              @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
            entity2(id: ID! id2: String): Entity!
              @resolver(operation: "query($id: ID!, $id2: String) { entity2(id: $id, id2: $id2) }", kind: FETCH, subgraph: "A")
          }
          """);
    
    [Fact]
    public async Task Extract_Entity_Resolvers_With_Annotations_Composite_Id_Batch()
      => await Succeed(
          """
          type Query {
            entity2(id: [ID!]! @is(field: "id") id2: [String]! @is(field: "id2")): [Entity!]
          }

          type Entity {
            id: ID!
            field1: String!
          }
          """,
          """
          type Query {
            entity(id: ID! @is(field: "id")): Entity!
          }

          type Entity {
            id: ID!
            id2: String
            field2: String!
          }
          """)
        .MatchInlineSnapshotAsync(
          """
          type Entity
            @variable(name: "Entity_id", select: "id", subgraph: "A")
            @variable(name: "Entity_id", select: "id", subgraph: "B")
            @variable(name: "Entity_id2", select: "id2", subgraph: "B")
            @resolver(operation: "query($Entity_id: [ID!]!, $Entity_id2: [String]!) { entity2(id: $Entity_id, id2: $Entity_id2) }", kind: BATCH, subgraph: "A")
            @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
            field1: String!
              @source(subgraph: "A")
            field2: String!
              @source(subgraph: "B")
            id: ID!
              @source(subgraph: "A")
              @source(subgraph: "B")
            id2: String
              @source(subgraph: "B")
          }
          
          type Query {
            entity(id: ID!): Entity!
              @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
            entity2(id: [ID!]! id2: [String]!): [Entity!]
              @resolver(operation: "query($id: [ID!]!, $id2: [String]!) { entity2(id: $id, id2: $id2) }", kind: FETCH, subgraph: "A")
          }
          """);
    
    [Fact]
    public async Task Extract_Entity_Resolvers_With_Annotations_Composite_Id_Group()
      => await Succeed(
          """
          type Query {
            entity2(id: [ID!]! @is(field: "id") id2: String @is(field: "id2")): [Entity!]
          }

          type Entity {
            id: ID!
            field1: String!
          }
          """,
          """
          type Query {
            entity(id: ID! @is(field: "id")): Entity!
          }

          type Entity {
            id: ID!
            id2: String
            field2: String!
          }
          """)
        .MatchInlineSnapshotAsync(
          """
          type Entity
            @variable(name: "Entity_id", select: "id", subgraph: "A")
            @variable(name: "Entity_id", select: "id", subgraph: "B")
            @variable(name: "Entity_id2", select: "id2", subgraph: "B")
            @resolver(operation: "query($Entity_id: [ID!]!, $Entity_id2: String) { entity2(id: $Entity_id, id2: $Entity_id2) }", kind: BATCH, subgraph: "A")
            @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
            field1: String!
              @source(subgraph: "A")
            field2: String!
              @source(subgraph: "B")
            id: ID!
              @source(subgraph: "A")
              @source(subgraph: "B")
            id2: String
              @source(subgraph: "B")
          }
          
          type Query {
            entity(id: ID!): Entity!
              @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
            entity2(id: [ID!]! id2: String): [Entity!]
              @resolver(operation: "query($id: [ID!]!, $id2: String) { entity2(id: $id, id2: $id2) }", kind: FETCH, subgraph: "A")
          }
          """);
    
    [Fact]
    public async Task Extract_Entity_Resolvers_From_Private_Field()
      => await Succeed(
          """
          type Query {
            entity2(id: [ID!]! @is(field: "id") id2: String @is(field: "id2")): [Entity!] @private
          }

          type Entity {
            id: ID!
            field1: String!
          }
          """,
          """
          type Query {
            entity(id: ID! @is(field: "id")): Entity!
          }

          type Entity {
            id: ID!
            id2: String
            field2: String!
          }
          """)
        .MatchInlineSnapshotAsync(
          """
          type Entity
            @variable(name: "Entity_id", select: "id", subgraph: "A")
            @variable(name: "Entity_id", select: "id", subgraph: "B")
            @variable(name: "Entity_id2", select: "id2", subgraph: "B")
            @resolver(operation: "query($Entity_id: [ID!]!, $Entity_id2: String) { entity2(id: $Entity_id, id2: $Entity_id2) }", kind: BATCH, subgraph: "A")
            @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
            field1: String!
              @source(subgraph: "A")
            field2: String!
              @source(subgraph: "B")
            id: ID!
              @source(subgraph: "A")
              @source(subgraph: "B")
            id2: String
              @source(subgraph: "B")
          }

          type Query {
            entity(id: ID!): Entity!
              @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
          }
          """);
    
    [Fact]
    public async Task Extract_Entity_Resolver_But_Do_Not_Map_As_Root_Resolver()
      => await Succeed(
          """
          type Query {
            entity(id: ID! @is(field: "id")): Entity! @private
          }

          type Entity {
            id: ID!
            field1: String!
          }
          """,
          """
          type Query {
            entity(id: ID! @is(field: "id")): Entity!
          }

          type Entity {
            id: ID!
            field2: String!
          }
          """)
        .MatchInlineSnapshotAsync(
          """
          type Entity
            @variable(name: "Entity_id", select: "id", subgraph: "A")
            @variable(name: "Entity_id", select: "id", subgraph: "B")
            @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "A")
            @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
            field1: String!
              @source(subgraph: "A")
            field2: String!
              @source(subgraph: "B")
            id: ID!
              @source(subgraph: "A")
              @source(subgraph: "B")
          }

          type Query {
            entity(id: ID!): Entity!
              @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
          }
          """);
    
    // [Fact]
    public async Task Extract_Entity_Resolvers_With_Annotations_Abstract_Type()
        => await Succeed(
                """
                type Query {
                  entity(id: ID! @is(field: "id")): Abstract!
                }

                type Entity implements Abstract {
                  id: ID!
                  field1: String!
                }

                interface Abstract {
                  id: ID!
                }
                """,
                """
                type Query {
                  entity(id: ID! @is(field: "id")): Entity!
                }

                type Entity {
                  id: ID!
                  field2: String!
                }
                """)
            .MatchInlineSnapshotAsync(
                """
                type Entity
                  @variable(name: "Entity_id", select: "id", subgraph: "A")
                  @variable(name: "Entity_id", select: "id", subgraph: "B")
                  @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "A")
                  @resolver(operation: "query($Entity_id: ID!) { entity(id: $Entity_id) }", kind: FETCH, subgraph: "B") {
                  field1: String!
                    @source(subgraph: "A")
                  field2: String!
                    @source(subgraph: "B")
                  id: ID!
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }

                type Query {
                  entity(id: ID!): Entity!
                    @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "A")
                    @resolver(operation: "query($id: ID!) { entity(id: $id) }", kind: FETCH, subgraph: "B")
                }
                """);
}