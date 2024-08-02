using HotChocolate.Configuration;

namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    public static ISchemaBuilder TryAddTypeInterceptor<T>(
        this ISchemaBuilder builder)
        where T : TypeInterceptor
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.TryAddTypeInterceptor(typeof(T));
    }
}
