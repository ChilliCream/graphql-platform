namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Marker interface for the <c>Address</c> type hierarchy in subgraph <c>b</c>.
/// </summary>
public interface IAddress
{
    string Id { get; }
    string? City { get; }
}
