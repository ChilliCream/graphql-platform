namespace Mocha.Shape.Contracts;

/// <summary>
/// A command family funnelled through one named endpoint.
/// </summary>
public interface IShapeCommand
{
    string Id { get; }
}

public sealed record RemoveThing(string Id) : IShapeCommand;

public sealed record ThingBooked(string Id);

public sealed record ThingPooled(string Id);

public sealed record ThingTaken(string Id);
