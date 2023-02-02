namespace HotChocolate.Stitching.Merge;

internal static class EnumerableExtensions
{
    public static IReadOnlyList<ITypeInfo> NotOfType<T>(
        this IEnumerable<ITypeInfo> types)
    {
        if (types is null)
        {
            throw new ArgumentNullException(nameof(types));
        }

        var list = new List<ITypeInfo>();

        foreach (var type in types)
        {
            if (type is not T)
            {
                list.Add(type);
            }
        }

        return list;
    }
}
