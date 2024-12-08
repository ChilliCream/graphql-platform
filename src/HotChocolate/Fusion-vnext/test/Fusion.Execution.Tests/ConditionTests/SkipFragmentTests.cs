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
            query GetProduct($id: ID!, $skip: Boolean!) {
                ...QueryFragment @skip(if: $skip)
                products {
                  nodes {
                    name
                  }
                }
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: false)
                products {
                  nodes {
                    name
                  }
                }
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: true)
                products {
                  nodes {
                    name
                  }
                }
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
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
            query GetProduct($id: ID!, $skip: Boolean!) {
                ...QueryFragment @skip(if: $skip)
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: false)
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                ...QueryFragment @skip(if: true)
            }

            fragment QueryFragment on Query {
                productById(id: $id) {
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
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                productById(id: $id) {
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
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                productById(id: $id) {
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
            query GetProduct($id: ID!) {
                productById(id: $id) {
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
