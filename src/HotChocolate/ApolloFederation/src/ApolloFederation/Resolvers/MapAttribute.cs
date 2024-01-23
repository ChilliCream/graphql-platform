namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// Maps an argument to a representation value.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class MapAttribute : Attribute
{
    /// <summary>
    /// Represents an argument to a representation value.
    /// </summary>
    /// <param name="path">
    /// A path to field in the representation graph.
    /// </param>
    public MapAttribute(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Path = path;
    }

    /// <summary>
    /// A path to field in the representation graph.
    /// </summary>
    public string Path { get; }
}
