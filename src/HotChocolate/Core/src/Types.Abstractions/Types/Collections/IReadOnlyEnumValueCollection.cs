using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyEnumValueCollection : IEnumerable<IEnumValue>
{
    IEnumValue this[string name] { get; }

    bool TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value);

    bool ContainsName(string name);
}
