namespace ChilliCream.Nitro.CommandLine.Commands.Clients;

internal sealed class PersistedQueryStreamResult
{
    public Guid ApiId { get; init; }

    public string[] DocumentIds { get; init; } = default!;

    public string Content { get; set; } = default!;
}
