using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class CancellationTests : FusionTestBase
{
    [Fact]
    public async Task Request_Is_Running_Into_Execution_Timeout_While_Http_Request_In_Node_Is_Still_Ongoing()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>(),
            isTimeouting: true);

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ],
        configureGatewayBuilder: builder =>
            builder.ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(250)));

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                topProduct {
                    id
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    // [Fact]
    // public async Task Execution_Is_Halted_While_Http_Request_In_Node_Is_Still_Ongoing()
    // {
    //
    // }
    //
    // [Fact]
    // public async Task Http_Request_To_Source_Schema_Hits_HttpClient_Timeout()
    // {
    //
    // }

    public sealed class SourceSchema1
    {
        public class Query
        {
            public Product? TopProduct() => new(1);
        }

        public record Product(int Id);
    }
}
