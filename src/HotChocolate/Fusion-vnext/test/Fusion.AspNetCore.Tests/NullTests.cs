using HotChocolate.Execution;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class NullTests : FusionTestBase
{
    [Fact(Skip = "Fix result generation")]
    public async Task Raise_NonNullViolation_Error_For_NonNull_Field_Being_Null()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>()
                .InsertUseRequest(
                    WellKnownRequestMiddleware.OperationExecutionMiddleware,
                    (_, _) => context =>
                    {
                        // TODO: Re-add this
                        // context.Result = new OperationResult(
                        //     new Dictionary<string, object?>
                        //     {
                        //         ["nonNullString"] = null
                        //     });
                        return ValueTask.CompletedTask;
                    },
                    key: "SetNull"));

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            {
                nonNullString
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public string NonNullString() => "";
        }
    }
}
