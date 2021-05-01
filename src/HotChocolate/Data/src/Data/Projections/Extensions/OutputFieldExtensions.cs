using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using static HotChocolate.Data.Projections.ProjectionConvention;
using static HotChocolate.Data.Projections.ProjectionProvider;

namespace HotChocolate.Data.Projections
{
    internal static class OutputFieldExtensions
    {
        public static bool IsNotProjected(this IOutputField field) =>
            field.ContextData.TryGetValue(IsProjectedKey, out object? isProjectedObject) &&
            isProjectedObject is false;

        public static bool HasProjectionMiddleware(this IOutputField field) =>
            field.ContextData.ContainsKey(ProjectionContextIdentifier);

        public static bool IsPagination(this IOutputField field) =>
            (field.Type is INullableType nt && nt.InnerType() is IPageType) ||
            field.Type is IPageType;
    }
}
