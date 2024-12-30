using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types;

public interface ISourceMemberCollection<out TMember> : IEnumerable<TMember> where TMember : ISourceMember
{
    int Count { get; }

    TMember this[string schemaName] { get; }

    bool ContainsSchema(string schemaName);

    ImmutableArray<string> Schemas { get; }
}

public interface ISourceComplexTypeCollection<TType> : ISourceMemberCollection<TType> where TType : ISourceComplexType
{
    bool TryGetType(string schemaName, [NotNullWhen(true)] out TType? type);

    ImmutableArray<TType> Types { get; }
}






