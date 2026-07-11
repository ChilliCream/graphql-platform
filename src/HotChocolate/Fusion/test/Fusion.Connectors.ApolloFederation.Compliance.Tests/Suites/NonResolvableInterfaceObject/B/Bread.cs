namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

public interface IProduct
{
    string Id { get; }
}

public sealed class Bread : IProduct
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
