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
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddTypeInterceptor(typeof(T));
        }

        public static ISchemaBuilder TryAddTypeInterceptor<T>(
            this ISchemaBuilder builder)
            where T : ITypeInitializationInterceptor
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.TryAddTypeInterceptor(typeof(T));
        }
    }
}
