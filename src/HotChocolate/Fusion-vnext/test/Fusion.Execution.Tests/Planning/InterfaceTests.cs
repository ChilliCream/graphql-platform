namespace HotChocolate.Fusion.Planning;

public class InterfaceTests : FusionTestBase
{
    # region interface { ... }

    [Fact]
    public void Interface_Field()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votable {
                viewerCanVote
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votable {
                  viewerCanVote
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votable {
                      viewerCanVote
                    }
                  }
            """);
    }

    [Fact]
    public void Interface_Field_Linked_Field_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorable {
                author {
                  id
                  displayName
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                authorable {
                  author {
                    id
                    displayName
                    id @fusion_internal
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    authorable {
                      author {
                        id
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.authorable
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_Field_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorable {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    id
                    displayName
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                authorable {
                  author {
                    id
                    displayName
                    id @fusion_internal
                  }
                  ... on Discussion {
                    author {
                      id
                      displayName
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    authorable {
                      author {
                        id
                      }
                      ... on Discussion {
                        author {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.authorable
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_3 {
                    authorById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: author.authorable
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_Field_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
              email: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorable {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    email
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                authorable {
                  author {
                    id
                    displayName
                    id @fusion_internal
                  }
                  ... on Discussion {
                    author {
                      email
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    authorable {
                      author {
                        id
                      }
                      ... on Discussion {
                        author {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      email
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.authorable
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_3 {
                    authorById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: author.authorable
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_Field_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votable {
                viewerCanVote
                ... on Discussion {
                  title
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    title
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votable {
                      viewerCanVote
                      ... on Discussion {
                        title
                      }
                    }
                  }
            """);
    }

    [Fact]
    public void Interface_Field_Concrete_Type_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votable: Votable
              discussionById(id: ID!): Discussion @lookup
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votable {
                viewerCanVote
                ... on Discussion {
                  viewerRating
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    viewerRating
                    id @fusion_internal
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votable {
                      viewerCanVote
                      ... on Discussion {
                        id
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    discussionById(id: $__fusion_1_id) {
                      viewerRating
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: <Discussion>.votable
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_Field_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              author: Author
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votable {
                viewerCanVote
                ... on Discussion {
                  author {
                    displayName
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    author {
                      displayName
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votable {
                      viewerCanVote
                      ... on Discussion {
                        author {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.votable
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    #endregion

    # region interfaces { ... }

    [Fact]
    public void Interface_List_Field()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votables {
                viewerCanVote
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votables {
                  viewerCanVote
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votables {
                      viewerCanVote
                    }
                  }
            """);
    }

    [Fact]
    public void Interface_List_Field_Linked_Field_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorables {
                author {
                  id
                  displayName
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                authorables {
                  author {
                    id
                    displayName
                    id @fusion_internal
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    authorables {
                      author {
                        id
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.authorables
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_List_Field_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorables {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    id
                    displayName
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                authorables {
                  author {
                    id
                    displayName
                    id @fusion_internal
                  }
                  ... on Discussion {
                    author {
                      id
                      displayName
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    authorables {
                      author {
                        id
                      }
                      ... on Discussion {
                        author {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.authorables
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_3 {
                    authorById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: author.authorables
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_List_Field_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
              email: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              authorables {
                author {
                  id
                  displayName
                }
                ... on Discussion {
                  author {
                    email
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                authorables {
                  author {
                    id
                    displayName
                    id @fusion_internal
                  }
                  ... on Discussion {
                    author {
                      email
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    authorables {
                      author {
                        id
                      }
                      ... on Discussion {
                        author {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      email
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.authorables
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_3 {
                    authorById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: author.authorables
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_List_Field_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votables {
                viewerCanVote
                ... on Discussion {
                  title
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votables {
                  viewerCanVote
                  ... on Discussion {
                    title
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votables {
                      viewerCanVote
                      ... on Discussion {
                        title
                      }
                    }
                  }
            """);
    }

    [Fact]
    public void Interface_List_Field_Concrete_Type_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votables: [Votable]
              discussionById(id: ID!): Discussion @lookup
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votables {
                viewerCanVote
                ... on Discussion {
                  viewerRating
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votables {
                  viewerCanVote
                  ... on Discussion {
                    viewerRating
                    id @fusion_internal
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votables {
                      viewerCanVote
                      ... on Discussion {
                        id
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    discussionById(id: $__fusion_1_id) {
                      viewerRating
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: <Discussion>.votables
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Interface_List_Field_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              author: Author
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              votables {
                viewerCanVote
                ... on Discussion {
                  author {
                    displayName
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                votables {
                  viewerCanVote
                  ... on Discussion {
                    author {
                      displayName
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    votables {
                      viewerCanVote
                      ... on Discussion {
                        author {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.votables
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    #endregion

    #region wrappers { interface { ... } }

    [Fact]
    public void List_Field_Interface_Object_Property_Linked_Field_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              wrappers {
                authorable {
                  author {
                    displayName
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                wrappers {
                  authorable {
                    author {
                      displayName
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    wrappers {
                      authorable {
                        author {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.authorable.wrappers
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              wrappers {
                authorable {
                  author {
                    displayName
                  }
                  ... on Discussion {
                    author {
                      displayName
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                wrappers {
                  authorable {
                    author {
                      displayName
                      id @fusion_internal
                    }
                    ... on Discussion {
                      author {
                        displayName
                        id @fusion_internal
                      }
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    wrappers {
                      authorable {
                        author {
                          id
                        }
                        ... on Discussion {
                          author {
                            id
                          }
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.authorable.wrappers
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_3 {
                    authorById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: author.authorable.wrappers
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
              email: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              wrappers {
                authorable {
                  author {
                    displayName
                  }
                  ... on Discussion {
                    author {
                      email
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                wrappers {
                  authorable {
                    author {
                      displayName
                      id @fusion_internal
                    }
                    ... on Discussion {
                      author {
                        email
                        id @fusion_internal
                      }
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    wrappers {
                      authorable {
                        author {
                          id
                        }
                        ... on Discussion {
                          author {
                            id
                          }
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      email
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.authorable.wrappers
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_3 {
                    authorById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: author.authorable.wrappers
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Concrete_Type()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              wrappers {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    title
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                wrappers {
                  votable {
                    viewerCanVote
                    ... on Discussion {
                      title
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    wrappers {
                      votable {
                        viewerCanVote
                        ... on Discussion {
                          title
                        }
                      }
                    }
                  }
            """);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Concrete_Type_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              wrappers: [Wrapper]
              discussionById(id: ID!): Discussion @lookup
            }

            type Wrapper {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              wrappers {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    viewerRating
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                wrappers {
                  votable {
                    viewerCanVote
                    ... on Discussion {
                      viewerRating
                      id @fusion_internal
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    wrappers {
                      votable {
                        viewerCanVote
                        ... on Discussion {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    discussionById(id: $__fusion_1_id) {
                      viewerRating
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: <Discussion>.votable.wrappers
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              wrappers: [Wrapper]
            }

            type Wrapper {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
              author: Author
            }

            type Comment implements Votable @key(fields: "id") {
              id: ID!
              viewerCanVote: Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = new TestSubgraph(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA, subgraphB);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              wrappers {
                votable {
                  viewerCanVote
                  ... on Discussion {
                    author {
                      displayName
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation: >-
              query testQuery {
                wrappers {
                  votable {
                    viewerCanVote
                    ... on Discussion {
                      author {
                        displayName
                        id @fusion_internal
                      }
                    }
                  }
                }
              }
            nodes:
              - id: 1
                schema: SUBGRAPH_1
                operation: >-
                  query testQuery_1 {
                    wrappers {
                      votable {
                        viewerCanVote
                        ... on Discussion {
                          author {
                            id
                          }
                        }
                      }
                    }
                  }
              - id: 2
                schema: SUBGRAPH_2
                operation: >-
                  query testQuery_2 {
                    authorById(id: $__fusion_1_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: author.<Discussion>.votable.wrappers
                    selectionMap: id
                dependencies:
                  - id: 1
            """);
    }

    #endregion
}
