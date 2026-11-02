namespace HotChocolate.Fusion.Suites.UnionIntersection.A;

internal static class AData
{
    public static readonly Book Media = new()
    {
        Id = "b1",
        Title = "The Lord of the Rings",
        ATitle = "A: The Lord of the Rings"
    };

    public static readonly Song SongData = new()
    {
        Id = "s2",
        Title = "Song Title",
        ATitle = "A: Song Title"
    };
}
