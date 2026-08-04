namespace HotChocolate.Fusion.Suites.UnionIntersection.B;

internal static class BData
{
    public static readonly Book Media = new()
    {
        Id = "b1",
        Title = "The Lord of the Rings",
        BTitle = "B: The Lord of the Rings"
    };

    public static readonly Movie MovieData = new()
    {
        Id = "m3",
        Title = "A Movie Title",
        BTitle = "B Movie Title"
    };
}
