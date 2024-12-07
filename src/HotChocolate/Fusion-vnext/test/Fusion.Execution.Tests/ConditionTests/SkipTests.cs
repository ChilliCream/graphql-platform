using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class SkipTests : FusionTestBase
{
    [Test]
    public async Task Skip_On_RootField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) @skip(if: $skip) {
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_RootField_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) {
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_RootField_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) {
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_RootField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) @skip(if: $skip) {
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
    public async Task Skip_On_RootField_Only_Skipped_Field_Selected_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: false) {
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
    public async Task Skip_On_RootField_Only_Skipped_Field_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) @skip(if: true) {
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
    public async Task Skip_On_SubField()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name @skip(if: $skip)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: false)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: true)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name @skip(if: $skip)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Only_Skipped_Field_Selected_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: false)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Only_Skipped_Field_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name @skip(if: true)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name
                    averageRating
                    reviews(first: 10) @skip(if: $skip) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    averageRating
                    reviews(first: 10) @skip(if: false) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    averageRating
                    reviews(first: 10) @skip(if: true) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!, $skip: Boolean!) {
                productById(id: $id) {
                    name
                    reviews(first: 10) @skip(if: $skip) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    reviews(first: 10) @skip(if: false) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    reviews(first: 10) @skip(if: true) {
                        nodes {
                            body
                        }
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }
}
