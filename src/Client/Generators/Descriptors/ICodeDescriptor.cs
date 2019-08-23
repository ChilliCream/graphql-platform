using System.Collections.Generic;

namespace StrawberryShake.Generators.Descriptors
{
    public interface ICodeDescriptor
    {
        string Name { get; }

        IEnumerable<ICodeDescriptor> GetChildren();
    }
}
