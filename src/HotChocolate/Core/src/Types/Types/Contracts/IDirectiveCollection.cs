using System.Collections.Generic;

namespace HotChocolate.Types;

public interface IDirectiveCollection : IReadOnlyCollection<IDirective>
{
    IEnumerable<IDirective> this[string key] { get; }

    bool Contains(string key);
}
