using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class GetMembersResult
{
    [JsonPropertyName("members")]
    public IReadOnlyList<MemberDetail> Members { get; init; } = Array.Empty<MemberDetail>();

    [JsonPropertyName("notFound")]
    public IReadOnlyList<string> NotFound { get; init; } = Array.Empty<string>();
}
