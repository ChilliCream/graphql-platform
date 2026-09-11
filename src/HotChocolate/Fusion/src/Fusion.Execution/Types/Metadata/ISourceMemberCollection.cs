using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types.Metadata;

public interface ISourceMemberCollection<TMember>
    : IEnumerable<TMember>
    where TMember : ISourceMember
{
    int Count { get; }

    TMember this[string schemaName] { get; }

    bool ContainsSchema(string schemaName);

    ImmutableArray<string> Schemas { get; }

    bool TryGetMember(string schemaName, [NotNullWhen(true)] out TMember? type);
}
