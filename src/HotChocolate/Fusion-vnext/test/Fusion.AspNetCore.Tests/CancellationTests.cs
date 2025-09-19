using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

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
            isTimingOut: true);

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(250)));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            """
            {
                topProduct {
                    id
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Execution_Is_Halted_While_Http_Request_In_Node_Is_Still_Ongoing()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>(),
            isTimingOut: true);

        using var server2 = CreateSourceSchema(
            "B",
            b => b
                .AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ],
        configureGatewayBuilder: builder =>
            builder.ModifyRequestOptions(o => o.DefaultErrorHandlingMode = ErrorHandlingMode.Halt));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            """
            {
                topProduct {
                    id
                }
                reviews {
                    id
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Execution_Is_Halted_While_Subscription_Is_Still_Ongoing()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema2.Query>()
                .AddSubscriptionType<SourceSchema2.Subscription>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.DefaultErrorHandlingMode = ErrorHandlingMode.Halt));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            """
            subscription {
                onReviewCreated {
                    id
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        var snapshot = new Snapshot();

        await foreach (var response in result.ReadAsResultStreamAsync())
        {
            snapshot.Add(response);
        }

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Http_Request_To_Source_Schema_Hits_HttpClient_Timeout()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema1.Query>(),
            configureHttpClient: client => client.Timeout = TimeSpan.FromMilliseconds(250),
            isTimingOut: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            """
            {
                topProduct {
                    id
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ErrorHandlingMode_Can_Be_Overridden()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ],
        configureGatewayBuilder: builder => builder
            .ModifyRequestOptions(o => o.AllowErrorHandlingModeOverride = true));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    reviews {
                        id
                    }
                }
                """,
                onError: ErrorHandlingMode.Halt),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    public sealed class SourceSchema1
    {
        public class Query
        {
            public Product? TopProduct() => new(1);
        }

        public record Product(int Id);
    }

    public sealed class SourceSchema2
    {
        public class Query
        {
            public Review[]? Reviews(IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage("Could not resolve reviews")
                    .SetPath(context.Path)
                    .Build());
        }

        public class Subscription
        {
            public async IAsyncEnumerable<Review> OnReviewCreatedStream()
            {
                yield return new Review(1);

                await Task.Delay(250);

                yield return new Review(2);
            }

            [Subscribe(With = nameof(OnReviewCreatedStream))]
            public Review? OnReviewCreated([EventMessage] Review review, IResolverContext context)
            {
                if (review.Id == 2)
                {
                    throw new GraphQLException(ErrorBuilder.New()
                        .SetMessage("Could not produce review")
                        .SetPath(context.Path)
                        .Build());
                }

                return review;
            }
        }

        public record Review(int Id);
    }
}
