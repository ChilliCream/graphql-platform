using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class SkipAndIncludeTests : FusionTestBase
{
    [Test]
    public void Skip_And_Include_On_RootField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skip: Boolean!, $include: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) @include(if: $include) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_With_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Skip_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) @include(if: $include) @skip(if: false) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Skip_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) @include(if: $include) @skip(if: true) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Skip_True_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: true) @include(if: false) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Skip_False_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: false) @include(if: true) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Skip_True_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: true) @include(if: true) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Skip_False_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: false) @include(if: false) {
                    name
                }
                products {
                    nodes {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skip: Boolean!, $include: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) @include(if: $include) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_With_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) @include(if: $include) @skip(if: false) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) @include(if: $include) @skip(if: true) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: true) @include(if: false) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: false) @include(if: true) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: true) @include(if: true) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) @skip(if: false) @include(if: false) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skip: Boolean!, $include: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skip) @include(if: $include)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_With_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skipOrInclude) @include(if: $skipOrInclude)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Skip_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) {
                    name @include(if: $include) @skip(if: false)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Skip_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) {
                    name @include(if: $include) @skip(if: true)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Skip_True_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: true) @include(if: false)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Skip_False_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: false) @include(if: true)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Skip_True_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: true) @include(if: true)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Skip_False_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: false) @include(if: false)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skip: Boolean!, $include: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skip) @include(if: $include)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) {
                    name @include(if: $include) @skip(if: false)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $include: Boolean!) {
                productBySlug(slug: $slug) {
                    name @include(if: $include) @skip(if: true)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_With_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skipOrInclude) @include(if: $skipOrInclude)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_True_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: true) @include(if: false)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_False_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: false) @include(if: true)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_True_Include_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: true) @include(if: true)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_False_Include_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: false) @include(if: false)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }
}
