using System;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Configures the resolver compiler.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="configure">
        /// A delegate that is to configure the resolver compiler.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        public static IRequestExecutorBuilder ConfigureResolverCompiler(
            this IRequestExecutorBuilder builder,
            Action<IResolverCompilerBuilder> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configure(new DefaultResolverCompilerBuilder(builder));
            return builder;
        }
    }
}
