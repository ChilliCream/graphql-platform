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
                .AddQueryType<Query>();
        }
    }
}
