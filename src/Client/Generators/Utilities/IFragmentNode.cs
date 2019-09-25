using System.Collections.Generic;

namespace StrawberryShake.Generators.Utilities
{
    internal interface IFragmentNode
    {
        IFragment Fragment { get; }
        IReadOnlyList<IFragmentNode> Children { get; }
    }
}
