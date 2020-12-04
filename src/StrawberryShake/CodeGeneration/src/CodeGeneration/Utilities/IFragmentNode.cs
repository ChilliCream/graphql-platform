using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal interface IFragmentNode
    {
        NameString Name { get; }

        IFragment Fragment { get; }

        IReadOnlyList<IFragmentNode> Children { get; }
    }
}
