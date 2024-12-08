using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class FragmentTests : FusionTestBase
{
    [Test]
    public async Task Fragment_On_Root()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                ...QueryFragment
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
    public async Task Fragment_On_Root_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                }
                ...QueryFragment
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
    public async Task Fragment_On_Root_Next_To_Same_Selection_With_Different_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    description
                }
                ...QueryFragment
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
    public async Task Fragment_On_Root_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                products {
                    nodes {
                        description
                    }
                }
                ...QueryFragment
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
    [Skip("Not yet supported by the planner")]
    public async Task Fragment_On_Root_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                viewer {
                    displayName
                }
                ...QueryFragment
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
    public async Task Two_Fragments_On_Root_With_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                ...QueryFragment1
                ...QueryFragment2
            }

            fragment QueryFragment1 on Query {
                productById(id: $id) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
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
    public async Task Two_Fragments_On_Root_With_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                ...QueryFragment1
                ...QueryFragment2
            }

            fragment QueryFragment1 on Query {
                productById(id: $id) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
                products {
                    nodes {
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
    [Skip("Not yet supported by the planner")]
    public async Task Two_Fragments_On_Root_With_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                ...QueryFragment1
                ...QueryFragment2
            }

            fragment QueryFragment1 on Query {
                productById(id: $id) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
                viewer {
                    displayName
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Fragment_On_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment
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
    public async Task Fragment_On_Sub_Selection_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    ...ProductFragment
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
    public async Task Fragment_On_Sub_Selection_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    ...ProductFragment
                }
            }

            fragment ProductFragment on Product {
                description
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Fragment_On_Sub_Selection_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    name
                    ...ProductFragment
                }
            }

            fragment ProductFragment on Product {
                averageRating
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

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
            }

            fragment ProductFragment2 on Product {
                name
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

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
            }

            fragment ProductFragment2 on Product {
                description
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

        var request = Parse(
            """
            query GetProduct($id: ID!) {
                productById(id: $id) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
            }

            fragment ProductFragment2 on Product {
                averageRating
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        await MatchSnapshotAsync(request, plan);
    }
}
