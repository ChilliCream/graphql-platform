namespace HotChocolate.Fusion.Planning;

public class InterfaceTests : FusionTestBase
{
    # region interface { ... }

    [Fact]
    public void Interface_Field()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_Field_Linked_Field_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_Field_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_Field_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_Field_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_Field_Concrete_Type_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_Field_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    #endregion

    # region interfaces { ... }

    [Fact]
    public void Interface_List_Field()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_List_Field_Linked_Field_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_List_Field_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_List_Field_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_List_Field_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_List_Field_Concrete_Type_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void Interface_List_Field_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    #endregion

    #region wrappers { interface { ... } }

    [Fact]
    public void List_Field_Interface_Object_Property_Linked_Field_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Linked_Field_With_Dependency_Same_Selection_In_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Linked_Field_With_Dependency_Different_Selection_In_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var schema = ComposeSchema(source1);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Concrete_Type_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              discussionById(id: ID!): Discussion @lookup
            }

            type Discussion @key(fields: "id") {
              id: ID!
              viewerRating: Float!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    [Fact]
    public void List_Field_Interface_Object_Property_Concrete_Type_Linked_Field_With_Dependency()
    {
        // arrange
        var source1 = new TestSourceSchema(
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

        var source2 = new TestSourceSchema(
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
              id: ID!
              displayName: String!
            }
            """);

        var schema = ComposeSchema(source1, source2);

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
        MatchSnapshot(plan);
    }

    #endregion
}
