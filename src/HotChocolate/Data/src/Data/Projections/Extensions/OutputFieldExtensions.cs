using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

internal static class OutputFieldExtensions
{
    public static bool IsNotProjected(this IOutputFieldDefinition field)
        => field.IsExcludedManually() || field.HasProjectionMiddleware() || field.IsPagingField();

    private static bool IsExcludedManually(this IOutputFieldDefinition field)
        => field.Features.Get<ProjectionFeature>()?.AlwaysProjected is false;

    public static bool IsAlwaysProjected(this IOutputFieldDefinition field)
        => field.Features.Get<ProjectionFeature>()?.AlwaysProjected is true;

    public static bool HasProjectionMiddleware(this IOutputFieldDefinition field)
        => field.Features.Get<ProjectionFeature>()?.HasProjectionMiddleware is true;

    private static bool IsPagingField(this IOutputFieldDefinition field)
        => ((field.Flags & FieldFlags.Connection) == FieldFlags.Connection)
            || ((field.Flags & FieldFlags.CollectionSegment) == FieldFlags.CollectionSegment);
}
