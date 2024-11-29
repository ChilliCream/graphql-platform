namespace HotChocolate.Types.Pagination.Utilities;

internal static class EmptyConnectionHolder
{
    public static readonly Connection Empty =
        new(Array.Empty<IEdge>(), EmptyConnectionPageInfoHolder.Empty);
}

internal static class EmptyConnectionHolder<T>
{
    public static readonly Connection<T> Empty =
        new(Array.Empty<Edge<T>>(), EmptyConnectionPageInfoHolder.Empty);
}
