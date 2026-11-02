namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// Marker interface for the <c>Address</c> type hierarchy in subgraph <c>a</c>.
/// </summary>
public interface IAddress
{
    string Id { get; }
    string? City { get; }
}
