using HotChocolate.Data.Filters;
using HotChocolate.Execution.Configuration;
using HotChocolate.MongoDb.Data.Filters;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
    /// </summary>
    public static class HotChocolateMongoDataRequestBuilderExtensions
    {
        /// <summary>
        /// Adds filtering support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddMongoDbFiltering(
            this IRequestExecutorBuilder builder) =>
            builder.ConfigureSchema(s => s.AddFiltering(x => x.AddMongoDbDefaults()));
    }
}
