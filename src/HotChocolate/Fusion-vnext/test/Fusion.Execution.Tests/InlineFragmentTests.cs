using HotChocolate.Language;

namespace HotChocolate.Fusion;

public class InlineFragmentTests : FusionTestBase
{
    [Test]
    public async Task InlineFragment_On_Root()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                ... {
                    productById(id: $id) {
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
    public async Task InlineFragment_On_Root_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                }
                ... {
                    productById(id: $id) {
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
    public async Task InlineFragment_On_Root_Next_To_Same_Selection_With_Different_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    description
                }
                ... {
                    productById(id: $id) {
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
    public async Task InlineFragment_On_Root_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                products {
                    nodes {
                        description
                    }
                }
                ... {
                    productById(id: $id) {
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
    [Skip("Not yet supported by the planner")]
    public async Task InlineFragment_On_Root_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                viewer {
                    displayName
                }
                ... {
                    productById(id: $id) {
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
    public async Task Two_InlineFragments_On_Root_With_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                ... {
                    productById(id: $id) {
                        name
                    }
                }
                ... {
                    productById(id: $id) {
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
    public async Task Two_InlineFragments_On_Root_With_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                ... {
                    productById(id: $id) {
                        name
                    }
                }
                ... {
                    products {
                        nodes {
                            description
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
    [Skip("Not yet supported by the planner")]
    public async Task Two_InlineFragments_On_Root_With_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                ... {
                    productById(id: $id) {
                        name
                    }
                }
                ... {
                    viewer {
                        displayName
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
    public async Task InlineFragment_On_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ... {
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
    public async Task InlineFragment_On_Sub_Selection_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    ... {
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
    public async Task InlineFragment_On_Sub_Selection_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    ... {
                        description
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
    public async Task InlineFragment_On_Sub_Selection_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    ... {
                        averageRating
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
    public async Task Two_Fragments_On_Sub_Selection_With_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ... {
                        name
                    }
                    ... {
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
    public async Task Two_Fragments_On_Sub_Selection_With_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ... {
                        name
                    }
                    ... {
                        description
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
    public async Task Two_Fragments_On_Sub_Selection_With_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                   ... {
                       name
                   }
                   ... {
                       averageRating
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
