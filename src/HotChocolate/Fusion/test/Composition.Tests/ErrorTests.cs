using HotChocolate.Fusion.Shared;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class ErrorTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Typo_In_Schema()
    {
        const string schema =
            """
            type Author {
                id: ID!
                reviews: ReviewConnection
            }

            type Book {
                id: ID!
                reviews: ReviewConnection
            }

            input CreateReviewInput {
                bookId: ID!
                reviewerId: ID!
                authorId: ID!
                comment: String!
                rating: Int!
            }

            input DeleteReviewInput {
                id: ID!
            }

            type Review {
                id: ID!
                bookId: ID!
                reviewerId: ID!
                authorId: ID!
                comment: String!
                rating: Int!
            }

            type ReviewConnection {
                items: [Review]
                nextToken: String
            }

            type Mutation {
                createReview(input: CreateReviewInput!): Review
                deleteReview(input: DeleteReviewInput!): Review
            }

            type Query {
                getReview(id: ID!): Review
                listReviews(limit: Int, nextToken: String): ReviewConnection
                queryReviewsByBook(bookId: ID!, limit: int, nextToken: String): ReviewConnection
                queryReviewsByReviewer(reviewerId: ID!, limit: int, nextToken: String): ReviewConnection
                queryReviewsByAuthor(authorId: ID!, limit: int, nextToken: String): ReviewConnection
            }

            schema {
                query: Query,
                mutation: Mutation
            }
            """;

        var log = new ErrorCompositionLog();
        var composer = new FusionGraphComposer(logFactory: () => log);

        var fusionConfig = await composer.TryComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test123",
                    schema,
                    Array.Empty<string>(),
                    new IClientConfiguration[]
                    {
                        new HttpClientConfiguration(new Uri("http://localhost")),
                    },
                    null),
            });

        Assert.Null(fusionConfig);
        Assert.True(log.HasErrors);
        Assert.Collection(
            log.Errors,
            a =>
            {
                Assert.Equal(
                    "The type `int` is not declared on subgraph Test123. " +
                    "Check the subgraph schema for consistency.",
                    a.Message);
                Assert.Equal("HF0009", a.Code);
            });
    }
}
