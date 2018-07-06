using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface IFieldCollection<out T>
        : IEnumerable<T>
        where T : IField
    {
        T this[string fieldName] { get; }

        bool ContainsField(string fieldName);
    }
}
