using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using static HotChocolate.Data.Projections.ProjectionConvention;
using static HotChocolate.Data.Projections.ProjectionProvider;

namespace HotChocolate.Data.Projections;

internal static class OutputFieldExtensions
{
    public static bool IsNotProjected(this IOutputField field) =>
        field.IsExcludedManually() || field.HasProjectionMiddleware() || field.IsPagingField();

    private static bool IsExcludedManually(this IOutputField field)
        => field.ContextData.TryGetValue(IsProjectedKey, out var isProjectedObject) &&
            isProjectedObject is false;

    private static bool HasProjectionMiddleware(this IOutputField field)
        => field.ContextData.ContainsKey(ProjectionContextIdentifier);

    private static bool IsPagingField(this IOutputField field)
        => field.Type.NamedType() is IPageType;
}
