using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder SetPagingSettings(
            this ISchemaBuilder builder,
            PagingSettings settings)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.SetContextData(typeof(PagingSettings).FullName!, settings);
        }
    }
}
