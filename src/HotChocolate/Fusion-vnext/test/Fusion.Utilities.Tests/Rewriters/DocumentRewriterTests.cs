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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productById(id: 1) {
                description
                id @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $slug: String!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $slug: String!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                description
                name @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                dimension {
                  width
                  height @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                dimension {
                  width
                  height @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                dimension {
                  ... @skip(if: $skip) {
                    height
                    width
                  }
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
              $include: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                description
                name @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                name
                description @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
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
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
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
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
              productBySlug(slug: "a") {
                name @skip(if: $skip1)
                name @skip(if: $skip2)
              }
            }
            """);

        // act
        var rewriter = new DocumentRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
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
            query(
              $skip1: Boolean!,
              $skip2: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
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
            query(
              $skip1: Boolean!,
              $skip2: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
              votables {
                voteCount
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
              $include: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
              $include: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
              $include: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
              $skip3: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                dimension {
                  ... @skip(if: $skip) {
                    width
                    height
                  }
                }
                name @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                secondaryName: name
                primaryName: name @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              productBySlug(slug: "a") {
                name
                dimension {
                  width
                }
                description @skip(if: $skip)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $include: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert - should merge dimension fields appropriately
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
              productBySlug(slug: "a") {
                name
                dimension {
                  depth
                  ... @skip(if: $skip1) {
                    width
                    height @skip(if: $skip2)
                  }
                }
                tags @skip(if: $skip1)
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
              votables {
                ... @skip(if: $skip) {
                  viewerCanVote
                  ... on Product {
                    voteCount
                  }
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $skip1: Boolean!
              $skip2: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $conditional: Boolean!
            ) {
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
        var rewritten = rewriter.RewriteOperation(doc);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            query(
              $conditional: Boolean!
            ) {
              productBySlug(slug: "a") @include(if: $conditional) {
                name
              }
            }
            """);
    }
}
