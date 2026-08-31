using HotChocolate.Fusion.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

internal readonly struct SourceResponseNameMapping(
    string fieldName,
    string sourceResponseName,
    string responseName)
{
    private readonly byte[] _responseNameUtf8 = Utf8StringCache.GetUtf8String(responseName);

    public string FieldName { get; } = fieldName;

    public string SourceResponseName { get; } = sourceResponseName;

    public string ResponseName { get; } = responseName;

    public ReadOnlySpan<byte> ResponseNameUtf8 => _responseNameUtf8;
}
