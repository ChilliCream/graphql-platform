using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Projections;

internal static class OutputFieldExtensions
{
    public static bool IsNotProjected(this IOutputFieldDefinition field)
        => field.IsExcludedManually() || field.HasProjectionMiddleware() || field.IsPagingField();

    private static bool IsExcludedManually(this IOutputFieldDefinition field)
        => field.HasCoreFieldFlags(CoreFieldFlags.NotProjected);

    public static bool IsAlwaysProjected(this IOutputFieldDefinition field)
        => field.HasCoreFieldFlags(CoreFieldFlags.AlwaysProjected);

    public static bool HasProjectionMiddleware(this IOutputFieldDefinition field)
        => field.HasCoreFieldFlags(CoreFieldFlags.HasProjectionMiddleware);

    private static bool IsPagingField(this IOutputFieldDefinition field)
        => ((field.Flags & FieldFlags.Connection) == FieldFlags.Connection)
            || ((field.Flags & FieldFlags.CollectionSegment) == FieldFlags.CollectionSegment);
}
