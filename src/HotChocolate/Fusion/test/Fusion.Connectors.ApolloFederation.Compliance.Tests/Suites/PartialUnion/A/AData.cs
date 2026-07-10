namespace HotChocolate.Fusion.Suites.PartialUnion.A;

internal static class AData
{
    public const string Message = "Hello, Federation!";

    public static readonly Alpha Alpha = new()
    {
        Id = "alpha-1",
        Value = "alpha value"
    };

    public static readonly Beta Beta = new()
    {
        Id = "beta-1",
        Name = "beta name",
        Details = "beta details"
    };

    public static readonly Gamma Gamma = new()
    {
        Id = "gamma-1",
        Label = "gamma label"
    };

    public static readonly Response Response = new()
    {
        Message = Message,
        Actions = [Alpha, Beta, Gamma]
    };
}
