using System;
using HotChocolate;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder SetPagingSettings(
            this IRequestExecutorBuilder builder,
            PagingSettings settings)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(s => s.SetPagingSettings(settings));
        }
    }
}
