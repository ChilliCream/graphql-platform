using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types;

public interface ISourceMemberCollection<out TMember>
    : IEnumerable<TMember>
    where TMember: ISourceMember
{
    int Count { get; }

    TMember this[string schemaName] { get; }

    bool ContainsSchema(string schemaName);

    ImmutableArray<string> Schemas { get; }
}
