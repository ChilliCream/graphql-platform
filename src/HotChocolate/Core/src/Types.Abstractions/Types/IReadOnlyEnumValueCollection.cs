using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyEnumValueCollection : IEnumerable<IReadOnlyEnumValue>
{
    IReadOnlyEnumValue this[string name] { get; }

    bool TryGetValue(string name, [NotNullWhen(true)] out IReadOnlyEnumValue? value);

    bool ContainsName(string name);
}
