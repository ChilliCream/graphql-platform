using System.Collections;
using System.Collections.Frozen;

namespace HotChocolate.Fusion.Types.Collections;

public class SourceMemberCollection<TMember> : IEnumerable<TMember> where TMember : ISourceMember
{
    private readonly FrozenDictionary<string, TMember> _members;

    protected SourceMemberCollection(IEnumerable<TMember> members)
    {
        _members = members.ToFrozenDictionary(t => t.SchemaName);
    }

    public int Count => _members.Count;

    public TMember this[string schemaName] => _members[schemaName];

    public IEnumerator<TMember> GetEnumerator()
        => ((IEnumerable<TMember>)_members.Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
