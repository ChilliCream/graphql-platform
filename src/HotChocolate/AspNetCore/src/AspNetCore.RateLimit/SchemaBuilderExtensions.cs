using System;

namespace HotChocolate.AspNetCore.RateLimit
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddLimitDirectiveType(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddDirectiveType<LimitDirectiveType>();
        }
    }
}
