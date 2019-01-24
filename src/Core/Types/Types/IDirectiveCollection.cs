using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types
{
    public interface IDirectiveCollection
        : IReadOnlyCollection<IDirective>
    {
        IEnumerable<IDirective> this[NameString key] { get; }

        bool Contains(NameString key);
    }
}
