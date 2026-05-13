using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Rewriters;

public class DocumentRewriterTests
{
    [Fact]
    public void Inline_Into_ProductById_SelectionSet()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Inline_Into_ProductById_SelectionSet_2_Levels()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product1
                }
            }

            fragment Product1 on Product {
                ... Product2
            }

            fragment Product2 on Product {
                id
                name
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Inline_Inline_Fragment_Into_ProductById_SelectionSet_1()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... {
                        id
                        name
                    }
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Inline_Into_ProductById_SelectionSet_3_Levels()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... on Product {
                        ... Product1
                    }
                }
            }

            fragment Product1 on Product {
                ... Product2
            }

            fragment Product2 on Product {
                id
                name
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Always_Included_Selections_Are_Inlined()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    id
                    ... @include(if: true) {
                        id
                        name
                    }
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Remove_Statically_Excluded_Fragment()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    id
                    ... @include(if: false) {
                        id
                        name
                    }
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition, true);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
              }
            }
            """);
    }

    [Fact]
    public void Deduplicate_Fields()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product
                    name
                }
            }

            fragment Product on Product {
                id
                name
                name
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);
    }

    [Fact]
    public void Remove_Statically_Included_Fragment_Spread()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product @skip(if: true)
                    name
                }
            }

            fragment Product on Product {
                id
                name
                name
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition, true);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """);
    }

    [Fact]
    public void Remove_Statically_Excluded_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    id
                    ... {
                        id
                        name @include(if: false)
                    }
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition, true);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                id
              }
            }
            """);
    }

    [Fact]
    public void Remove_Statically_Excluded_Field_2()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    id @include(if: false)
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition, true);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productById(id: 1) {
                __typename @fusion__empty
              }
            }
            """);
    }

    [Fact]
    public void Remove_Statically_Included_Skip_Included()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
                productById(id: 1) {
                    id @skip(if: $skip) @include(if: true)
                    name @include(if: false)
                    description @include(if: true)
                    description @skip(if: false)
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition, true);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productById(id: 1) {
                id @skip(if: $skip)
                description
              }
            }
            """);
    }

    [Fact]
    public void Leafs_With_Different_Directives_Do_Not_Merge()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
                productById(id: 1) {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name @include(if: $skip)
                name @include(if: $skip)
                name @skip(if: $skip)
                name @skip(if: $skip)
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productById(id: 1) {
                id
                name @include(if: $skip)
                name @skip(if: $skip)
              }
            }
            """);
    }

    [Fact]
    public void Composites_Without_Directives_Are_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                reviews {
                    nodes {
                        body
                    }
                }
            }

            fragment ProductFragment2 on Product {
                reviews {
                    pageInfo {
                        hasNextPage
                    }
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($slug: String!) {
              productBySlug(slug: $slug) {
                reviews {
                  nodes {
                    body
                  }
                  pageInfo {
                    hasNextPage
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Merge_Fields_With_Aliases()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                   ... {
                       a: name
                   }
                   a: name
                   name
                   ... {
                       name
                   }
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($slug: String!) {
              productBySlug(slug: $slug) {
                a: name
                name
              }
            }
            """);
    }

    [Fact]
    public void Merge_Fusion_Requirements()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
                productById(id: 1) {
                    id @fusion__requirement
                    id @fusion__requirement
                    id @fusion__requirement
                    id @fusion__requirement
                }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition, true);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productById(id: 1) {
                id @fusion__requirement
              }
            }
            """);
    }

    [Fact]
    public void Field_Without_SelectionSet_With_Conditional_Merged_With_Unconditional_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a")  {
                name @skip(if: $skip)
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
              }
            }
            """);
    }

    [Fact]
    public void Field_Without_SelectionSet_With_Conditional_Merged_With_Unconditional_Field_In_Different_Parent()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a")  {
                name
              }
              productBySlug(slug: "a")  {
                name @skip(if: $skip)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
              }
            }
            """);
    }

    [Fact]
    public void Field_With_SelectionSet_With_Conditional_Merged_With_Unconditional_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
              }
              productBySlug(slug: "a") {
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                description
              }
            }
            """);
    }

    [Fact]
    public void Field_With_SelectionSet_With_Conditional_Merged_With_Unconditional_Field_Reversed()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
              }
              productBySlug(slug: "a") @skip(if: $skip) {
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                description @skip(if: $skip)
              }
            }
            """);
    }

    [Fact]
    public void Nested_Field_With_Conditional_Merged_With_Unconditional_Field_In_Different_Parent()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  height
                }
              }
              productBySlug(slug: "a") {
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height @skip(if: $skip)
                  width
                }
              }
            }
            """);
    }

    [Fact]
    public void Nested_Field_With_Conditional_Merged_With_Unconditional_Field_In_Different_Parent_Reversed()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height
                }
              }
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height
                  width @skip(if: $skip)
                }
              }
            }
            """);
    }

    [Fact]
    public void Parent_Field_With_Conditional_Merged_With_Unconditional_Nested_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                dimension {
                  height
                }
              }
              productBySlug(slug: "a") {
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height @skip(if: $skip)
                  width
                }
              }
            }
            """);
    }

    [Fact]
    public void Parent_Field_With_Conditional_Merged_With_Unconditional_Nested_Field_Reversed()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height
                }
              }
              productBySlug(slug: "a") @skip(if: $skip) {
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height
                  width @skip(if: $skip)
                }
              }
            }
            """);
    }

    [Fact]
    public void Redundant_Conditional_Removed_When_Parent_Has_Same_Conditional()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name @skip(if: $skip)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
              }
            }
            """);
    }

    [Fact]
    public void Redundant_Conditional_Removed_When_Parent_Has_Same_Conditional_In_Different_Parent()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
              }
              productBySlug(slug: "a") {
                name @skip(if: $skip)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
              }
              productBySlug(slug: "a") {
                name @skip(if: $skip)
              }
            }
            """);
    }

    [Fact]
    public void Nested_Fields_With_Conditional_Wrapped_In_InlineFragment()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  height
                }
              }
              productBySlug(slug: "a") {
                dimension {
                  width @skip(if: $skip)
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  height
                }
                dimension {
                  width @skip(if: $skip)
                }
              }
            }
            """);
    }

    [Fact]
    public void Same_Conditionals_In_Different_Order_Are_Normalized()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a")  {
                name @include(if: $include) @skip(if: $skip)
              }
              productBySlug(slug: "a")  {
                name @skip(if: $skip) @include(if: $include)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip) @include(if: $include)
              }
            }
            """);
    }

    [Fact]
    public void Fragment_With_Conditional_Field_Merged_With_Unconditional_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
              }
              ...Fragment1
            }

            fragment Fragment1 on Query {
              productBySlug(slug: "a") {
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                description
              }
            }
            """);
    }

    [Fact]
    public void FragmentSpread_With_Conditional_Merged_With_Unconditional_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              ... Fragment1 @skip(if: $skip)
              productBySlug(slug: "a") {
                name
              }
            }

            fragment Fragment1 on Query {
              productBySlug(slug: "a") {
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                description @skip(if: $skip)
                name
              }
            }
            """);
    }

    [Fact]
    public void InlineFragment_With_Conditional_On_Same_Type_Merged_With_Unconditional()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  viewerCanVote
                  voteCount
                }
                ... on Product @skip(if: $skip) {
                  viewerCanVote
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  viewerCanVote
                  voteCount
                }
              }
            }
            """);
    }

    [Fact]
    public void InlineFragment_With_Conditional_On_Same_Type_With_Different_Fields()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                }
                ... on Product @skip(if: $skip) {
                  viewerCanVote
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                  viewerCanVote @skip(if: $skip)
                }
              }
            }
            """);
    }

    [Fact]
    public void InlineFragment_With_Conditional_Without_TypeCondition_Not_Merged()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                }
                ... @skip(if: $skip) {
                  viewerCanVote
                  voteCount
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                }
                ... @skip(if: $skip) {
                  viewerCanVote
                  voteCount
                }
              }
            }
            """);
    }

    [Fact]
    public void Nested_InlineFragment_With_Conditional_Merged_Into_Parent()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                  ... on Votable {
                    viewerCanVote
                    voteCount @skip(if: $skip)
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                  viewerCanVote
                }
              }
            }
            """);
    }

    [Fact]
    public void Nested_InlineFragment_With_Conditional_On_Parent_And_Child()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                  ... on Votable @skip(if: $skip) {
                    viewerCanVote
                    voteCount
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product {
                  voteCount
                  viewerCanVote @skip(if: $skip)
                }
              }
            }
            """);
    }

    [Fact]
    public void Multiple_Nested_InlineFragments_With_Different_Conditionals_Not_Merged()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  dimension @skip(if: $skip2) {
                    height
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  dimension @skip(if: $skip2) {
                    height
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Multiple_Nested_InlineFragments_With_Different_Conditionals_Merged_When_Field_Unconditional()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  ... @skip(if: $skip2) {
                    name
                    dimension {
                      height
                    }
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  dimension @skip(if: $skip2) {
                    height
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Same_Field_With_Different_Conditionals_Not_Merged()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip1)
                name @skip(if: $skip2)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip1)
                name @skip(if: $skip2)
              }
            }
            """);
    }

    [Fact]
    public void InlineFragments_On_Same_Type_With_Different_Conditionals_Not_Merged()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!,, $skip2: Boolean!) {
              votables {
                ... on Product @skip(if: $skip1) {
                  voteCount
                }
                ... on Product  @skip(if: $skip2) {
                  voteCount
                  viewerCanVote
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              votables {
                ... on Product @skip(if: $skip1) {
                  voteCount
                }
                ... on Product @skip(if: $skip2) {
                  voteCount
                  viewerCanVote
                }
              }
            }
            """);
    }

    // TODO: We could try to optimize this scenario in the future, by removing
    //       selections from type refinements, if they exist on the base type.
    [Fact]
    public void InlineFragments_With_Different_Conditionals_Merged_When_Field_Unconditional()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!,, $skip2: Boolean!) {
              votables {
                ... on Product @skip(if: $skip1) {
                  voteCount
                }
                ... on Product @skip(if: $skip2) {
                  voteCount
                  viewerCanVote
                }
                voteCount
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              votables {
                ... on Product @skip(if: $skip1) {
                  voteCount
                }
                ... on Product @skip(if: $skip2) {
                  voteCount
                  viewerCanVote
                }
                voteCount
              }
            }
            """);
    }

    [Fact]
    public void Field_With_Multiple_Conditionals_Merged_When_Parent_Has_Same_Conditional()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                dimension @include(if: $include) @skip(if: $skip) {
                  width
                  height
                }
                dimension @include(if: $include) {
                  primaryWidth: width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                dimension @include(if: $include) {
                  width
                  height
                  primaryWidth: width
                }
              }
            }
            """);
    }

    [Fact]
    public void Field_With_Multiple_Conditionals_Not_Merged_When_Different()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a") {
                dimension @include(if: $include) @skip(if: $skip) {
                  width
                  height
                }
                dimension @include(if: $include) {
                  primaryWidth: width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) @include(if: $include) {
                  width
                  height
                }
                dimension @include(if: $include) {
                  primaryWidth: width
                }
              }
            }
            """);
    }

    [Fact]
    public void Multiple_Nested_InlineFragments_With_Same_Conditional_Merged()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
              depth: Int!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip) {
                  dimension {
                    ... @skip(if: $skip) {
                      width
                      ... @skip(if: $skip) {
                        height
                        depth
                      }
                    }
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                dimension @skip(if: $skip) {
                  width
                  height
                  depth
                }
              }
            }
            """);
    }

    [Fact]
    public void InlineFragment_With_Include_And_Skip_Merged_With_Parent_Skip()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
                ... @include(if: $include) @skip(if: $skip) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
                description @include(if: $include)
              }
            }
            """);
    }

    [Fact]
    public void Three_Level_Nested_InlineFragments_With_Mixed_Conditionals()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
              weight: Weight
            }

            type Weight {
              value: Float!
              unit: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!, $skip2: Boolean!, $skip3: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  dimension {
                    width
                    ... @skip(if: $skip2) {
                      height
                      weight {
                        ... @skip(if: $skip3) {
                          value
                          unit
                        }
                      }
                    }
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!, $skip3: Boolean!) {
              productBySlug(slug: "a") {
                name
                dimension @skip(if: $skip1) {
                  width
                  ... @skip(if: $skip2) {
                    height
                    weight {
                      ... @skip(if: $skip3) {
                        value
                        unit
                      }
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void InlineFragments_At_Multiple_Levels_With_Same_Conditional_Merged()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  name
                }
                dimension {
                  ... @skip(if: $skip) {
                    width
                    height
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                dimension {
                  ... @skip(if: $skip) {
                    width
                    height
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Sibling_InlineFragments_With_Same_Conditional_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip) {
                  description
                }
                ... @skip(if: $skip) {
                  dimension {
                    width
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip) {
                  description
                  dimension {
                    width
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Field_With_Conditional_And_Alias_Merged_With_Same_Field_Different_Alias()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                primaryName: name @skip(if: $skip)
                secondaryName: name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                primaryName: name @skip(if: $skip)
                secondaryName: name
              }
            }
            """);
    }

    [Fact]
    public void Multiple_Fields_With_Same_Conditional_In_InlineFragment()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                description @skip(if: $skip)
                ... {
                  name
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  name
                  description
                }
                name
                description
              }
            }
            """);
    }

    [Fact]
    public void InlineFragment_With_Conditional_Merged_Across_Multiple_Siblings()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
              }
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  description
                }
              }
              productBySlug(slug: "a") {
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                description @skip(if: $skip)
                dimension {
                  width
                }
              }
            }
            """);
    }

    [Fact]
    public void InlineFragment_And_Field_With_Include_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                name @include(if: $include)
                ... @include(if: $include) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                ... @include(if: $include) {
                  name
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Complex_Nested_Structure_With_Multiple_Conditional_Levels()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
              tags: [String!]!
            }

            type Dimension {
              width: Int!
              height: Int!
              depth: Int!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  tags
                  dimension {
                    width
                    ... @skip(if: $skip2) {
                      height
                    }
                  }
                }
                dimension {
                  depth
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  tags
                  dimension {
                    width
                    height @skip(if: $skip2)
                  }
                }
                dimension {
                  depth
                }
              }
            }
            """);
    }

    [Fact]
    public void InlineFragment_With_TypeCondition_And_Without_Both_Conditional()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              votables {
                ... on Product @skip(if: $skip) {
                  voteCount
                }
                ... @skip(if: $skip) {
                  viewerCanVote
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              votables {
                ... @skip(if: $skip) {
                  ... on Product {
                    voteCount
                  }
                  viewerCanVote
                }
              }
            }
            """);
    }

    [Fact]
    public void FragmentSpread_And_InlineFragment_With_Same_Conditional()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                ...ProductFields @skip(if: $skip)
                ... @skip(if: $skip) {
                  dimension {
                    width
                  }
                }
              }
            }

            fragment ProductFields on Product {
              description
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip) {
                  description
                  dimension {
                    width
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Field_Arguments_With_Different_Values_And_Conditionals_Not_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              product1: productBySlug(slug: "a") {
                name
              }
              product2: productBySlug(slug: "b") @skip(if: $skip) {
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              product1: productBySlug(slug: "a") {
                name
              }
              product2: productBySlug(slug: "b") @skip(if: $skip) {
                name
              }
            }
            """);
    }

    [Fact]
    public void Deeply_Nested_InlineFragments_With_Alternating_Conditionals()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
              weight: Weight
            }

            type Weight {
              value: Float!
              unit: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip1) {
                  dimension {
                    width
                    ... @skip(if: $skip2) {
                      height
                      ... @skip(if: $skip1) {
                        weight {
                          value
                          ... @skip(if: $skip2) {
                            unit
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "a") {
                name
                dimension @skip(if: $skip1) {
                  width
                  ... @skip(if: $skip2) {
                    height
                    weight {
                      value
                      unit
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Remove_Opposite_Child_Conditional_With_Same_Value_Skip_Include()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($conditional: Boolean!) {
              productBySlug(slug: "a") @skip(if: $conditional) {
                name
                dimension @include(if: $conditional) {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($conditional: Boolean!) {
              productBySlug(slug: "a") @skip(if: $conditional) {
                name
              }
            }
            """);
    }

    [Fact]
    public void Remove_Opposite_Child_Conditional_With_Same_Value_Include_Skip()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              dimension: Dimension
            }

            type Dimension {
              width: Int!
              height: Int!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($conditional: Boolean!) {
              productBySlug(slug: "a") @include(if: $conditional) {
                name
                dimension @skip(if: $conditional) {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($conditional: Boolean!) {
              productBySlug(slug: "a") @include(if: $conditional) {
                name
              }
            }
            """);
    }

    #region @defer

    [Fact]
    public void Defer_With_Same_Leaf_Field_Outside_Is_Dropped()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Composite_Field_Merges_Sub_Selections_Under_Defer()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                dimension {
                  width
                }
                ... @defer {
                  dimension {
                    width
                    height
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                dimension {
                  width
                  ... @defer {
                    height
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Skip_Conditional_Same_Leaf_Field_Is_Not_Rewritten()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                ... @defer {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                ... @defer {
                  name
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Skip_Conditional_Same_Composite_Field_Is_Not_Rewritten()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  width
                }
                ... @defer {
                  dimension {
                    width
                    height
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  width
                }
                ... @defer {
                  dimension {
                    width
                    height
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Include_Conditional_Same_Leaf_Field_Is_Not_Rewritten()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                name @include(if: $include)
                ... @defer {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                name @include(if: $include)
                ... @defer {
                  name
                }
              }
            }
            """);
    }

    [Fact]
    public void Sibling_Defers_Without_Arguments_Are_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                ... @defer {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Sibling_Defers_With_Different_Labels_Are_Not_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer(label: "a") {
                  name
                }
                ... @defer(label: "b") {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer(label: "a") {
                  name
                }
                ... @defer(label: "b") {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Sibling_Defers_With_Same_Label_Are_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer(label: "a") {
                  name
                }
                ... @defer(label: "a") {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer(label: "a") {
                  name
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Sibling_Defers_With_Different_If_Argument_Are_Not_Merged()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($a: Boolean!, $b: Boolean!) {
              productBySlug(slug: "a") {
                ... @defer(if: $a) {
                  name
                }
                ... @defer(if: $b) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($a: Boolean!, $b: Boolean!) {
              productBySlug(slug: "a") {
                ... @defer(if: $a) {
                  name
                }
                ... @defer(if: $b) {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_Only_Selection_Is_Kept_When_No_Sibling_Selects_The_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_False_Inlines_Fragment_Selections()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer(if: false) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                description
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_True_Is_Normalized_To_Defer_Without_Arguments()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer(if: true) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_True_Is_Merged_With_Other_Defer_If_True()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer(if: true) {
                  name
                }
                ... @defer(if: true) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_True_Is_Merged_With_Defer_Without_If_Argument()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                ... @defer(if: true) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_True_With_Label_Is_Merged_With_Defer_With_Same_Label()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer(label: "a") {
                  name
                }
                ... @defer(label: "a", if: true) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer(label: "a") {
                  name
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_On_FragmentSpread_With_Same_Field_Outside_Is_Dropped()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ...ProductFields @defer
              }
            }

            fragment ProductFields on Product {
              name
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
              }
            }
            """);
    }

    [Fact]
    public void Defer_On_FragmentSpread_With_Different_Fields_Is_Kept()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ...ProductFields @defer
              }
            }

            fragment ProductFields on Product {
              description
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Sub_Selections_Partially_Subsumed_By_Unconditional()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                description
                ... @defer {
                  name
                  dimension {
                    width
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                description
                ... @defer {
                  dimension {
                    width
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Nested_Defer_With_Same_Arguments_Is_Flattened()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  ... @defer {
                    description
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Skip_On_Same_Fragment_Are_Kept_Together()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip) @defer {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @skip(if: $skip) @defer {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_On_TypeRefining_Fragment_Same_Field_Is_Dropped()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              votables {
                ... on Product {
                  voteCount
                }
                ... on Product @defer {
                  voteCount
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              votables {
                ... on Product {
                  voteCount
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_On_TypeRefining_Fragment_With_Additional_Fields_Is_Kept()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              votables {
                ... on Product {
                  voteCount
                }
                ... on Product @defer {
                  viewerCanVote
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              votables {
                ... on Product {
                  voteCount
                  ... @defer {
                    viewerCanVote
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_False_On_FragmentSpread_Inlines_Fragment_Selections()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ...ProductFields @defer(if: false)
              }
            }

            fragment ProductFields on Product {
              description
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                description
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_False_On_TypeRefining_Fragment_Keeps_Type_Refinement()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              votables: [Votable!]!
            }

            interface Votable {
              viewerCanVote: Boolean!
              voteCount: Int
            }

            type Product implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              voteCount: Int
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              votables {
                voteCount
                ... on Product @defer(if: false) {
                  viewerCanVote
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              votables {
                voteCount
                ... on Product {
                  viewerCanVote
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_False_With_Label_Still_Removes_Defer()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer(label: "a", if: false) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                description
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_Variable_Is_Preserved()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($defer: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @defer(if: $defer) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($defer: Boolean!) {
              productBySlug(slug: "a") {
                name
                ... @defer(if: $defer) {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_If_False_Sibling_Merged_Into_Sibling_Defer_Group()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                ... @defer(if: false) {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                description
              }
            }
            """);
    }

    [Fact]
    public void Unconditional_Leaf_With_Same_Deferred_Leaf_Drops_Defer()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                ... @defer {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Skip_Conditional_Different_Field_Is_Kept()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                ... @defer {
                  description
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                ... @defer {
                  description
                }
              }
            }
            """);
    }

    [Fact]
    public void Unconditional_Leaf_Subsumes_Both_Skip_Conditional_And_Deferred_Selections_Of_Same_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                name @skip(if: $skip)
                ... @defer {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Include_Conditional_Same_Composite_Field_Is_Not_Rewritten()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                dimension @include(if: $include) {
                  width
                }
                ... @defer {
                  dimension {
                    width
                    height
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                dimension @include(if: $include) {
                  width
                }
                ... @defer {
                  dimension {
                    width
                    height
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_Inside_Same_Skip_Block_Is_Subsumed_By_Unconditional_Selection_In_That_Block()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  name
                  ... @defer {
                    name
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
              }
            }
            """);
    }

    [Fact]
    public void Defer_With_Single_Field_Wraps_In_Inline_Fragment()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
              }
            }
            """);
    }

    #endregion

    #region Spec ordering

    [Fact]
    public void Field_Then_TypeRefinement_Then_Field_Preserves_Order()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            interface Post {
              title: String!
              commentCount: Int!
            }

            type Article implements Post {
              title: String!
              commentCount: Int!
              body: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              post {
                title
                ... on Article { body }
                commentCount
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              post {
                title
                ... on Article {
                  body
                }
                commentCount
              }
            }
            """);
    }

    [Fact]
    public void TypeRefinement_Before_Field_Preserves_Order()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            interface Post {
              title: String!
            }

            type Article implements Post {
              title: String!
              body: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              post {
                ... on Article { body }
                title
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              post {
                ... on Article {
                  body
                }
                title
              }
            }
            """);
    }

    [Fact]
    public void Field_Between_Two_TypeRefinements_Preserves_Order()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            interface Post {
              title: String!
            }

            type Article implements Post {
              title: String!
              body: String!
            }

            type Photo implements Post {
              title: String!
              pictureUrl: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              post {
                ... on Article { body }
                title
                ... on Photo { pictureUrl }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              post {
                ... on Article {
                  body
                }
                title
                ... on Photo {
                  pictureUrl
                }
              }
            }
            """);
    }

    [Fact]
    public void Skip_On_Field_Before_Unconditional_Same_Field_Keeps_Both()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                description
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                description
                name
              }
            }
            """);
    }

    [Fact]
    public void Include_On_Field_Before_Unconditional_Same_Field_Keeps_Both()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                name @include(if: $include)
                description
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($include: Boolean!) {
              productBySlug(slug: "a") {
                name @include(if: $include)
                description
                name
              }
            }
            """);
    }

    [Fact]
    public void Defer_Fragment_Before_Unconditional_Same_Field_Keeps_Both()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                description
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                description
                name
              }
            }
            """);
    }

    [Fact]
    public void Skip_On_Composite_Before_Unconditional_Same_Field_Keeps_Both()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  height
                }
                name
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  height
                }
                name
                dimension {
                  width
                }
              }
            }
            """);
    }

    [Fact]
    public void Unconditional_Field_Before_Skipped_Same_Field_Drops_Skipped()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
                name @skip(if: $skip)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name
              }
            }
            """);
    }

    [Fact]
    public void Two_Same_Skip_Blocks_Are_Merged_Into_First_Position()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  name
                }
                description
                ... @skip(if: $skip) {
                  price
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  name
                  price
                }
                description
              }
            }
            """);
    }

    [Fact]
    public void Aliases_Are_Distinct_Slots()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                foo: name
                description
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                foo: name
                description
                name
              }
            }
            """);
    }

    [Fact]
    public void Static_Skip_Shifts_Slots_Left()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                name
                description @skip(if: true)
                price
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition, removeStaticallyExcludedSelections: true);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                name
                price
              }
            }
            """);
    }

    [Fact]
    public void Multiple_TypeRefinements_With_Interleaved_Fields_Preserves_Order()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            interface Post {
              title: String!
              commentCount: Int!
            }

            interface AltTextHaver {
              altText: String!
            }

            type Video implements Post {
              title: String!
              commentCount: Int!
              videoUrl: String!
            }

            type Photo implements Post & AltTextHaver {
              title: String!
              commentCount: Int!
              altText: String!
              pictureUrl: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              post {
                title
                ... on Video { videoUrl }
                ... on AltTextHaver { altText }
                ... on Photo { pictureUrl }
                commentCount
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              post {
                title
                ... on Video {
                  videoUrl
                }
                ... on AltTextHaver {
                  altText
                }
                ... on Photo {
                  pictureUrl
                }
                commentCount
              }
            }
            """);
    }

    [Fact]
    public void Skip_Before_Defer_Before_Unconditional_Same_Field_Keeps_All_Three()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                ... @defer {
                  name
                }
                description
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                ... @defer {
                  name
                }
                description
                name
              }
            }
            """);
    }

    [Fact]
    public void Defer_Before_Skip_Before_Unconditional_Same_Field_Keeps_All_Three()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                name @skip(if: $skip)
                description
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                name @skip(if: $skip)
                description
                name
              }
            }
            """);
    }

    #endregion

    #region Conditional composite sub-pruning

    [Fact]
    public void Conditional_Composite_Sub_Field_Owned_By_Unconditional_Is_Pruned()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            type Post {
              author: Author
            }

            type Author {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              post {
                ... @skip(if: $skip) {
                  author {
                    id
                    name
                  }
                }
                author {
                  id
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              post {
                author {
                  ... @skip(if: $skip) {
                    id
                    name
                  }
                  id
                }
              }
            }
            """);
    }

    [Fact]
    public void Conditional_Composite_Sub_Field_Unique_To_Conditional_Survives()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            type Post {
              author: Author
            }

            type Author {
              id: ID!
              name: String!
              email: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              post {
                ... @skip(if: $skip) {
                  author {
                    id
                    email
                  }
                }
                author {
                  id
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              post {
                author {
                  ... @skip(if: $skip) {
                    id
                    email
                  }
                  id
                  name
                }
              }
            }
            """);
    }

    [Fact]
    public void Conditional_Composite_Sub_Field_Pruned_Across_Multiple_Levels()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            type Post {
              author: Author
            }

            type Author {
              profile: Profile
            }

            type Profile {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              post {
                ... @skip(if: $skip) {
                  author {
                    profile {
                      id
                      name
                    }
                  }
                }
                author {
                  profile {
                    id
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              post {
                author {
                  profile {
                    ... @skip(if: $skip) {
                      id
                      name
                    }
                    id
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Conditional_Composite_Fully_Covered_By_Unconditional_Drops_Conditional_Slot()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            type Post {
              author: Author
            }

            type Author {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              post {
                ... @skip(if: $skip) {
                  author {
                    id
                  }
                }
                author {
                  id
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              post {
                author {
                  id
                  name
                }
              }
            }
            """);
    }

    [Fact]
    public void Defer_Composite_Sub_Field_Owned_By_Unconditional_Is_Pruned()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            type Post {
              author: Author
            }

            type Author {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              post {
                ... @defer {
                  author {
                    id
                    name
                  }
                }
                author {
                  id
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              post {
                ... @defer {
                  author {
                    id
                    name
                  }
                }
                author {
                  id
                }
              }
            }
            """);
    }

    [Fact]
    public void Type_Refining_Conditional_Composite_Sub_Field_Owned_By_Unconditional_Is_Pruned()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              node: Node
            }

            interface Node {
              id: ID!
            }

            type Article implements Node {
              id: ID!
              author: Author
            }

            type Author {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              node {
                ... on Article @skip(if: $skip) {
                  author {
                    id
                    name
                  }
                }
                ... on Article {
                  author {
                    id
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              node {
                ... on Article {
                  author {
                    ... @skip(if: $skip) {
                      id
                      name
                    }
                    id
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Conditional_Composite_Sub_Field_Pruning_Does_Not_Drop_Direct_Leaf()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  name
                  dimension {
                    width
                    height
                  }
                }
                name
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) {
                  name
                  dimension {
                    width
                    height
                  }
                }
                name
                dimension {
                  width
                }
              }
            }
            """);
    }

    [Fact]
    public void Partial_Pruning_Of_Conditional_Subfields_Does_Not_Shift_Order()
    {
        // arrange
        // GraphQL spec, Section 6 (Execution): the depth-first first-occurrence order of
        // each field set produced by CollectFields is preserved through execution. Removing
        // a sub-field from a conditional whose unconditional sibling sits later would shift
        // the surviving sub-fields' positions in the merged response, which violates that
        // order. The conservative prune drops the conditional only when it is wholesale
        // covered; partial overlap leaves the conditional intact.
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            type Post {
              author: Author
            }

            type Author {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              post {
                ... @skip(if: $skip) {
                  author {
                    id
                    name
                  }
                }
                author {
                  id
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        // skip=false runtime: author@0; inside author the depth-first first occurrence
        // gives id@0 (from the conditional), name@1, then second author merges (id already).
        // skip=true runtime: only the unconditional sibling contributes, giving author{id}.
        // The folded output below preserves both orders: with skip=false the @skip block
        // delivers id, name in that order, then the trailing unconditional `id` is already
        // at slot 0; with skip=true the inline fragment is dropped, leaving `id` at slot 0.
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              post {
                author {
                  ... @skip(if: $skip) {
                    id
                    name
                  }
                  id
                }
              }
            }
            """);
    }

    #endregion

    #region Adjacent same-key fold

    [Fact]
    public void Conditional_Adjacent_To_Unconditional_Folds()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                name
              }
              productBySlug(slug: "a") {
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                name @skip(if: $skip)
                description
              }
            }
            """);
    }

    [Fact]
    public void Conditional_With_Other_Slot_Between_Unconditional_Does_NOT_Fold()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
              extra: String
            }

            type Post {
              title: String
              body: String
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              post @skip(if: $skip) {
                title
              }
              extra
              post {
                body
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              post @skip(if: $skip) {
                title
              }
              extra
              post {
                body
              }
            }
            """);
    }

    [Fact]
    public void Conditional_With_Multiple_Slots_Does_NOT_Fold()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              ... @skip(if: $skip) {
                productBySlug(slug: "a") {
                  name
                }
                viewer {
                  displayName
                }
              }
              productBySlug(slug: "a") {
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              ... @skip(if: $skip) {
                productBySlug(slug: "a") {
                  name
                }
                viewer {
                  displayName
                }
              }
              productBySlug(slug: "a") {
                description
              }
            }
            """);
    }

    [Fact]
    public void Defer_Adjacent_To_Unconditional_Does_NOT_Fold_Via_New_Rule()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            {
              productBySlug(slug: "a") {
                ... @defer {
                  name
                }
                description
              }
            }
            """);
    }

    [Fact]
    public void Skip_And_Defer_Combined_Adjacent_To_Unconditional_Does_NOT_Fold()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) @defer {
                  name
                }
                description
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                ... @skip(if: $skip) @defer {
                  name
                }
                description
              }
            }
            """);
    }

    [Fact]
    public void Three_Level_Adjacent_Fold_Cascades()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension @skip(if: $skip) {
                  height
                }
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height @skip(if: $skip)
                  width
                }
              }
            }
            """);
    }

    [Fact]
    public void Type_Refinement_Conditional_Adjacent_To_Type_Refinement_Unconditional_Folds()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              node: Node
            }

            interface Node {
              id: ID!
            }

            type Article implements Node {
              id: ID!
              body: String!
              title: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              node {
                ... on Article @skip(if: $skip) {
                  body
                }
                ... on Article {
                  title
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              node {
                ... on Article {
                  body @skip(if: $skip)
                  title
                }
              }
            }
            """);
    }

    [Fact]
    public void Type_Refinement_Conditional_With_Other_Slot_Between_Does_NOT_Fold()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              node: Node
            }

            interface Node {
              id: ID!
            }

            type Article implements Node {
              id: ID!
              body: String!
              title: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              node {
                ... on Article @skip(if: $skip) {
                  body
                }
                id
                ... on Article {
                  title
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              node {
                ... on Article @skip(if: $skip) {
                  body
                }
                id
                ... on Article {
                  title
                }
              }
            }
            """);
    }

    [Fact]
    public void Parent_Conditional_Composite_With_Inner_Overlap_Folds_Cascading()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") @skip(if: $skip) {
                dimension {
                  height
                }
              }
              productBySlug(slug: "a") {
                dimension {
                  width
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              productBySlug(slug: "a") {
                dimension {
                  height @skip(if: $skip)
                  width
                }
              }
            }
            """);
    }

    [Fact]
    public void Parent_Conditional_Composite_With_Two_Level_Inner_Overlap_Folds_Cascading()
    {
        // arrange
        var schemaDefinition = SchemaParser.Parse(
            """
            type Query {
              post: Post
            }

            type Post {
              author: Author
            }

            type Author {
              id: ID!
              name: String!
            }
            """);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!) {
              ... @skip(if: $skip) {
                post {
                  author {
                    name
                  }
                }
              }
              post {
                author {
                  id
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

        // assert
        rewritten.Document.MatchInlineSnapshot(
            """
            query($skip: Boolean!) {
              post {
                author {
                  name @skip(if: $skip)
                  id
                }
              }
            }
            """);
    }

    #endregion

    #region HasIncrementalParts

    [Fact]
    public void HasIncrementalParts_Is_False_Without_Directives()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.False(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_True_For_Defer_On_Inline_Fragment()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productById(id: 1) {
                id
                ... @defer {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.True(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_True_For_Defer_On_Fragment_Spread()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productById(id: 1) {
                id
                ... Product @defer
              }
            }

            fragment Product on Product {
              name
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.True(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_False_For_Defer_If_False()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productById(id: 1) {
                id
                ... @defer(if: false) {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.False(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_True_For_Defer_If_True()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productById(id: 1) {
                id
                ... @defer(if: true) {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.True(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_True_For_Defer_If_Variable()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($defer: Boolean!) {
              productById(id: 1) {
                id
                ... @defer(if: $defer) {
                  name
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.True(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_True_For_Stream_On_Field()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productById(id: 1) {
                id
                reviews @stream {
                  nodes {
                    body
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.True(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_True_For_Defer_And_Stream_Together()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
              productById(id: 1) {
                id
                ... @defer {
                  name
                }
                reviews @stream {
                  nodes {
                    body
                  }
                }
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.True(result.HasIncrementalParts);
    }

    [Fact]
    public void HasIncrementalParts_Is_False_For_Skip_And_Include_Only()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($skip: Boolean!, $include: Boolean!) {
              productById(id: 1) {
                id
                name @skip(if: $skip)
                description @include(if: $include)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var result = rewriter.RewriteDocument(doc);

        // assert
        Assert.False(result.HasIncrementalParts);
    }

    #endregion
}
