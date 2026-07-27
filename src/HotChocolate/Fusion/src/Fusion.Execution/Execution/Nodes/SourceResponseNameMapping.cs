using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

internal readonly struct SourceResponseNameMapping(
    string fieldName,
    string sourceResponseName,
    string responseName)
{
    public string FieldName { get; } = fieldName;

    public string SourceResponseName { get; } = sourceResponseName;

    public string ResponseName { get; } = responseName;

    public byte[] ResponseNameUtf8 { get; } = Encoding.UTF8.GetBytes(responseName);
}
