namespace HotChocolate.Transport.Formatters;

internal sealed class OperationResultFormatterContext
{
    private Dictionary<int, PendingResultState>? _pendingResults;

    public Dictionary<int, PendingResultState> PendingResults
        => _pendingResults ??= [];
}

internal readonly record struct PendingResultState(Path? Path, string? Label);
