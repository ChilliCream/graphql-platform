using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface IEnumValueCollection : ICollection<EnumValue>
{
    EnumValue this[string name] { get; }

    bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? field);

    bool ContainsName(string name);
}
