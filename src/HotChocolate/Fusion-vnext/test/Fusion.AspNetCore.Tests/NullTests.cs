using HotChocolate.Execution;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class NullTests : FusionTestBase
{
    [Fact]
    public async Task Raise_NonNullViolation_Error_For_NonNull_Field_Being_Null()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>()
                .InsertUseRequest("OperationExecutionMiddleware", (_, _) => context =>
                {
                    context.Result = OperationResultBuilder.New()
                        .SetData(new Dictionary<string, object?> { ["nonNullString"] = null })
                        .Build();

                    return ValueTask.CompletedTask;
                }, key: "SetNull"));

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
