namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

internal static class BData
{
    public const string ContainerId = "container-1";

    public static readonly Common Common = new() { Label = "common label" };

    public static readonly OnlyB OnlyB = new() { B = "only b" };

    public static readonly IReadOnlyList<object> BActions = [Common, OnlyB];

    public static readonly IReadOnlyList<object> SharedActions = [Common];
}
