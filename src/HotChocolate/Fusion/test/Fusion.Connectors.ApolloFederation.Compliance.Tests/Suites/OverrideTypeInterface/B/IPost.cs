namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// The <c>Post</c> interface as projected by subgraph <c>b</c>.
/// </summary>
public interface IPost
{
    string Id { get; }

    string CreatedAt { get; }
}
