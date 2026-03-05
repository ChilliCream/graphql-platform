namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

internal sealed class BestPracticeDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required BestPracticeCategory Category { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public required IReadOnlyList<string> Styles { get; init; }

    public required string Keywords { get; init; }

    public required string Abstract { get; init; }

    public required string Body { get; init; }
}
