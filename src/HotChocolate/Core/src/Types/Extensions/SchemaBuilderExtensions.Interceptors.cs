using System;
using HotChocolate.Configuration;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddTypeInterceptor<T>(
            this ISchemaBuilder builder)
            where T : ITypeInitializationInterceptor
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddTypeInterceptor(typeof(T));
        }
    }
}
