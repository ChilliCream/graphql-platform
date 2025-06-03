using HotChocolate.Configuration;

namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    public static ISchemaBuilder TryAddTypeInterceptor<T>(
        this ISchemaBuilder builder)
        where T : TypeInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.TryAddTypeInterceptor(typeof(T));
    }
}
