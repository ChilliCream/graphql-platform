namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Services;

internal readonly struct TextSearchResult
{
    public int DocumentIndex { get; init; }
    public double Score { get; init; }
}
