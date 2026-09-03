namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Captures crossing minimization operations for verification.
/// </summary>
internal sealed class GraphLayoutMetrics
{
    private readonly List<GraphLayoutCandidateObservation>? _candidateObservations;

    public GraphLayoutMetrics(bool captureCandidateObservations = false)
    {
        if (captureCandidateObservations)
        {
            _candidateObservations = [];
        }
    }

    public int CandidateCount { get; private set; }

    public int IncidentComparisonCount { get; private set; }

    public bool CaptureCandidateObservations => _candidateObservations is not null;

    public IReadOnlyList<GraphLayoutCandidateObservation> CandidateObservations => _candidateObservations
        ?? (IReadOnlyList<GraphLayoutCandidateObservation>)Array.Empty<GraphLayoutCandidateObservation>();

    public void RecordCandidate()
    {
        CandidateCount++;
    }

    public void RecordIncidentComparison()
    {
        IncidentComparisonCount++;
    }

    public void RecordCandidateObservation(
        int incidentBefore,
        int incidentAfter,
        int fullBefore,
        int fullAfter,
        bool accepted)
    {
        _candidateObservations?.Add(
            new GraphLayoutCandidateObservation(
                incidentBefore,
                incidentAfter,
                fullBefore,
                fullAfter,
                accepted));
    }
}
