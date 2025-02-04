namespace HotChocolate.Data;

public static class TestHelpers
{

    // this is only to keep test snapshots ordered.
    public static IReadOnlyList<int> EnsureOrdered(this IReadOnlyList<int> ids)
        => ids.OrderBy(t => t).ToArray();
}
