namespace HotChocolate.Fusion.Metadata;

internal sealed class FusionGraphConfig
{
    /// <summary>
    /// Gets the names of the sub-graphs involved composed into the fusion graph
    /// </summary>
    public IReadOnlyList<string> SubGraphNames { get; }

    public string ToLocalName(SubGraphCoordinate coordinate)
    {
        throw new NotImplementedException();
    }

    public string ToRemoteName(string localName, string subGraphName)
    {
        throw new NotImplementedException();
    }
}

public readonly record struct SubGraphCoordinate(
    SchemaCoordinate Coordinate,
    string SubGraphName);
