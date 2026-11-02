namespace ChilliCream.Nitro.CommandLine.Helpers;

internal delegate IEnumerable<TEdge>? SelectEdges<out TEdge, in TResult>(TResult result);
