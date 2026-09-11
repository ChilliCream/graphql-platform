namespace HotChocolate.Utilities;

internal static class MiddlewareNames
{
    internal const string UseDbContext = "UseDbContext";
    internal const string UsePaging = "UsePaging";
    internal const string UseProjection = "UseProjection";
    internal const string UseFiltering = "UseFiltering";
    internal const string UseSorting = "UseSorting";

    internal static string? GetDisplayName(string? key)
        => key switch
        {
            WellKnownMiddleware.DbContext => UseDbContext,
            WellKnownMiddleware.Paging => UsePaging,
            WellKnownMiddleware.Projection => UseProjection,
            WellKnownMiddleware.Filtering => UseFiltering,
            WellKnownMiddleware.Sorting => UseSorting,
            _ => null
        };
}
