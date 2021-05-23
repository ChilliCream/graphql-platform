using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Sets the global paging options.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="options">
        /// The paging options.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder SetPagingOptions(
            this IRequestExecutorBuilder builder,
            PagingOptions options)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(s => s.SetPagingOptions(options));
        }
    }
}
