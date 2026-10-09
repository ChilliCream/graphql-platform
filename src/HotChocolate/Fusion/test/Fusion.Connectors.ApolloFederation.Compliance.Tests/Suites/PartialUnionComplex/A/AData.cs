namespace HotChocolate.Fusion.Suites.PartialUnionComplex.A;

internal static class AData
{
    public const string ContainerId = "container-1";

    public static readonly Common Common = new() { Label = "common label" };

    public static readonly OnlyA OnlyA = new() { A = "only a" };

    public static readonly IReadOnlyList<object> AActions = [Common, OnlyA];

    public static readonly IReadOnlyList<object> SharedActions = [Common];
}
