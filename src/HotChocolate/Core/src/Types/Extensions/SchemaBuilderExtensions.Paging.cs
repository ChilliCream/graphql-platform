using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder SetPagingOptions(
            this ISchemaBuilder builder,
            PagingOptions options)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.SetContextData(typeof(PagingOptions).FullName!, options);
        }
    }
}
