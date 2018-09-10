using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface IDirectiveCollection<out T>
        : IEnumerable<T>
        where T : IDirective
    {
        T this[string fieldName] { get; }

        bool ContainsDirective(string directiveName);
    }
}
