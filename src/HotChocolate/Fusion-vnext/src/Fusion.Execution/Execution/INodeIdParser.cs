using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Execution;

public interface INodeIdParser
{
    bool TryParseTypeNameFromId(string id, [NotNullWhen(true)] out string? typeName);
}
