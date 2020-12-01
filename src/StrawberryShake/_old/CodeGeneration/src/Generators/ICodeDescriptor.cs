using System.Collections.Generic;

namespace StrawberryShake.Generators
{
    public interface ICodeDescriptor
    {
        string Name { get; }

        IEnumerable<ICodeDescriptor> GetChildren();
    }
}
