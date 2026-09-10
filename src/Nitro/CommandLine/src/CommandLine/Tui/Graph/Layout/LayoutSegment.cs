namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Connects two neighboring layout vertices for crossing minimization.
/// </summary>
internal sealed record LayoutSegment(LayoutVertex From, LayoutVertex To, LayoutArc Arc, int Sequence);
