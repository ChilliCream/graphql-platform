namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

internal sealed class BestPracticeSearchRequest
{
    public BestPracticeCategory? Category { get; init; }

    public IReadOnlyList<string>? Tags { get; init; }

    public string? Query { get; init; }
}
