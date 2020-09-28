using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Enables relay schema style.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        public static IRequestExecutorBuilder EnableRelaySupport(
            this IRequestExecutorBuilder builder) =>
            builder.ConfigureSchema(c => c.EnableRelaySupport());
    }
}
