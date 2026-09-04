namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

internal sealed record GraphLayoutCandidateObservation(
    int IncidentBefore,
    int IncidentAfter,
    int FullBefore,
    int FullAfter,
    bool Accepted);
