using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class SkipFragmentTests : FusionTestBase
{
     [Test]
     public async Task Skip_On_RootFragment()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!, $skip: Boolean!) {
                 ...QueryFragment @skip(if: $skip)
                 products {
                   nodes {
                     name
                   }
                 }
             }

             fragment QueryFragment on Query {
                 productBySlug(slug: $slug) {
                     name
                 }
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_RootFragment_If_False()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 ...QueryFragment @skip(if: false)
                 products {
                   nodes {
                     name
                   }
                 }
             }

             fragment QueryFragment on Query {
                 productBySlug(slug: $slug) {
                     name
                 }
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_RootFragment_If_True()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 ...QueryFragment @skip(if: true)
                 products {
                   nodes {
                     name
                   }
                 }
             }

             fragment QueryFragment on Query {
                 productBySlug(slug: $slug) {
                     name
                 }
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_RootFragment_Only_Skipped_Fragment_Selected()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!, $skip: Boolean!) {
                 ...QueryFragment @skip(if: $skip)
             }

             fragment QueryFragment on Query {
                 productBySlug(slug: $slug) {
                     name
                 }
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_RootFragment_Only_Skipped_Fragment_Selected_If_False()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 ...QueryFragment @skip(if: false)
             }

             fragment QueryFragment on Query {
                 productBySlug(slug: $slug) {
                     name
                 }
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_RootFragment_Only_Skipped_Fragment_Selected_If_True()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 ...QueryFragment @skip(if: true)
             }

             fragment QueryFragment on Query {
                 productBySlug(slug: $slug) {
                     name
                 }
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_SubFragment()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!, $skip: Boolean!) {
                 productBySlug(slug: $slug) {
                     ...ProductFragment @skip(if: $skip)
                     description
                 }
             }

             fragment ProductFragment on Product {
                 name
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_SubFragment_If_False()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 productBySlug(slug: $slug) {
                     ...ProductFragment @skip(if: false)
                     description
                 }
             }

             fragment ProductFragment on Product {
                 name
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_SubFragment_If_True()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 productBySlug(slug: $slug) {
                     ...ProductFragment @skip(if: true)
                     description
                 }
             }

             fragment ProductFragment on Product {
                 name
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_SubFragment_Only_Skipped_Fragment_Selected()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!, $skip: Boolean!) {
                 productBySlug(slug: $slug) {
                     ...ProductFragment @skip(if: $skip)
                 }
             }

             fragment ProductFragment on Product {
                 name
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_SubFragment_Only_Skipped_Fragment_SelectedIf_False()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 productBySlug(slug: $slug) {
                     ...ProductFragment @skip(if: false)
                 }
             }

             fragment ProductFragment on Product {
                 name
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }

     [Test]
     public async Task Skip_On_SubFragment_Only_Skipped_Fragment_Selected_If_True()
     {
         // arrange
         var compositeSchema = CreateCompositeSchema();

         var request = Parse(
             """
             query GetProduct($slug: String!) {
                 productBySlug(slug: $slug) {
                     ...ProductFragment @skip(if: true)
                 }
             }

             fragment ProductFragment on Product {
                 name
             }
             """);

         // act
         var plan = PlanOperation(request, compositeSchema);

         // assert
         await MatchSnapshotAsync(request, plan);
     }
}
