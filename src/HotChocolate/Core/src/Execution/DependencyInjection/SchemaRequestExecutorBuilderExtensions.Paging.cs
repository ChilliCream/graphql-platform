using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
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
