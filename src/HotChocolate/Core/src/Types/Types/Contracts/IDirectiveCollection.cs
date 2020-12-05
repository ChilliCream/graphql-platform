using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface IDirectiveCollection : IReadOnlyCollection<IDirective>
    {
        IEnumerable<IDirective> this[NameString key] { get; }

        bool Contains(NameString key);
    }
}
