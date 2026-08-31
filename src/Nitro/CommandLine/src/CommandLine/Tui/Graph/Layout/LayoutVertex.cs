namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// A real node or an internal long-edge vertex participating in ordering.
/// </summary>
internal sealed class LayoutVertex
{
    public required string Key { get; init; }

    public string? NodeId { get; init; }

    public required GraphNodeSize Size { get; init; }

    public int Layer { get; set; }

    public int Order { get; set; }

    public List<LayoutSegment> Incoming { get; } = [];

    public List<LayoutSegment> Outgoing { get; } = [];

    public bool IsDummy => NodeId is null;
}

/// <summary>
/// Connects two neighboring layout vertices for crossing minimization.
/// </summary>
internal sealed record LayoutSegment(LayoutVertex From, LayoutVertex To, LayoutArc Arc, int Sequence);
