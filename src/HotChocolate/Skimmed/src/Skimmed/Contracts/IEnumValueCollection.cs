using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface IEnumValueCollection : ICollection<EnumValue>
{
    EnumValue this[string name] { get; }

    bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value);

    void Insert(int index, EnumValue value);

    bool Remove(string name);

    void RemoveAt(int index);

    bool ContainsName(string name);

    int IndexOf(EnumValue value);

    int IndexOf(string name);
}
