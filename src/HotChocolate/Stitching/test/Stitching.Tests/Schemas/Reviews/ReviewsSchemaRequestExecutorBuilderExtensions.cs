using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Reviews
{
    public static class ReviewSchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddReviewSchema(
            this IRequestExecutorBuilder builder)
        {
            builder.Services
                .AddSingleton<ReviewRepository>();

            return builder
                .AddQueryType<Query>()
                .PublishSchemaDefinition(c => c
                    .SetName("reviews")
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        @"extend type User {
                            reviews: [Review]
                                @delegate(path:""reviewsByAuthor(authorId: $fields:id)"")
                        }

                        extend type Product {
                            reviews: [Review]
                                @delegate(path:""reviewsByProduct(upc: $fields:upc)"")
                        }"));
        }
    }
}
