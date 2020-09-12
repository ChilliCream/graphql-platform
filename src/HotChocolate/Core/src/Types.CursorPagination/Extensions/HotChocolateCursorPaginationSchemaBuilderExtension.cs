using System;
using HotChocolate.Types.Pagination;

namespace HotChocolate
{
    public static class HotChocolateCursorPaginationSchemaBuilderExtension
    {
        public static ISchemaBuilder SetConnectionSettings(
            this ISchemaBuilder builder,
            ConnectionSettings settings)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.SetContextData(ConnectionSettings.GetKey(), settings);
        }
    }
}
