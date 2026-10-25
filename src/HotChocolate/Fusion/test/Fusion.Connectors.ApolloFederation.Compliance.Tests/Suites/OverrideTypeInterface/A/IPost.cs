namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.A;

/// <summary>
/// The <c>Post</c> interface as projected by subgraph <c>a</c>.
/// </summary>
public interface IPost
{
    string Id { get; }

    string CreatedAt { get; }
}
