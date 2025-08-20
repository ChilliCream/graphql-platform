using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class SelectionSetByTypePartitionerTests : FusionTestBase
{
    [Fact]
    public void Only_Selections_On_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Discussion {
                      title
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: null

            Discussion: {
              title
            }
            """);
    }

    [Fact]
    public void Only_Shared_Selections()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    id
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: {
              id
            }
            """);
    }

    [Fact]
    public void Selections_On_Shared_And_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    __typename
                    id
                    ... on Discussion {
                        title
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: {
              __typename
              id
            }

            Discussion: {
              __typename
              id
              title
            }
            """);
    }

    [Fact]
    public void Selections_On_Shared_And_Concrete_Type_With_Conditions()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!, $skip: Boolean!) {
                node(id: $id) {
                    ... on Node @skip(if: $skip) {
                        id
                    }
                    ... on Discussion {
                        title
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: {
              ... @skip(if: $skip) {
                id
              }
            }

            Discussion: {
              ... @skip(if: $skip) {
                id
              }
              title
            }
            """);
    }

    [Fact]
    public void Selections_On_Interface()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            interface Votable {
              viewerHasUpvoted: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              title: String!
              viewerHasUpvoted: Boolean!
            }

            type Author implements Node & Votable {
              id: ID!
              name: String!
              viewerHasUpvoted: Boolean!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Votable {
                        viewerHasUpvoted
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: null

            Author: {
              viewerHasUpvoted
            }
            Discussion: {
              viewerHasUpvoted
            }
            """);
    }

    [Fact]
    public void Concrete_Type_Selections_Within_Interface()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            interface Votable {
              viewerHasUpvoted: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              title: String!
              viewerHasUpvoted: Boolean!
            }

            type Author implements Node & Votable {
              id: ID!
              name: String!
              viewerHasUpvoted: Boolean!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Votable {
                        viewerHasUpvoted
                        ... on Discussion {
                            title
                        }
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: null

            Author: {
              viewerHasUpvoted
            }
            Discussion: {
              viewerHasUpvoted
              title
            }
            """);
    }

    [Fact]
    public void Interface_Selections_Within_Concrete_Type()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            interface Votable {
              viewerHasUpvoted: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              title: String!
              viewerHasUpvoted: Boolean!
            }

            type Author implements Node & Votable {
              id: ID!
              name: String!
              viewerHasUpvoted: Boolean!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Author {
                        ... on Votable {
                            viewerHasUpvoted
                        }
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: null

            Author: {
              viewerHasUpvoted
            }
            """);
    }

        [Fact]
    public void Spread_On_Type_Of_SelectionSet_Is_Part_Of_Shared_Selections()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... {
                      id
                    }
                    ... on Discussion  {
                        title
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: {
              id
            }

            Discussion: {
              id
              title
            }
            """);
    }

    [Fact]
    public void Spread_With_TypeCondition_On_Type_Of_SelectionSet_Is_Part_Of_Shared_Selections()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Node {
                      id
                    }
                    ... on Discussion  {
                        title
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: {
              id
            }

            Discussion: {
              id
              title
            }
            """);
    }

    [Fact]
    public void Conditional_Concrete_Type_Selections()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Discussion @skip(if: $skip) {
                        title
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: null

            Discussion: {
              ... @skip(if: $skip) {
                title
              }
            }
            """);
    }

    [Fact]
    public void Conditional_Shared_Selections()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Node @skip(if: $skip) {
                      id
                    }
                    ... on Discussion  {
                        title
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: {
              ... @skip(if: $skip) {
                id
              }
            }

            Discussion: {
              ... @skip(if: $skip) {
                id
              }
              title
            }
            """);
    }

    [Fact]
    public void Conditional_SelectionSet_Root()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Node @skip(if: $skip) {
                        id
                        ... on Discussion  {
                            title
                        }
                    }
                }
            }
            """);

        // act
        var result = Partition(schema, doc);

        // assert
        MatchInlineSnapshot(
            result,
            """
            Shared: {
              ... @skip(if: $skip) {
                id
              }
            }

            Discussion: {
              ... @skip(if: $skip) {
                id
              }
              ... @skip(if: $skip) {
                title
              }
            }
            """);
    }

    private static SelectionSetByTypePartitionerResult Partition(FusionSchemaDefinition schema, DocumentNode document)
    {
        var fragmentRewriter = new InlineFragmentOperationRewriter(schema);
        var operation = fragmentRewriter.RewriteDocument(document).Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var index = SelectionSetIndexer.Create(operation);

        var nodeField = operation.SelectionSet.Selections
            .OfType<FieldNode>()
            .Single();

        var input = new SelectionSetByTypePartitionerInput
        {
            SelectionSet = new SelectionSet(
                index.GetId(nodeField.SelectionSet!),
                nodeField.SelectionSet!,
                schema.Types["Node"],
                SelectionPath.Root),
            SelectionSetIndex = index
        };
        var partitioner = new SelectionSetByTypePartitioner(schema);

        return partitioner.Partition(input);
    }

    private static void MatchInlineSnapshot(
        SelectionSetByTypePartitionerResult result,
        string expected)
    {
        var sb = new StringBuilder();

        sb.Append("Shared: ");
        sb.Append(result.SharedSelectionSet?.ToString(true) ?? "null");

        for (var i = 0; i < result.SelectionSetsByType.Length; i++)
        {
            if (i == 0)
            {
                sb.AppendLine();
                sb.AppendLine();
            }

            var (type, selectionSet) = result.SelectionSetsByType[i];

            sb.Append(type.Name);
            sb.Append(": ");
            sb.Append(selectionSet.ToString(true));

            if (i < result.SelectionSetsByType.Length - 1)
            {
                sb.AppendLine();
            }
        }

        sb.ToString().MatchInlineSnapshot(expected);
    }
}
