namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// The <c>AnotherPost</c> interface as projected by subgraph <c>b</c>.
/// </summary>
public interface IAnotherPost
{
    string Id { get; }

    string CreatedAt { get; }
}
