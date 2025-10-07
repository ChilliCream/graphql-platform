using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Rewriters;

public class OperationRewriterTests
{
    [Fact]
    public void Field_Without_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Field_Without_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional_In_Different_Parent_Selection()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Field_With_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Field_With_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional_Reversed()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Field_With_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional_In_Different_Parent_Selection()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Field_With_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional_In_Different_Parent_Selection_Reversed()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Field_With_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional_In_Different_Parent_Selection_Over_Multiple_Levels()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Field_With_SelectionSet_And_Conditional_Merged_With_Field_Without_Conditional_In_Different_Parent_Selection_Over_Multiple_Levels_Reversed()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Conditionals_Are_Removed_If_Already_In_A_Conditional_Scope()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Conditionals_Are_Removed_If_Already_In_A_Conditional_Scope_In_Different_Parent_Selection()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Conditionals_Are_Removed_If_Already_In_A_Conditional_Scope_In_Different_Parent_Selection_2()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
    public void Same_Conditionals_In_Different_Order_Are_Merged()
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
        var rewriter = new OperationRewriter(schemaDefinition);
        var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test1()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test2()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test3()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test4()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test5()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test6()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test7()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test8()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test9()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test10()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test11()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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

     [Fact]
     public void Test12()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

         // assert
         rewritten.MatchInlineSnapshot(
             """
             query(
               $skip1: Boolean!
               $skip2: Boolean!
             ) {
               votables {
                 voteCount
                 ... on Product @skip(if: $skip2) {
                   viewerCanVote
                 }
               }
             }
             """);
     }

     [Fact]
     public void Test13()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

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
     public void Test14()
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
         var rewriter = new OperationRewriter(schemaDefinition);
         var rewritten = rewriter.RewriteDocument(doc);

         // assert
         rewritten.MatchInlineSnapshot(
             """
             query(
               $skip: Boolean!
               $include: Boolean!
             ) {
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
     }
}
