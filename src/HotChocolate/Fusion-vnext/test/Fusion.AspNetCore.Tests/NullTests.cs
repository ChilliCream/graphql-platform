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

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                nonNullString
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public string NonNullString() => "";
        }
    }
}
