using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface IDirectiveCollection
        : IReadOnlyCollection<IDirective>
    {
        IEnumerable<IDirective> this[NameString key] { get; }

        IDirective GetFirst(NameString directiveName);

        bool Contains(NameString key);
    }
}
