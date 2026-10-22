using HotChocolate.Fusion.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

internal readonly struct SourceResponseNameMapping(
    string fieldName,
    string sourceResponseName,
    string responseName)
{
    private readonly byte[] _utf8JsonPropertyName = Utf8StringCache.GetJsonPropertyName(responseName);

    public string FieldName { get; } = fieldName;

    public string SourceResponseName { get; } = sourceResponseName;

    public string ResponseName { get; } = responseName;

    public ReadOnlySpan<byte> ResponseNameUtf8
        => _utf8JsonPropertyName is null ? default : _utf8JsonPropertyName.AsSpan(1..^2);
}
